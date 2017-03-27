using System;
using System.Diagnostics;
using System.IO;
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

        private const int SampleFrequency = 48000;
        private const int Milliseconds = 20;
        private const int SamplesPerFrame = SampleFrequency * Milliseconds / 1000;
        private const int FrameBytes = 3840; // 2 channel, 16 bit

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
            _song = song;
            //if (!_songIdDictionary.TryGetValue(song.Url, out var guid))
            if (audio.ConnectionState != ConnectionState.Connected)
            {
                return;
            }

            AudioState = AudioState.Loading;

            var inFile = await _fileProvider.GetStreamingFile(song);
            var waitCount = 0;
            
            while (true)
            {
                var info = new FileInfo(inFile);
                if (info.Exists && info.Length != 0)
                    break;
                if (++waitCount == 20)
                    return;
                await Task.Delay(100, song.TokenSource.Token);
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments =
                    $"-re -i \"{inFile}\" -ss {song.StartTime.TotalSeconds} -f s16le -ar 48000 -ac 2 -loglevel quiet pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true
            };

            var buffer = new byte[FrameBytes];
            var retryCount = 0;
            var bytesSent = 0;

            using (var process = Process.Start(startInfo))
            {
                if (process == null)
                {
                    Log.Error("Failed to created ffmpeg process.");
                    return;
                }

                Log.Debug($"Creating PCM stream for file {inFile}");

                using (var pcmStream = audio.CreatePCMStream(AudioApplication.Music, SamplesPerFrame))
                {
                    AudioState = AudioState.Playing;
                    Log.Debug("Playing song.");

                    while (!song.TokenSource.IsCancellationRequested)
                    {
                        var byteCount = await process.StandardOutput.BaseStream.ReadAsync(buffer, 0, buffer.Length);

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
