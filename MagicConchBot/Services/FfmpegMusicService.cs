﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using MagicConchBot.Common.Enums;
using MagicConchBot.Common.Interfaces;
using MagicConchBot.Common.Types;
using MagicConchBot.Helpers;
using NLog;

namespace MagicConchBot.Services
{
    public class FfmpegMusicService : IMusicService
    {
        private const int SampleFrequency = 48000;
        private const int Milliseconds = 20;
        private const int SamplesPerFrame = SampleFrequency * Milliseconds / 1000;
        private const int FrameBytes = 3840; // 2 channel, 16 bit

        private const int MaxVolume = 100;
        private const int MinVolume = 0;

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static readonly string[] DirectPlayFormats = { "webm", "mp3", "avi", "wav", "mp4", "flac" };

        private IAudioClient _audio;

        private CancellationTokenSource _tokenSource;

        private float _currentVolume = 0.5f;

        private readonly ConcurrentDictionary<string, Guid> _songIdDictionary;

        public FfmpegMusicService()
        {
            SongList = new List<Song>();
            PlayMode = PlayMode.Queue;
            CurrentSong = null;
            LastSong = null;
            State = MusicState.Stopped;
            _songIdDictionary = new ConcurrentDictionary<string, Guid>();
        }

        public List<Song> SongList { get; }

        public MusicState State { get; set; }

        public PlayMode PlayMode { get; set; }

        public Song LastSong { get; private set; }

        public Song CurrentSong { get; private set; }

        private int _songIndex;

        public int Volume
        {
            get
            {
                return (int)(_currentVolume * 100);
            }

            set
            {
                if (value < MinVolume)
                {
                    value = MinVolume;
                }

                if (value > MaxVolume)
                {
                    value = MaxVolume;
                }

                _currentVolume = value / 100f;
            }
        }

        public async Task PlayAsync(IUserMessage msg)
        {
            _tokenSource = new CancellationTokenSource();

            await Task.Factory.StartNew(async () =>
            {
                try
                {
                    await JoinChannelAsync(msg).ConfigureAwait(false);

                    while (true)
                    {
                        _tokenSource.Token.ThrowIfCancellationRequested();

                        if (CurrentSong != null)
                        {
                            LastSong = CurrentSong;
                        }

                        if (_songIndex < 0 || _songIndex >= SongList.Count)
                        {
                            return;
                        }

                        CurrentSong = SongList[_songIndex];

                        State = MusicState.Loading;
                        
                        CurrentSong.TokenSource = new CancellationTokenSource();
                        CurrentSong.StreamUrl = await ResolveStreamFromUrlAsync(CurrentSong.Url);

                        if (CurrentSong.StreamUrl == string.Empty)
                        {
                            Log.Debug($"Couldn't resolve stream url from url: {CurrentSong.Url}");
                            return;
                        }

                        _tokenSource.Token.ThrowIfCancellationRequested();

                        try
                        {

                            //await PlaySong(msg.Channel, CurrentSong);
                            await Task.Factory.StartNew(async () => await TranscodeSong(CurrentSong).ConfigureAwait(false));
                            await PlaySong(msg.Channel, CurrentSong).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException ex)
                        {
                            Log.Info("Song cancelled: " + ex.Message);
                        }
                        catch (Exception ex)
                        {
                            Log.Debug(ex.ToString());
                        }
                        finally
                        {
                            if (State != MusicState.Paused)
                            {
                                if (PlayMode == PlayMode.Queue)
                                {
                                    SongList.Remove(CurrentSong);
                                }
                                else
                                {
                                    if (++_songIndex == SongList.Count)
                                    {
                                        _songIndex = 0;
                                    }
                                }

                                State = MusicState.Stopped;
                            }
                        }
                    }
                }
                finally
                {
                    await LeaveChannelAsync().ConfigureAwait(false);
                }
            }, _tokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private async Task TranscodeSong(Song song)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-i {song.StreamUrl} -ss {song.StartTime.TotalSeconds} -f s16le -ar 48000 -ac 2 pipe:1 -threads 1 -loglevel quiet",
                UseShellExecute = false,
                RedirectStandardOutput = true
            };

            if (!_songIdDictionary.TryGetValue(song.StreamUrl, out var guid))
            {
                guid = Guid.NewGuid();
                _songIdDictionary.TryAdd(song.StreamUrl, guid);
            }

            var directory = Path.Combine(Directory.GetCurrentDirectory(), "temp");
            var outputFile = Path.Combine(directory, $"{guid}.raw");

            // File exists but no way to verify file is not corrupted so delete
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (File.Exists(outputFile))
            {
                File.Delete(outputFile);
            }

            Log.Debug("Ffmpeg started.");

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            // Start writing file, 80kb buffer to ensure we can send enough data without issue
            var buffer = new byte[81920];
            
            using (var outfile = new FileStream(outputFile, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite))
            {
                using (var process = Process.Start(startInfo))
                {
                    if (process == null)
                    {
                        throw new Exception("ffmpeg process could not be created.");
                    }

                    process.PriorityClass = ProcessPriorityClass.BelowNormal;

                    while (!song.TokenSource.Token.IsCancellationRequested)
                    {
                        var byteCount = await process.StandardOutput.BaseStream.ReadAsync(buffer, 0, buffer.Length, song.TokenSource.Token);

                        if (byteCount == 0)
                        {
                            break;
                        }

                        await outfile.WriteAsync(buffer, 0, byteCount, song.TokenSource.Token);
                    }

                }
            }

            stopwatch.Stop();
            Log.Debug($"Ffmpeg complete. Time: {stopwatch.Elapsed}");
        }

