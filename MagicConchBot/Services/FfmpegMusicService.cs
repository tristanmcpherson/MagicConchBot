using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
using System.IO;
using System.Net;
using System.Text;
using MagicConchBot.Resources;

namespace MagicConchBot.Services
{
    public class FfmpegMusicService : IMusicService
    {
        public bool debug = true;

        private IAudioClient _audio;
        private readonly ConcurrentQueue<Song> _songQueue;

        private static string _serverPath;
        private static string _serverUrl;

        private ConcurrentDictionary<string, Guid> _urlToUniqueFile;

        private static bool generatingMp3;

        private CancellationTokenSource _tokenSource;

        private static readonly ILog Log =
            LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private Song _currentSong;

        private const int SampleFrequency = 48000;
        private const int Milliseconds = 5;
        private const int SamplesPerFrame = SampleFrequency * Milliseconds / 1000;
        private const int FrameBytes = 960; // 2 channel, 16 bit

        private const int MaxVolume = 100;
        private const int MinVolume = 0;

        private float _currentVolume = 0.4f;

        private PlayMode _playMode;

        private List<string> _directPlayFormats = new List<string>
        {
            "webm",
            "mp3",
            "avi",
            "wav"
        };

        public FfmpegMusicService()
        {
            var config = Configuration.Load();

            _songQueue = new ConcurrentQueue<Song>();
            _playMode = PlayMode.Queue;
            _urlToUniqueFile = new ConcurrentDictionary<string, Guid>();
            _serverPath = config.ServerMusicPath;
            _serverUrl = config.ServerMusicUrlBase;
        }

        public async Task<string> GenerateMp3Async()
        {
            if (generatingMp3)
            {
                return null;
            }

            return await Task.Factory.StartNew(() =>
            {
                // ffmpeg -i input.wav -vn -ar 44100 -ac 2 -ab 192k -f mp3 output.mp3
                if (!_urlToUniqueFile.TryGetValue(_currentSong.DirectUrl, out var guid))
                {
                    guid = Guid.NewGuid();
                    _urlToUniqueFile.TryAdd(_currentSong.DirectUrl, guid);
                }

                var outputFile = _currentSong.Name + "_" + guid.ToString() + ".mp3";
                var outputUrl = _serverUrl + WebUtility.UrlEncode(outputFile);
                var destinationPath = Path.Combine(_serverPath, outputFile);

                if (File.Exists(destinationPath))
                {
                    return outputUrl;
                }

                generatingMp3 = true;

                var convert = Process.Start(new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $"-i {_currentSong.DirectUrl} -vn -ar 44100 -ac 2 -ab 192k -f mp3 {outputFile}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = false,
                    CreateNoWindow = true
                });

                convert.StandardOutput.ReadToEnd();
                convert.WaitForExit();

                using (var source = File.OpenRead(outputFile))
                {
                    using (var destination = File.Create(destinationPath))
                    {
                        source.CopyTo(destination);
                    }
                }

                File.Delete(outputFile);

                generatingMp3 = false;

                return outputUrl;
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
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
                await _audio?.DisconnectAsync();
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
                        if (!_songQueue.TryPeek(out _currentSong))
                            continue;
                        _currentSong.TokenSource = new CancellationTokenSource();

                        string url = null;
                        if (_directPlayFormats.Contains(_currentSong.Url.Split('.').LastOrDefault()))
                        {
                            url = _currentSong.Url;
                        }
                        else if (_currentSong.Url.Contains("youtube"))
                        {
                            var video = DownloadUrlResolver.GetDownloadUrls(_currentSong.Url)
                                .OrderByDescending(info => info.AudioBitrate)
                                .ThenBy(info => info.Resolution)
                                .First();
                            url = video.DownloadUrl;
                        }
                        else
                        {
                            Log.Debug("Retrieving url using youtube-dl");
                            var stopwatch = new Stopwatch();
                            stopwatch.Start();

                            url = await GetUrlFromYoutubeDlAsync().ConfigureAwait(false);

                            stopwatch.Stop();
                            Log.Debug("Url source found: " + stopwatch.Elapsed);
                        }

                        if (url == null)
                        {
                            return;
                        }

                        _currentSong.DirectUrl = url;

                        var ffmpegArguments = (int)_currentSong.SeekTo.TotalSeconds == 0
                            ? ""
                            : $"-ss {_currentSong.SeekTo.TotalSeconds} ";

                        var startInfo = new ProcessStartInfo
                        {
                            FileName = "ffmpeg",
                            Arguments = ffmpegArguments + $"-i {url} -f s16le -ar 48000 -ac 2 pipe:1",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = false,
                            CreateNoWindow = true
                        };

                        var process = Process.Start(startInfo);

                        var bytesSent = 0;
                        var buffer = new byte[FrameBytes];
                        var retryCount = 0;

                        Log.Debug("Creating PCM stream.");

                        try
                        {
                            using (var pcmStream = _audio.CreatePCMStream(SamplesPerFrame))
                            {
                                Log.Debug("Playing song.");
                                await PlayerAsync(msg).ConfigureAwait(false);

                                while (true)
                                {
                                    if (process == null)
                                        throw new Exception("ffmpeg process could not be created.");

                                    var byteCount = await process.StandardOutput.BaseStream.ReadAsync(buffer, 0, FrameBytes,
                                        _currentSong.TokenSource.Token);

                                    if (byteCount == 0)
                                    {
                                        await Task.Delay(250).ConfigureAwait(false);
                                        retryCount++;
                                        Log.Warn($"Retrying. Retries: {retryCount}");
                                    }

                                    if (retryCount == 5)
                                        break;

                                    _currentSong.TokenSource.Token.ThrowIfCancellationRequested();

                                    buffer = AudioHelper.AdjustVolume(buffer, _currentVolume);

                                    await pcmStream.WriteAsync(buffer, 0, byteCount, _currentSong.TokenSource.Token).ConfigureAwait(false);
                                    bytesSent += byteCount;
                                    _currentSong.CurrentTime = _currentSong.SeekTo +
                                                                TimeSpan.FromSeconds(bytesSent /
                                                                                    (1000d * FrameBytes / Milliseconds));
                                }
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
                            if (!_currentSong.IsPaused)
                            {
                                if (_playMode == PlayMode.Queue)
                                {
                                    _songQueue.TryDequeue(out var _);
                                }
                            }
                            _currentSong = null;
                        }
                    }
                }
                finally
                {
                    await LeaveChannelAsync().ConfigureAwait(false);
                }
            }, _tokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private async Task<string> GetUrlFromYoutubeDlAsync()
        {
            var youtubeDl = new ProcessStartInfo
            {
                FileName = "youtube-dl",
                // 
                Arguments = $"{_currentSong.Url} -g -f bestaudio --audio-quality 0",
                RedirectStandardOutput = true,
                RedirectStandardError = false,
                CreateNoWindow = true,
                UseShellExecute = false
            };

            var p = Process.Start(youtubeDl);

            if (youtubeDl == null)
            {
                Console.WriteLine("Unable to create youtube-dl process");
                return null;
            }

            return await p.StandardOutput.ReadLineAsync();
        }

