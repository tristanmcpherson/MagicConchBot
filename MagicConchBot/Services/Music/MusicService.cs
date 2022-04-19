using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Discord;
using Discord.Audio;
using MagicConchBot.Common.Enums;
using MagicConchBot.Common.Interfaces;
using MagicConchBot.Common.Types;
using MagicConchBot.Helpers;
using NLog;

namespace MagicConchBot.Services.Music
{
    public class MusicService : IMusicService
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly ISongPlayer _songPlayer;
        private readonly IEnumerable<ISongInfoService> _songResolvers;

        private int _songIndex;

        private CancellationTokenSource _tokenSource;

        public MusicService(IEnumerable<ISongInfoService> songResolvers, ISongPlayer songPlayer)
        {
            _songResolvers = songResolvers;
            _songPlayer = songPlayer;
            _songPlayer.OnSongCompleted += async (s, e) => await PlayNextSong(s, e);
            _songList = new List<Song>();
            PlayMode = PlayMode.Queue;
            LastSong = Maybe.None;
        }

        public float GetVolume() {
            return _songPlayer.GetVolume();
        }

        public void SetVolume(float value) {
            _songPlayer.SetVolume(value);
        }

        public List<Song> GetSongs()
        {
            return _songList;
        }

        private List<Song> _songList { get; }

        public PlayMode PlayMode { get; set; }

        public Maybe<Song> LastSong { get; private set; }

        public Maybe<Song> CurrentSong => _songIndex >= 0 && _songIndex < _songList.Count ? Maybe.From(_songList[_songIndex]) : Maybe.None;

        public bool IsPlaying => _songPlayer.IsPlaying();

        private async Task<Song> ResolveSong(Song song)
        {
            var properResolver = _songResolvers.FirstOrDefault(resolver => resolver.Regex.IsMatch(song.OriginalUrl));
            return await properResolver.ResolveStreamUri(song);
        }

        private async Task PlayNextSong(object sender, SongCompletedArgs e)
        {
            LastSong = e.Song;

            if (PlayMode == PlayMode.Queue)
            {
                CurrentSong.Execute(song => _songList.Remove(song));
            }
            else
            {
                _songIndex = (_songIndex + 1) % _songList.Count;
            }

            await CurrentSong.Map(song =>
                Play(e.Client, e.MessageChannel, song, e.Bitrate)
            ).ExecuteNoValue(() => AudioHelper.LeaveChannelAsync(e.Client));
        }

        public async Task Play(IInteractionContext context, GuildSettings settings)
        {
            await CurrentSong.Execute(async song =>
            {
                IVoiceChannel audioChannel = await AudioHelper.GetAudioChannel(context);
                IAudioClient audioClient = await AudioHelper.JoinChannelAsync(audioChannel);

                if (audioClient == null)
                {
                    await context.Channel.SendMessageAsync("Failed to join voice channel.");
                    return;
                }

                await Play(audioClient, context.Channel, song, audioChannel.Bitrate);
            });
        }

        public async Task Play(IAudioClient audioClient, IMessageChannel channel, Song song, int bitrate)
        {
            if (_tokenSource == null || _tokenSource.Token.IsCancellationRequested)
            {
                _tokenSource = new CancellationTokenSource();
            }

            var resolvedSong = await ResolveSong(song);
            _songList[_songIndex] = resolvedSong;

            _tokenSource.Token.ThrowIfCancellationRequested();

            try
            {
                Log.Info($"Playing song {resolvedSong.Name} at {channel.Name}");
                _songPlayer.PlaySong(audioClient, channel, resolvedSong, bitrate);
                await StatusUpdater(channel).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Debug(ex.ToString());
            }
        }

        private async Task StatusUpdater(IMessageChannel channel)
        {
            await Task.Factory.StartNew(async () =>
            {
                IUserMessage message = null;
                try
                {
                    var time = 2000;
                    var stopwatch = new Stopwatch();

                    await CurrentSong.Execute(async song =>
                    {

                        message = await channel.SendMessageAsync(string.Empty, false, song.GetEmbed("", true, true, GetVolume()));

                        while (IsPlaying)
                        {
                            // Song changed or stopped. Stop updating song info.
                            await CurrentSong.Where(current => current.Identifier == song.Identifier).Execute(async song =>
                            {
                                var embed = song.GetEmbed("", true, true, GetVolume());
                                await message.ModifyAsync(m => m.Embed = embed);
                                var ms = stopwatch.ElapsedMilliseconds;
                                if (ms < time)
                                {
                                    await Task.Delay(time - (int)ms);
                                }
                                stopwatch.Restart();
                            });
                        }
                    });

                }
                catch (OperationCanceledException ex)
                {
                    Log.Debug($"Player task cancelled: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Log.Debug(ex.ToString());
                }
                finally
                {
                    if (message != null)
                    {
                        await message.DeleteAsync(new RequestOptions() { RetryMode = RetryMode.AlwaysRetry });
                    }
                }
            }, _tokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current);
        }

        public async Task Stop()
        {
            _songList.Clear();
            await _songPlayer.Stop();
        }

        public async Task Pause()
        {
            await _songPlayer.Pause();
        }

        public bool Skip()
        {
            _songPlayer.Stop();
            return true;
        }

        public void QueueSong(Song song)
        {
            _songList.Add(song);
        }

        public async Task<Maybe<Song>> RemoveSong(int songNumber)
        {
            if (songNumber < 0 || songNumber >= _songList.Count)
                return Maybe.None;

            if (songNumber == 0)
                await Stop();

            var song = _songList[songNumber];
            _songList.Remove(song);

            return song;
        }

        public void ClearQueue()
        {
            _songList.Clear();
        }
    }
}