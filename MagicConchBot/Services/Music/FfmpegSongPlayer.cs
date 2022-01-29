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

namespace MagicConchBot.Services.Music {
	public class FfmpegSongPlayer : ISongPlayer {
		private static readonly Logger Log = LogManager.GetCurrentClassLogger();

		private const int Milliseconds = 20;
		private const int FrameSize = 3840;
		private const int MaxRetryCount = 50;

		private const float MaxVolume = 1f;
		private const float MinVolume = 0f;

		private float _currentVolume = 0.5f;

		private Song _song;

        private CancellationTokenSource tokenSource;

        public float GetVolume() {
            return _currentVolume;
        }

        public void SetVolume(float value) {
            _currentVolume = Math.Clamp(value, MinVolume, MaxVolume);
        }

        private static TimeSpan CalculateCurrentTime(int currentBytes) {
            return TimeSpan.FromSeconds(currentBytes /
                                        (1000d * FrameSize /
                                         Milliseconds));
        }

        public PlayerState PlayerState { get; private set; } = PlayerState.Stopped;

		public async Task PlaySong(IAudioClient audio, Song song) {
			if (audio == null || audio.ConnectionState != ConnectionState.Connected) {
				return;
			}

            tokenSource = new CancellationTokenSource();
            _song = song;

			PlayerState = PlayerState.Loading;

			try {
			    var inStream = StartFfmpeg(song.StreamUri, song);
                if (inStream == null) {
                    throw new Exception("FFMPEG stream was not created.");
                }

                Log.Debug($"Creating PCM stream for file {song.Name}.");

				var buffer = new byte[FrameSize];
				var retryCount = 0;
                using (var pcmStream = audio.CreatePCMStream(AudioApplication.Music, packetLoss: 0)) {
                    PlayerState = PlayerState.Playing;
                    Log.Debug("Playing song.");
                    song.CurrentTime = song.StartTime;

                    while (!tokenSource.IsCancellationRequested) {
                        var byteCount = await inStream.ReadAsync(buffer.AsMemory(0, buffer.Length), tokenSource.Token);

                        if (byteCount == 0) {
                            if (song.Length != TimeSpan.Zero && song.Length - song.CurrentTime <= TimeSpan.FromMilliseconds(1000)) {
                                Log.Debug("Read 0 bytes but s5ong is finished.");
                                break;
                            }

                            await Task.Delay(100, tokenSource.Token).ConfigureAwait(false);

                            if (++retryCount == MaxRetryCount) {
                                Log.Warn($"Failed to read from ffmpeg. Retries: {retryCount}");
                                break;
                            }
                        } else {
                            retryCount = 0;
                        }

                        buffer = AudioHelper.ChangeVol(buffer, _currentVolume);

                        if (pcmStream.CanWrite)
                        {
                            await pcmStream.WriteAsync(buffer.AsMemory(0, byteCount));
                        }

                        song.CurrentTime += CalculateCurrentTime(byteCount);
                    }
                    await pcmStream.FlushAsync();
                }

            } catch (OperationCanceledException ex) {
				Log.Info("Song cancelled: " + ex.Message);
			} catch (Exception ex) {
				Log.Error(ex);
			} finally {
				PlayerState = PlayerState == PlayerState.PauseRequested ? PlayerState.Paused : PlayerState.Stopped;
				if (!tokenSource.Token.IsCancellationRequested) { 
                    tokenSource.Cancel(); 
                }
			}
		}

        private static Stream StartFfmpeg(string inputFile, Song song) {
            try {
                //var oldArguments =
                //    $"-v 9 -re -i \"{inputFile}\" -ss {song.StartTime.TotalSeconds} -f s16le -acodec pcm_s16le -ar 48000 \"{outputFile}\"";
                // -ac 2 -f s16le -ar 48000
                var seek = song.StartTime.TotalSeconds > 0 ? $"-ss {song.StartTime.TotalSeconds}" : "";
                var arguments = $"-reconnect 1 -reconnect_streamed 1 -reconnect_delay_max 5 -err_detect ignore_err -i \"{inputFile}\" {seek} -ac 2 -f s16le -vn -ar 48000 pipe:1 -loglevel error";

                //Log.Debug(arguments);

                Log.Info(Directory.GetCurrentDirectory());

                if (!File.Exists("ffmpeg") && !File.Exists("ffmpeg.exe")) {
                    Log.Error("FFMPEG not found.");
                    throw new Exception();
                }

                var startInfo = new ProcessStartInfo {
                    FileName = "ffmpeg",
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                };
                var process = new Process()
                {
                    StartInfo = startInfo,

                };

                process.ErrorDataReceived += (sender, data) => {
                    Log.Error(data.Data);
                };

                try
                {
                    process.Start();
                    process.BeginErrorReadLine();
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }

                if (process == null) {
                    
                    throw new Exception("Could not start FFMPEG");
                }

                
                return process.StandardOutput.BaseStream;

            } catch (Exception ex) {
                if (ex.InnerException != null)
                {
                    Log.Error(ex.InnerException);
                }

                if (ex.InnerException?.InnerException != null)
                {
                    Log.Error(ex.InnerException.InnerException);
                }

                Log.Warn(ex, "FFMPEG IO EXCEPTION");
            }

            Log.Debug("ffmpeg exited.");

            return null;
        }

        public void Stop() {
			tokenSource?.Cancel();
		}

		public void Pause() {
			PlayerState = PlayerState.PauseRequested;
            tokenSource?.Cancel();

            _song.StartTime = _song.CurrentTime;
		}
	}
}
