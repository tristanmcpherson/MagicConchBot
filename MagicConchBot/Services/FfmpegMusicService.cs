using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using log4net;
using MagicConchBot.Common.Enums;
using MagicConchBot.Common.Interfaces;
using MagicConchBot.Common.Types;
using MagicConchBot.Helpers;
using YoutubeExtractor;

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

        private static readonly ILog Log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly string[] DirectPlayFormats = { "webm", "mp3", "avi", "wav", "mp4", "flac" };

        private readonly ConcurrentQueue<Song> _songQueue;
        private IAudioClient _audio;

        private CancellationTokenSource _tokenSource;

        private float _currentVolume = 0.5f;

        public FfmpegMusicService()
        {
            _songQueue = new ConcurrentQueue<Song>();
            PlayMode = PlayMode.Queue;
            CurrentSong = null;
            LastSong = null;
        }

        public PlayMode PlayMode { get; set; }

        public Song LastSong { get; private set; }

        public Song CurrentSong { get; private set; }

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

                _currentVolume = (float)(value / 100m);
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

                    while (!_tokenSource.Token.IsCancellationRequested && !_songQueue.IsEmpty)
                    {
                        if (CurrentSong != null)
                        {
                            LastSong = CurrentSong;
                        }

                        if (!_songQueue.TryPeek(out Song currentSong))
                        {
                            continue;
                        }

                        CurrentSong = currentSong;

                        if (CurrentSong.IsPaused)
                        {
                            CurrentSong.IsPaused = false;
                        }

                        CurrentSong.TokenSource = new CancellationTokenSource();
                        CurrentSong.StreamUrl = await ResolveStreamFromUrlAsync(CurrentSong.Url);

                        if (CurrentSong.StreamUrl == string.Empty)
                        {
                            Log.Debug($"Couldn't resolve stream url from url: {CurrentSong.Url}");
                            return;
                        }

                        var seekArg = (int) CurrentSong.StartTime.TotalSeconds == 0
                            ? string.Empty
                            : $"-ss {CurrentSong.StartTime.TotalSeconds} ";

                        var startInfo = new ProcessStartInfo
                        {
                            FileName = "ffmpeg",
                            Arguments = seekArg + $"-i {CurrentSong.StreamUrl} -f s16le -ar 48000 -ac 2 pipe:1",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = false,
                            CreateNoWindow = true
                        };



                        var bytesSent = 0;
                        var buffer = new byte[FrameBytes];
                        var retryCount = 0;

                        Log.Debug("Creating PCM stream.");

                        using (var process = Process.Start(startInfo))
                        {
                            if (process == null)
                            {
                                throw new Exception("ffmpeg process could not be created.");
                            }

                            try
                            {
                                using (var pcmStream = _audio.CreatePCMStream(SamplesPerFrame, 2, null, 2000))
                                {
                                    Log.Debug("Playing song.");
                                    await PlayerAsync(msg).ConfigureAwait(false);

                                    while (true)
                                    {
                                        var byteCount = await process.StandardOutput.BaseStream.ReadAsync(buffer, 0,
                                            buffer.Length);

                                        if (byteCount == 0)
                                        {
                                            if (process.HasExited)
                                            {
                                                Log.Error("Process exited before stream finished?");
                                            }
                                            else if (process.StandardOutput.EndOfStream)
                                            {
                                                Log.Error("StandardOutput end of stream D:");
                                            }

                                            Log.Warn($"Read 0 bytes. Retrying. Retries at {retryCount}.");
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
                                            Log.Warn($"Failed. Retries: {retryCount}");
                                            break;
                                        }

                                        CurrentSong.TokenSource.Token.ThrowIfCancellationRequested();

                                        buffer = AudioHelper.AdjustVolume(buffer, _currentVolume);

                                        await pcmStream.WriteAsync(buffer, 0, byteCount, CurrentSong.TokenSource.Token);
                                        bytesSent += byteCount;
                                        CurrentSong.CurrentTime = CurrentSong.StartTime +
                                                                  TimeSpan.FromSeconds(bytesSent /
                                                                                       (1000d * FrameBytes /
                                                                                        Milliseconds));
                                    }

                                    process.WaitForExit(3000);
                                }
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
                                if (!CurrentSong.IsPaused)
                                {
                                    if (PlayMode == PlayMode.Queue)
                                    {
                                        _songQueue.TryDequeue(out Song _);
                                    }
                                }

                                CurrentSong = null;
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

        public async Task PlayerAsync(IMessage msg)
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

                    var message = await msg.Channel.SendMessageAsync(string.Empty, false, song.GetEmbed(string.Empty, true, true));

                    while (CurrentSong != null)
                    {
                        // Song changed. Stop updating song info.
                        if (CurrentSong.Url != song.Url)
                        {
                            break;
                        }

                        CurrentSong.TokenSource.Token.ThrowIfCancellationRequested();
                        await message.ModifyAsync(m => m.Embed = song.GetEmbed(string.Empty, true, true));
                        await Task.Delay(2000);
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
            if (CurrentSong != null && !CurrentSong.IsPaused)
            {
                _tokenSource.Cancel();
                CurrentSong.TokenSource.Cancel();
            }
            return true;
        }

        public bool Pause()
        {
            if (CurrentSong == null)
            {
                return false;
            }

            CurrentSong.IsPaused = true;
            CurrentSong.StartTime = CurrentSong.CurrentTime;
            CurrentSong.TokenSource.Cancel();
            _tokenSource.Cancel();
            return true;
        }

        public bool Skip()
        {
            if (CurrentSong == null || _songQueue.Count == 0)
            {
                return false;
            }

            CurrentSong.TokenSource.Cancel();
            return PlayMode == PlayMode.Queue || _songQueue.TryDequeue(out Song _);
        }

        public void QueueSong(Song song)
        {
            _songQueue.Enqueue(song);
        }

        public Song DequeueSong(int songNumber)
        {
            if (songNumber < 0 || songNumber > _songQueue.Count)
            {
                return null;
            }

            if (songNumber == 0)
            {
                Stop();
                return null;
            }

            // They want to remove the currently playing song from queue, also stop playing it
            var stack = new Stack<Song>();
            for (var i = 0; i <= songNumber; i++)
            {
                _songQueue.TryDequeue(out Song song);
                stack.Push(song);
            }

            // Remove last song, aka the song we want to dequeue
            var removedSong = stack.Pop();

            while (stack.Count > 0)
            {
                _songQueue.Enqueue(stack.Pop());
            }

            return removedSong;
        }

        public void ClearQueue()
        {
            while (_songQueue.Count > 0)
            {
                _songQueue.TryDequeue(out Song _);
            }
        }

        public void ChangePlayMode(PlayMode mode)
        {
            PlayMode = mode;
        }

        public List<Song> QueuedSongs()
        {
            return new List<Song>(_songQueue.ToArray());
        }

        private static async Task<string> ResolveStreamFromUrlAsync(string url)
        {
            string streamUrl;

            if (DirectPlayFormats.Contains(url.Split('.').LastOrDefault()))
            {
                streamUrl = url;
            }
            else if (url.Contains("youtube.com"))
            {
                var videos = await DownloadUrlResolver.GetDownloadUrlsAsync(url);
                var video = videos.OrderByDescending(info => info.AudioBitrate).ThenBy(info => info.Resolution).First();
                streamUrl = video.DownloadUrl;
            }
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
            var channel = (msg.Author as IGuildUser)?.VoiceChannel;
            if (channel == null)
            {
                await msg.Channel.SendMessageAsync("User must be in a voice channel.");
                return;
            }

            // Get the IAudioClient by calling the JoinAsync method
            _audio = await channel.ConnectAsync();
        }

        private async Task LeaveChannelAsync()
        {
            if (_audio != null && _audio.ConnectionState == ConnectionState.Connected)
            {
                await _audio?.DisconnectAsync();
            }
        }
    }
}