        public async Task PlayerAsync(IMessage msg)
        {
            await Task.Factory.StartNew(async () =>
            {
                try
                {
                    var song = _currentSong;
                    if (song == null)
                    {
                        return;
                    }

                    var message = await msg.Channel.SendMessageAsync("", false, song.GetEmbed("", false));

                    while (_currentSong != null)
                    {
                        // Song changed. Stop updating song info.
                        if (_currentSong.Url != song.Url)
                        {
                            break;
                        }

                        _currentSong.TokenSource.Token.ThrowIfCancellationRequested();
                        await message.ModifyAsync(m => m.Embed = song.GetEmbed("", false));
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
            }, _currentSong.TokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public bool Stop()
        {
            if (_currentSong == null)
                return false;
            _tokenSource.Cancel();
            _currentSong.TokenSource.Cancel();
            return true;
        }

        public bool Pause()
        {
            if (_currentSong == null)
                return false;
            _currentSong.IsPaused = true;
            _currentSong.SeekTo = _currentSong.CurrentTime;
            _currentSong.TokenSource.Cancel();
            _tokenSource.Cancel();
            return true;
        }

        public bool Skip()
        {
            if (_currentSong == null || _songQueue.Count <= 0)
            {
                return false;
            }

            _currentSong.TokenSource.Cancel();
            return _playMode == PlayMode.Queue || _songQueue.TryDequeue(out var _);
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
            }

            // They want to remove the currently playing song from queue, also stop playing it

            var stack = new Stack<Song>();
            for (var i = 0; i <= songNumber; i++)
            {
                _songQueue.TryDequeue(out var song);
                stack.Push(song);
            }

            // Remove last song, aka the song we want to dequeue
            var removedSong = stack.Pop();

            while (stack.Count > 0)
                _songQueue.Enqueue(stack.Pop());

            return removedSong;
        }

        public void ClearQueue()
        {
            while (_songQueue.Count > 0)
                _songQueue.TryDequeue(out var _);
        }

        public void ChangePlayMode(PlayMode mode)
        {
            _playMode = mode;
        }

        public int ChangeVolume(int volume)
        {
            if (volume < MinVolume)
                volume = MinVolume;
            if (volume > MaxVolume)
                volume = MaxVolume;
            _currentVolume = (float) (volume / 100m);
            return volume;
        }

        public Song GetCurrentSong()
        {
            return _currentSong;
        }

        public List<Song> QueuedSongs()
        {
            return new List<Song>(_songQueue.ToArray());
        }
    }
}
