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

namespace MagicConchBot.Services.Music {
	public class FfmpegSongPlayer : ISongPlayer {
		private static readonly Logger Log = LogManager.GetCurrentClassLogger();

		private const int Milliseconds = 20;
		private const int FrameSize = 1920;
		private const int MaxRetryCount = 50;

		private const int MaxVolume = 1;
		private const int MinVolume = 0;

		private float _currentVolume = 0.5f;

		private Song _song;

		private bool _pauseRequested;

		public float Volume {
			get => _currentVolume;

			set {
				if (value < MinVolume)
					value = MinVolume;

				if (value > MaxVolume)
					value = MaxVolume;

				_currentVolume = value;
			}
		}

		public PlayerState PlayerState { get; private set; } = PlayerState.Stopped;

		public async Task PlaySong(IAudioClient audio, Song song) {
			if (audio == null || audio.ConnectionState != ConnectionState.Connected) {
				return;
			}

			_song = song;

			PlayerState = PlayerState.Loading;

			try {
				var directory = Path.Combine(Directory.GetCurrentDirectory(), "temp");
				//var outputFile = Path.Combine(directory, $"{Guid.NewGuid()}.raw");
				Directory.CreateDirectory(directory);

                //var ffmpegTask = new Task(async () => , TaskCreationOptions.LongRunning);
			    //ffmpegTask.Start();
			    var inStream = StartFfmpeg(song.StreamUri, song);

                //await FileHelper.WaitForFile(outputFile, FrameSize, song.Token, -1);

                Log.Debug($"Creating PCM stream for file {song.StreamUri}.");

				var buffer = new byte[FrameSize];
				var retryCount = 0;
				var stopwatch = new Stopwatch();

				//using (var inStream = new FileStream(outputFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
					using (var pcmStream = audio.CreatePCMStream(AudioApplication.Music)) {
						PlayerState = PlayerState.Playing;
						Log.Debug("Playing song.");
						song.CurrentTime = song.StartTime;

						stopwatch.Start();
						while (!song.TokenSource.IsCancellationRequested) {
							var byteCount = await inStream.ReadAsync(buffer, 0, buffer.Length, song.Token);

							if (byteCount == 0) {
								if (song.Length != TimeSpan.Zero && song.Length - song.CurrentTime <= TimeSpan.FromMilliseconds(1000)) {
									Log.Info("Read 0 bytes but song is finished.");
									break;
								}

								await Task.Delay(100, song.Token).ConfigureAwait(false);

								if (++retryCount == MaxRetryCount) {
									Log.Warn($"Failed to read from ffmpeg. Retries: {retryCount}");
									break;
								}
							} else {
								retryCount = 0;
							}

							song.Token.ThrowIfCancellationRequested();
							buffer = AudioHelper.ChangeVol(buffer, _currentVolume);
							if (stopwatch.ElapsedMilliseconds < Milliseconds) {
								//await Task.Delay((int)((Milliseconds - (int)stopwatch.ElapsedMilliseconds) * 0.5));
							}
							stopwatch.Restart();

							await pcmStream.WriteAsync(buffer, 0, byteCount, song.Token);
							song.CurrentTime += CalculateCurrentTime(byteCount);
						}
						await pcmStream.FlushAsync();
					}
				//}

				//ffmpegTask.Wait(song.Token);
			} catch (OperationCanceledException ex) {
				Log.Info("Song cancelled: " + ex.Message);
			} catch (Exception ex) {
				Log.Error(ex);
			} finally {
				PlayerState = _pauseRequested ? PlayerState.Paused : PlayerState.Stopped;
				_pauseRequested = false;
			}
		}

        private static Stream StartFfmpeg(string inputFile, Song song) {
            try {
                //var oldArguments =
                //    $"-v 9 -re -i \"{inputFile}\" -ss {song.StartTime.TotalSeconds} -f s16le -acodec pcm_s16le -ar 48000 \"{outputFile}\"";
                // -ac 2 -f s16le -ar 48000
                var seek = song.StartTime.TotalSeconds > 0 ? $"-ss {song.StartTime.TotalSeconds}" : "";
                var arguments = $"-reconnect 1 -reconnect_streamed 1 -reconnect_delay_max 5 -err_detect ignore_err -i \"{inputFile}\" {seek} -ac 2 -f s16le -vn -ar 48000 pipe:1 -loglevel error";

                var startInfo = new ProcessStartInfo {
                    FileName = "ffmpeg",
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = false,
                    CreateNoWindow = true,
                    //RedirectStandardInput = true,
                    UseShellExecute = false
                };

                //if (File.Exists(outputFile)) {
                //    File.Delete(outputFile);
                //}

                var p = Process.Start(startInfo);

                //while (!song.Token.IsCancellationRequested && !p.HasExited) {
                //	await Task.Delay(100);
                //}

                //if (!p.HasExited) {
                //	await p.StandardInput.WriteLineAsync('q');
                //}

                //p.WaitForExit();

                if (p == null) {
                    throw new Exception("Could not start FFMPEG");
                }

                return p.StandardOutput.BaseStream;

            } catch (Exception ex) {
                Log.Warn(ex, "FFMPEG IO EXCEPTION");
            }

            Log.Debug("ffmpeg exited.");

            return null;
        }

        public void Stop() {
			_song.TokenSource.Cancel();
		}

		public void Pause() {
			_pauseRequested = true;
			_song.StartTime = _song.CurrentTime;
			_song.TokenSource.Cancel();
		}

		private static TimeSpan CalculateCurrentTime(int currentBytes) {
			return TimeSpan.FromSeconds(currentBytes /
										(1000d * 3840 /
										 Milliseconds));
		}
	}
}
