using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Discord;
using Discord.Audio;
using MagicConchBot.Common.Interfaces;
using MagicConchBot.Common.Types;
using MagicConchBot.Helpers;
using NLog;
using Stateless;

namespace MagicConchBot.Services.Music
{
    public enum PlayerState
    {
        Stopped = 0,
        Paused,
        Playing
    }

    public enum PlayerAction
    {
        ChangeVolume,
        Play,
        Pause,
        Stop,
        SongFinished
    }

    public class FfmpegSongPlayer : ISongPlayer
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private const float MaxVolume = 1f;
        private const float MinVolume = 0f;
        private const int Milliseconds = 20;
        private const int FrameSize = 3840;
        private const int MaxRetryCount = 50;

        private Song currentSong;
        private IAudioClient audioClient;
        private IMessageChannel messageChannel;
        private CancellationTokenSource tokenSource;
        private float volume = 0.5f;

        private readonly StateMachine<PlayerState, PlayerAction> songPlayer;
        private readonly StateMachine<PlayerState, PlayerAction>.TriggerWithParameters<float> changeVolume;
        private readonly StateMachine<PlayerState, PlayerAction>.TriggerWithParameters<IAudioClient> playTrigger;

        public event AsyncEventHandler<SongCompletedArgs> OnSongCompleted;
        private Task HandleSongCompleted() => OnSongCompleted?.Invoke(this, new(audioClient, messageChannel, currentSong));

        public FfmpegSongPlayer()
        {
            songPlayer = new StateMachine<PlayerState, PlayerAction>(PlayerState.Stopped);

            playTrigger = songPlayer.SetTriggerParameters<IAudioClient>(PlayerAction.Play);
            changeVolume = songPlayer.SetTriggerParameters<float>(PlayerAction.ChangeVolume);

            songPlayer.Configure(PlayerState.Stopped)
                .Ignore(PlayerAction.Stop)
                .Ignore(PlayerAction.Pause)
                .OnEntry(CancelToken)
                .OnEntryFrom(PlayerAction.Stop, OnStop)
                .OnEntryFromAsync(PlayerAction.SongFinished, HandleSongCompleted)
                .Permit(PlayerAction.Play, PlayerState.Playing)
                .PermitReentry(PlayerAction.SongFinished);

            songPlayer.Configure(PlayerState.Paused)
                .Ignore(PlayerAction.Pause)
                .Ignore(PlayerAction.SongFinished)
                .OnEntry(CancelToken)
                .OnEntryAsync(SaveTimeAndDisconnect)
                .Permit(PlayerAction.Play, PlayerState.Playing)
                .Permit(PlayerAction.Stop, PlayerState.Stopped);

            songPlayer.Configure(PlayerState.Playing)
                .Ignore(PlayerAction.Play)
                .InternalTransition(changeVolume, (volume, _) => this.volume = volume)
                .Permit(PlayerAction.SongFinished, PlayerState.Stopped)
                .Permit(PlayerAction.Stop, PlayerState.Stopped)
                .Permit(PlayerAction.Pause, PlayerState.Paused)
                .OnEntryFrom(playTrigger, PlaySongLongRunning);

            songPlayer.OnTransitioned((transition) => Log.Info(transition.Trigger + ": " + transition.Source + " -> " + transition.Destination));
        }

        public void PlaySong(IAudioClient client, IMessageChannel channel, Song song)
        {
            currentSong = song;
            messageChannel = channel;
            audioClient = client;
            tokenSource = new CancellationTokenSource();

            songPlayer.Fire(playTrigger, client);
        }

        public async Task Stop()
        {
            await songPlayer.FireAsync(PlayerAction.Stop);
        }

        public async Task Pause()
        {
            await songPlayer.FireAsync(PlayerAction.Pause);
        }

        private void OnStop()
        {
            currentSong.Time.StartTime = TimeSpan.Zero;
        }

        private void CancelToken()
        {
            tokenSource.Cancel();
        }

        private async Task SaveTimeAndDisconnect()
        {
            currentSong.Time.StartTime = currentSong.Time.CurrentTime.GetValueOrThrow("No value");
            if (audioClient.ConnectionState == ConnectionState.Connected)
                await audioClient.StopAsync();
        }

