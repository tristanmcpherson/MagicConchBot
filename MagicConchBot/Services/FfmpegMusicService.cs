namespace MagicConchBot.Services
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    using Discord;
    using Discord.Audio;

    using log4net;

    using MagicConchBot.Common.Enums;
    using MagicConchBot.Common.Interfaces;
    using MagicConchBot.Common.Types;
    using MagicConchBot.Helpers;
    using MagicConchBot.Resources;

    using YoutubeExtractor;

    public class FfmpegMusicService : IMusicService
    {
        private const int SampleFrequency = 48000;
        private const int Milliseconds = 5;
        private const int SamplesPerFrame = SampleFrequency * Milliseconds / 1000;
        private const int FrameBytes = 960; // 2 channel, 16 bit

        private const int MaxVolume = 100;
        private const int MinVolume = 0;

        private static readonly ILog Log =
            LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly string[] DirectPlayFormats = { "webm", "mp3", "avi", "wav", "mp4", "flac" };

        private static string serverPath;
        private static string serverUrl;
        private static bool generatingMp3;

        private readonly ConcurrentDictionary<string, Guid> urlToUniqueFile;
        private readonly ConcurrentQueue<Song> songQueue;
        private IAudioClient audio;

        private PlayMode playMode;

        private CancellationTokenSource tokenSource;

        private Song lastSong;
        private Song currentSong;

        private float currentVolume = 0.4f;

        public FfmpegMusicService()
        {
            var config = Configuration.Load();

            songQueue = new ConcurrentQueue<Song>();
            playMode = PlayMode.Queue;
            urlToUniqueFile = new ConcurrentDictionary<string, Guid>();
            serverPath = config.ServerMusicPath;
            serverUrl = config.ServerMusicUrlBase;
            currentSong = null;
            lastSong = null;
        }

        public int Volume => (int)(currentVolume * 100);

        public async Task<string> GenerateMp3Async()
        {
            if (generatingMp3)
            {
                return null;
            }

            return await Task.Factory.StartNew(() =>
            {
                // ffmpeg -i input.wav -vn -ar 44100 -ac 2 -ab 192k -f mp3 output.mp3
                var songToDownload = currentSong ?? lastSong;
                if (songToDownload == null)
                {
                    return null;
                }

                if (!urlToUniqueFile.TryGetValue(songToDownload.StreamUrl, out var guid))
                {
                    guid = Guid.NewGuid();
                    urlToUniqueFile.TryAdd(songToDownload.StreamUrl, guid);
                }

                var outputFile = songToDownload.Name + "_" + guid.ToString() + ".mp3";
                var downloadFile = outputFile + ".raw";

                var outputUrl = serverUrl + Uri.EscapeDataString(outputFile);
                var destinationPath = Path.Combine(serverPath, outputFile);

                if (File.Exists(destinationPath))
                {
                    return outputUrl;
                }

                generatingMp3 = true;

                using (var webClient = new WebClient())
                {
                    webClient.DownloadFile(songToDownload.StreamUrl, downloadFile);
                }

                var convert = Process.Start(new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $@"-i ""{downloadFile}"" -vn -ar 44100 -ac 2 -ab 320k -f mp3 ""{outputFile}""",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = false,
                    CreateNoWindow = true
                });

                if (convert == null)
                {
                    Log.Error("Couldn't start ffmpeg process.");
                    return null;
                }

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
                File.Delete(downloadFile);

                generatingMp3 = false;

                return outputUrl;
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public async Task PlayAsync(IUserMessage msg)
        {
            tokenSource = new CancellationTokenSource();

            await Task.Factory.StartNew(async () =>
            {
                try
                {
                    await JoinChannelAsync(msg).ConfigureAwait(false);

                    while (!tokenSource.Token.IsCancellationRequested && !songQueue.IsEmpty)
                    {
                        if (currentSong != null)
                        {
                            lastSong = currentSong;
                        }

                        if (!songQueue.TryPeek(out currentSong))
                        {
                            continue;
                        }

                        if (currentSong.IsPaused)
                        {
                            currentSong.IsPaused = false;
                        }

                        currentSong.TokenSource = new CancellationTokenSource();
                        currentSong.StreamUrl = await ResolveStreamFromUrlAsync(currentSong.Url);

                        if (currentSong.StreamUrl == "")
                        {
                            Log.Debug($"Couldn't resolve stream url from url: {currentSong.Url}");
                            return;
                        }

                        var seekArg = (int)currentSong.SeekTo.TotalSeconds == 0
                            ? ""
                            : $"-ss {currentSong.SeekTo.TotalSeconds} ";

                        var startInfo = new ProcessStartInfo
                        {
                            FileName = "ffmpeg",
                            Arguments = seekArg + $"-i {currentSong.StreamUrl} -f s16le -ar 48000 -ac 2 pipe:1",
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
                            using (var pcmStream = audio.CreatePCMStream(SamplesPerFrame))
                            {
                                Log.Debug("Playing song.");
                                await PlayerAsync(msg).ConfigureAwait(false);

                                while (true)
                                {
                                    if (process == null)
                                    {
                                        throw new Exception("ffmpeg process could not be created.");
                                    }

                                    var byteCount = await process.StandardOutput.BaseStream.ReadAsync(buffer, 0, FrameBytes,
                                        currentSong.TokenSource.Token);

                                    if (byteCount == 0)
                                    {
                                        await Task.Delay(250).ConfigureAwait(false);
                                        retryCount++;
                                        Log.Warn($"Retrying. Retries: {retryCount}");
                                    }

                                    if (retryCount == 5)
                                    {
                                        break;
                                    }

                                    currentSong.TokenSource.Token.ThrowIfCancellationRequested();

                                    buffer = AudioHelper.AdjustVolume(buffer, currentVolume);

                                    await pcmStream.WriteAsync(buffer, 0, byteCount, currentSong.TokenSource.Token).ConfigureAwait(false);
                                    bytesSent += byteCount;
                                    currentSong.CurrentTime = currentSong.SeekTo +
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
                            if (!currentSong.IsPaused)
                            {
                                if (playMode == PlayMode.Queue)
                                {
                                    songQueue.TryDequeue(out var _);
                                }
                            }

                            currentSong = null;
                        }
                    }
                }
                finally
                {
                    await LeaveChannelAsync().ConfigureAwait(false);
                }
            }, tokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public async Task PlayerAsync(IMessage msg)
        {
            await Task.Factory.StartNew(async () =>
            {
                try
                {
                    var song = currentSong;
                    if (song == null)
                    {
                        return;
                    }

                    var message = await msg.Channel.SendMessageAsync("", false, song.GetEmbed("", true, true));

                    while (currentSong != null)
                    {
                        // Song changed. Stop updating song info.
                        if (currentSong.Url != song.Url)
                        {
                            break;
                        }

                        currentSong.TokenSource.Token.ThrowIfCancellationRequested();
                        await message.ModifyAsync(m => m.Embed = song.GetEmbed("", true, true));
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
            }, currentSong.TokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public bool Stop()
        {
            ClearQueue();
            tokenSource.Cancel();
            currentSong?.TokenSource.Cancel();
            return true;
        }

        public bool Pause()
        {
            if (currentSong == null)
            {
                return false;
            }

            currentSong.IsPaused = true;
            currentSong.SeekTo = currentSong.CurrentTime;
            currentSong.TokenSource.Cancel();
            tokenSource.Cancel();
            return true;
        }

        public bool Skip()
        {
            if (currentSong == null || songQueue.Count == 0)
            {
                return false;
            }

            currentSong.TokenSource.Cancel();
            return playMode == PlayMode.Queue || songQueue.TryDequeue(out var _);
        }

        public void QueueSong(Song song)
        {
            songQueue.Enqueue(song);
        }

        public Song DequeueSong(int songNumber)
        {
            if (songNumber < 0 || songNumber > songQueue.Count)
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
                songQueue.TryDequeue(out var song);
                stack.Push(song);
            }

            // Remove last song, aka the song we want to dequeue
            var removedSong = stack.Pop();

            while (stack.Count > 0)
            {
                songQueue.Enqueue(stack.Pop());
            }

            return removedSong;
        }

        public void ClearQueue()
        {
            while (songQueue.Count > 0)
            {
                songQueue.TryDequeue(out var _);
            }
        }

        public void ChangePlayMode(PlayMode mode)
        {
            playMode = mode;
        }

        public int ChangeVolume(int volume)
        {
            if (volume < MinVolume)
            {
                volume = MinVolume;
            }

            if (volume > MaxVolume)
            {
                volume = MaxVolume;
            }

            currentVolume = (float) (volume / 100m);
            return volume;
        }

        public Song GetCurrentSong()
        {
            return currentSong;
        }

        public List<Song> QueuedSongs()
        {
            return new List<Song>(songQueue.ToArray());
        }

        private static async Task<string> ResolveStreamFromUrlAsync(string url)
        {
            string streamUrl;

            if (DirectPlayFormats.Contains(url.Split('.').LastOrDefault()))
            {
                streamUrl = url;
            }
            else if (url.Contains("youtube"))
            {
                var videos = await DownloadUrlResolver.GetDownloadUrlsAsync(url);
                var video = videos
                    .OrderByDescending(info => info.AudioBitrate)
                    .ThenBy(info => info.Resolution)
                    .First();
                streamUrl = video.DownloadUrl;
            }
            else
            {
                Log.Debug("Retrieving url using youtube-dl");
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                streamUrl = await GetUrlFromYoutubeDlAsync(url).ConfigureAwait(false);

                stopwatch.Stop();
                Log.Debug("Url source found: " + stopwatch.Elapsed);
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

            if (p != null)
            {
                return await p.StandardOutput.ReadLineAsync();
            }

            Console.WriteLine("Unable to create youtube-dl process");
            return null;
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
            audio = await channel.ConnectAsync();
        }

        private async Task LeaveChannelAsync()
        {
            if (audio != null && audio.ConnectionState == ConnectionState.Connected)
            {
                await audio?.DisconnectAsync();
            }
        }
    }
}
