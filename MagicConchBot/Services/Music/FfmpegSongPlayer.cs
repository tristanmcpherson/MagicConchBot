using System;
using System.Diagnostics;
using System.IO;
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
                var uri = new Uri(song.StreamUri).IsFile;
                var inFile = uri ? song.StreamUri : await _fileProvider.GetStreamingFile(song);

                var waitCount = 0;

                while (true)
                {
                    var info = new FileInfo(inFile);
                    if (info.Exists && info.Length >= 4096)
                        break;

                    if (++waitCount == 20)
                        throw new Exception("Streaming file took too long to download. Stopping.");
                    
                    await Task.Delay(100, song.Token);
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $"-re -i \"{inFile}\" -ss {song.StartTime.TotalSeconds} -f s16le -ac 2 -ar 48000 -loglevel quiet pipe:1",
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };

                var buffer = new byte[4096];
                var retryCount = 0;
                var bytesSent = 0;

                using (var process = Process.Start(startInfo))
                {
                    if (process == null)
                    {
                        throw new Exception("Failed to created ffmpeg process.");
                    }

                    Log.Debug($"Creating PCM stream for file {inFile}");

                    using (var pcmStream = audio.CreatePCMStream(AudioApplication.Music))
                    {
                        AudioState = AudioState.Playing;
                        Log.Debug("Playing song.");

                        while (!song.TokenSource.IsCancellationRequested)
                        {
                            var byteCount = await process.StandardOutput.BaseStream.ReadAsync(buffer, 0, buffer.Length, song.Token);

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
                            else
                            {
                                retryCount = 0;
                            }

                            if (retryCount == 50)
                            {
                                Log.Warn($"Failed to read from ffmpeg. Retries: {retryCount}");
                                break;
                            }

                            song.Token.ThrowIfCancellationRequested();

                            buffer = AudioHelper.AdjustVolume(buffer, _currentVolume);

                            await pcmStream.WriteAsync(buffer, 0, byteCount, song.Token);
                            bytesSent += byteCount;
                            song.CurrentTime = song.StartTime +
                                               TimeSpan.FromSeconds(bytesSent /
                                                                    (1000d * 4096 /
                                                                     Milliseconds));
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
    }
}