        public void SetVolume(float volume)
        {
            songPlayer.Fire(changeVolume, Math.Clamp(volume, MinVolume, MaxVolume));
        }

        public float GetVolume()
        {
            return volume;
        }

        public bool IsPlaying()
        {
            return songPlayer.State == PlayerState.Playing;
        }

        private void PlaySongLongRunning(IAudioClient audioClient)
        {
            Task.Factory.StartNew(() => PlaySong(audioClient), tokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private async Task PlaySong(IAudioClient audioClient)
        {
            try
            {
                using var process = StartFfmpeg(currentSong);
                using var inStream = process.StandardOutput.BaseStream;
                using var outStream = await CreatePCMStream(audioClient, currentSong);

                tokenSource.Token.Register(() => {
                    process.CloseMainWindow();
                });

                await StreamAudio(currentSong, inStream, outStream, tokenSource);
            } 
            finally
            {
                await songPlayer.FireAsync(PlayerAction.SongFinished);
            }
        }

        private async Task StreamAudio(Song song, Stream inStream, AudioOutStream outStream, CancellationTokenSource tokenSource)
        {
            var buffer = new byte[FrameSize];
            var retryCount = 0;

            Log.Debug("Playing song.");
            song.Time.CurrentTime = song.Time.StartTime.GetValueOrDefault(TimeSpan.Zero);

            while (!tokenSource.IsCancellationRequested)
            {
                var byteCount = await inStream.ReadAsync(buffer.AsMemory(0, buffer.Length), tokenSource.Token);

                if (byteCount == 0)
                {
                    if (song.Time.Length != TimeSpan.Zero && song.Time.Length - song.Time.CurrentTime.GetValueOrDefault() <= TimeSpan.FromMilliseconds(1000))
                    {
                        Log.Debug("Read 0 bytes but song is finished.");
                        break;
                    }

                    await Task.Delay(100, tokenSource.Token).ConfigureAwait(false);

                    if (++retryCount == MaxRetryCount)
                    {
                        Log.Warn($"Failed to read from ffmpeg. Retries: {retryCount}");
                        break;
                    }
                }
                else
                {
                    retryCount = 0;
                }

                buffer = AudioHelper.ChangeVol(buffer, volume);

                if (outStream.CanWrite)
                {
                    await outStream.WriteAsync(buffer.AsMemory(0, byteCount), tokenSource.Token);
                }

                song.Time.CurrentTime = song.Time.CurrentTime.Map(current => current + CalculateCurrentTime(byteCount));

            }

            await outStream.FlushAsync(tokenSource.Token);
        }

        private static async Task<AudioOutStream> CreatePCMStream(IAudioClient audioClient, Song song)
        {
            var audioOut = audioClient.CreatePCMStream(AudioApplication.Music, packetLoss: 0);
            return audioOut;
        }

        private static Process StartFfmpeg(Song song)
        {
            var seek = song.Time.StartTime.Map(totalSeconds => $"-ss {totalSeconds}").GetValueOrDefault(string.Empty);
            var arguments = $"-reconnect 1 -reconnect_streamed 1 -reconnect_delay_max 5 -err_detect ignore_err -i \"{song.StreamUri}\" {seek} -ac 2 -f s16le -vn -ar 48000 pipe:1 -loglevel error";

            Log.Debug(arguments);

            var startInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                UseShellExecute = false,
            };
            var process = new Process()
            {
                StartInfo = startInfo
            };

            process.ErrorDataReceived += (sender, data) =>
            {
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

            if (process == null)
            {
                throw new Exception("Could not start FFMPEG");
            }

            if (process.StandardOutput.BaseStream == null)
            {
                throw new Exception("FFMPEG stream was not created.");
            }

            return process;
        }

        private static TimeSpan CalculateCurrentTime(int currentBytes)
        {
            return TimeSpan.FromSeconds(currentBytes /
                                        (1000d * FrameSize /
                                         Milliseconds));
        }
    }
}