        private async Task PlaySong(IMessageChannel msgChannel, Song song)
        {
            Log.Debug("Creating PCM stream.");

            if (!_songIdDictionary.TryGetValue(song.StreamUrl, out var guid))
            {
                // Something went wrong here, guid should've been created.
                throw new Exception("No guid created for Song.");
            }

            var inFile = Path.Combine(Directory.GetCurrentDirectory(), "temp", $"{guid}.raw");


            while (!File.Exists(inFile))
            {
                await Task.Delay(100);
            }

            var buffer = new byte[FrameBytes];
            var retryCount = 0;
            var bytesSent = 0;

            using (var inStream = new FileStream(inFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var pcmStream = _audio.CreatePCMStream(AudioApplication.Music, SamplesPerFrame, 2, null, 2000))
                {
                    State = MusicState.Playing;
                    Log.Debug("Playing song.");
                    await PlayerAsync(msgChannel).ConfigureAwait(false);

                    while (true)
                    {
                        var byteCount = await inStream.ReadAsync(buffer, 0, buffer.Length);

                        if (byteCount == 0)
                        {
                            if (song.Length - song.CurrentTime <= TimeSpan.FromSeconds(1))
                            {
                                Log.Info("Read 0 bytes but song is finished.");
                                break;
                            }

                            await Task.Delay(100);
                            retryCount++;
                        }
                        else if (byteCount != FrameBytes)
                        {
                            Log.Warn($"Read {byteCount} bytes instead of the buffer size. Warning!!!");
                            await Task.Delay(20);
                            retryCount++;
                        }
                        else
                        {
                            retryCount = 0;
                        }

                        if (retryCount == 20)
                        {
                            Log.Warn($"Failed to read from ffmpeg. Retries: {retryCount}");
                            break;
                        }

                        song.TokenSource.Token.ThrowIfCancellationRequested();

                        buffer = AudioHelper.AdjustVolume(buffer, _currentVolume);

                        await pcmStream.WriteAsync(buffer, 0, byteCount, song.TokenSource.Token);
                        bytesSent += byteCount;
                        song.CurrentTime = song.StartTime +
                                           TimeSpan.FromSeconds(bytesSent /
                                                                (1000d * FrameBytes /
                                                                 Milliseconds));
                    }
                }
            }
        }

