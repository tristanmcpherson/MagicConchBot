using System;
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

namespace MagicConchBot.Services.Music
{
    public class FfmpegSongPlayer : ISongPlayer
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        
        private const int Milliseconds = 20;

        private const int MaxVolume = 1;
        private const int MinVolume = 0;

        private float _currentVolume = 0.5f;
        
        private readonly IFileProvider _fileProvider;
        private Song _song;

        public float Volume
        {
            get => _currentVolume;

            set
            {
                if (value < MinVolume)
                    value = MinVolume;

                if (value > MaxVolume)
                    value = MaxVolume;

                _currentVolume = value;
            }
        }

        public AudioState AudioState { get; private set; } = AudioState.Stopped;

        public FfmpegSongPlayer(IFileProvider fileProvider)
        {
            _fileProvider = fileProvider;
        }

        public async Task PlaySong(IAudioClient audio, Song song)
        {
            if (audio == null || audio.ConnectionState != ConnectionState.Connected)
            {
                return;
            }

            _song = song;

            AudioState = AudioState.Loading;

            try
            {
                var directory = Path.Combine(Directory.GetCurrentDirectory(), "temp");
                var outputFile = Path.Combine(directory, $"{Guid.NewGuid()}.raw");
                Directory.CreateDirectory(directory);

                var ffmpegTask = new Task(async () => await StartFfmpeg(song.StreamUri, outputFile, song), TaskCreationOptions.LongRunning);
                ffmpegTask.Start();

                await FileHelper.WaitForFile(outputFile, 3840, song.Token, -1);
                
                Log.Debug($"Creating PCM stream for file {song.StreamUri}");

                var buffer = new byte[3840];
                var retryCount = 0;
                var stopwatch = new Stopwatch();

                using (var inStream = new FileStream(outputFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (var pcmStream = audio.CreatePCMStream(AudioApplication.Music))
                    {
                        AudioState = AudioState.Playing;
                        Log.Debug("Playing song.");
                        song.CurrentTime = song.StartTime;

                        stopwatch.Start();
                        while (!song.TokenSource.IsCancellationRequested)
                        {
                            var byteCount = await inStream.ReadAsync(buffer, 0, buffer.Length, song.Token);

                            if (byteCount == 0)
                            {
                                if (song.Length != TimeSpan.Zero && song.Length - song.CurrentTime <= TimeSpan.FromMilliseconds(50))
                                {
                                    Log.Info("Read 0 bytes but song is finished.");
                                    break;
                                }

                                await Task.Delay(100, song.Token).ConfigureAwait(false);

                                if (++retryCount == 50)
                                {
                                    Log.Warn($"Failed to read from ffmpeg. Retries: {retryCount}");
                                    break;
                                }
                            }
                            else
                            {
                                retryCount = 0;
                            }

                            song.Token.ThrowIfCancellationRequested();

                            buffer = AudioHelper.ChangeVol(buffer, _currentVolume);
                            await pcmStream.WriteAsync(buffer, 0, byteCount, song.Token);
                            song.CurrentTime += CalculateCurrentTime(byteCount);
                        }
                        await pcmStream.FlushAsync();
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                Log.Info("Song cancelled: " + ex.Message);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
            finally
            {
                if (AudioState != AudioState.Paused)
                {
                    AudioState = AudioState.Stopped;
                }
            }
        }

        private static async Task StartFfmpeg(string inputFile, string outputFile, Song song)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments =
                        $"-re -i \"{inputFile}\" -ss {song.StartTime.TotalSeconds} -f s16le -ar 48000 -acodec pcm_s16le -loglevel quiet \"{outputFile}\"",
                    RedirectStandardOutput = false,
                    RedirectStandardInput = true,
                    UseShellExecute = false
                };

                if (File.Exists(outputFile))
                {
                    File.Delete(outputFile);
                }

                var p = Process.Start(startInfo);

                while (!song.Token.IsCancellationRequested && !p.HasExited)
                {
                    await Task.Delay(100);
                }

                if (!p.HasExited)
                {
                    await p.StandardInput.WriteLineAsync('q');
                }

                p.WaitForExit();
            }
            catch (IOException)
            {
            }
            Log.Debug("ffmpeg exited.");
        }

        public void Stop()
        {
            _song.TokenSource.Cancel();
        }

        public void Pause()
        {
            AudioState = AudioState.Paused;
            _song.StartTime = _song.CurrentTime;
            _song.TokenSource.Cancel();
        }

        private static TimeSpan CalculateCurrentTime(int currentBytes)
        {
            return TimeSpan.FromSeconds(currentBytes /
                                        (1000d * 3840 /
                                         Milliseconds));
        }
    }
}