        public async Task PlayerAsync(IMessageChannel channel)
        {
            await Task.Factory.StartNew(async () =>
            {
                try
                {
                    var song = CurrentSong;
                    if (song == null)
                    {
                        return;
                    }

                    var message = await channel.SendMessageAsync(string.Empty, false, song.GetEmbed("", false, true));

                    while (State == MusicState.Playing)
                    {
                        // Song changed. Stop updating song info.
                        if (CurrentSong.Url != song.Url)
                        {
                            break;
                        }

                        song.TokenSource.Token.ThrowIfCancellationRequested();
                        await message.ModifyAsync(m => m.Embed = song.GetEmbed("", false, true));
                        await Task.Delay(4700);
                    }
                    
                    await message.DeleteAsync();
                }
                catch (OperationCanceledException ex)
                {
                    Log.Debug($"Player task cancelled: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Log.Debug(ex.ToString());
                }
            }, CurrentSong.TokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public bool Stop()
        {
            ClearQueue();

            if (State == MusicState.Playing || State == MusicState.Loading)
            {
                _tokenSource.Cancel();
                CurrentSong.TokenSource.Cancel();
            }

            return true;
        }

        public bool Pause()
        {
            if (State != MusicState.Playing && State != MusicState.Loading)
            {
                return false;
            }

            CurrentSong.StartTime = CurrentSong.CurrentTime;

            _tokenSource.Cancel();

            CurrentSong.TokenSource.Cancel();
            State = MusicState.Paused;

            return true;
        }

        public bool Skip()
        {
            if (State != MusicState.Playing && State != MusicState.Loading)
            {
                return false;
            }

            CurrentSong.TokenSource.Cancel();
            return true;
        }

        public void AddSong(Song song)
        {
            SongList.Add(song);
        }

        public Song RemoveSong(int songNumber)
        {
            if (songNumber < 0 || songNumber > SongList.Count)
            {
                return null;
            }

            if (songNumber == 0)
            {
                Stop();
            }

            var removedSong = SongList[songNumber];
            SongList.Remove(removedSong);

            return removedSong;
        }

        public void ClearQueue()
        {
            SongList.Clear();
        }

        public void ChangePlayMode(PlayMode mode)
        {
            PlayMode = mode;
        }

        private static async Task<string> ResolveStreamFromUrlAsync(string url)
        {
            string streamUrl;

            if (DirectPlayFormats.Contains(url.Split('.').LastOrDefault()))
            {
                streamUrl = url;
            }
            //else if (url.Contains("youtube.com"))
            //{
            //    var videos = await DownloadUrlResolver.GetDownloadUrlsAsync(url);
            //    var video = videos.OrderByDescending(info => info.AudioBitrate).ThenBy(info => info.Resolution).First();
            //    streamUrl = video.DownloadUrl;
            //}
            else
            {
                Log.Debug("Retrieving url using youtube-dl");
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                streamUrl = await GetUrlFromYoutubeDlAsync(url).ConfigureAwait(false);

                stopwatch.Stop();

                if (streamUrl == null)
                {
                    Log.Error("Failed to get url from youtube-dl. Possible update needed.");
                }
                else
                {
                    Log.Debug("Url source found: " + stopwatch.Elapsed);
                }
            }

            return streamUrl;
        }

        private static async Task<string> GetUrlFromYoutubeDlAsync(string url)
        {
            var youtubeDl = new ProcessStartInfo
            {
                FileName = "youtube-dl",
                Arguments = $"{url} -g -f bestaudio --audio-quality 0",
                RedirectStandardOutput = true,
                RedirectStandardError = false,
                CreateNoWindow = true,
                UseShellExecute = false
            };

            var p = Process.Start(youtubeDl);

            if (p == null)
            {
                Log.Error("Unable to create youtube-dl process");
                return null;
            }

            return await p.StandardOutput.ReadLineAsync();
        }

        private async Task JoinChannelAsync(IMessage msg)
        {
            try
            {
                var channel = (msg.Author as IGuildUser)?.VoiceChannel;
                if (channel == null)
                {
                    await msg.Channel.SendMessageAsync("User must be in a voice channel.");
                    return;
                }

                // Get the IAudioClient by calling the JoinAsync method
                _audio = await channel.ConnectAsync();
                Log.Info("Connected to audio channel.");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex);
            }
        }

        private async Task LeaveChannelAsync()
        {
            if (_audio != null && _audio.ConnectionState == ConnectionState.Connected)
            {
                await _audio?.StopAsync();
            }
        }
    }
}
