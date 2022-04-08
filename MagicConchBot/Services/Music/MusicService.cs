using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.Commands;
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
        private readonly IEnumerable<ISongResolver> _songResolvers;

        private int _songIndex;

        private CancellationTokenSource _tokenSource;

        public MusicService(IEnumerable<ISongResolver> songResolvers, ISongPlayer songPlayer)
        {
            _songResolvers = songResolvers;
            _songPlayer = songPlayer;
            _songList = new List<Song>();
            PlayMode = PlayMode.Queue;
            CurrentSong = null;
            LastSong = null;
        }

        public PlayerState PlayerState => _songPlayer.PlayerState;

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

        public Song LastSong { get; private set; }

        public Song CurrentSong { get; private set; }

        public void Play(IInteractionContext context, GuildSettings settings)
        {
            if (_tokenSource == null || _tokenSource.Token.IsCancellationRequested)
            {
                _tokenSource = new CancellationTokenSource();
            }

            IAudioClient audioClient = null;

            Task.Factory.StartNew(async () =>
            {
                try
                {
                    audioClient = await AudioHelper.JoinChannelAsync(context);

                    if (audioClient == null)
                    {
                        await context.Channel.SendMessageAsync("Failed to join voice channel.");
                        return;
                    }

                    while (!_tokenSource.IsCancellationRequested)
                    {
                        if (CurrentSong != null)
                            LastSong = CurrentSong;

                        if (_songIndex < 0 || _songIndex >= _songList.Count)
                            return;

                        CurrentSong = _songList[_songIndex];

                        _tokenSource.Token.ThrowIfCancellationRequested();

                        string streamUrl = await _songResolvers.SelectFirst(async resolver => await resolver.GetSongStreamUrl(CurrentSong));
                        if (streamUrl == null)
                        {
                            throw new Exception($"No songs resolved for song: ${CurrentSong.Identifier}");
                        }

                        CurrentSong.StreamUri = streamUrl;
                        

                        await StatusUpdater(context.Channel).ConfigureAwait(false);

                        try
                        {
                            Log.Info($"Playing song {CurrentSong.Name} on channel {context.Channel.Name}");
                            await Task.Run(async () => await _songPlayer.PlaySong(audioClient, CurrentSong, settings.IntroPCM));
                        }
                        catch (Exception ex)
                        {
                            Log.Debug(ex.ToString());
                        }
                        finally
                        {
                            Log.Info($"Song ended at {CurrentSong.GetCurrentTimePretty()} / {CurrentSong.GetLengthPretty()}");

                            if (_songPlayer.PlayerState != PlayerState.Paused)
                            {
                                if (PlayMode == PlayMode.Queue)
                                {
                                    _songList.Remove(CurrentSong);
                                }
                                else
                                {
                                    _songIndex = (_songIndex + 1) % _songList.Count;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                    _songList.Remove(CurrentSong);
                    CurrentSong = null;
                }
                finally
                {
                    await AudioHelper.LeaveChannelAsync(audioClient).ConfigureAwait(false);
                }
            }, _tokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private async Task StatusUpdater(IMessageChannel channel)
        {
            await Task.Factory.StartNew((Func<Task>)(async () =>
            {
                IUserMessage message = null;
                try
                {
                    var song = CurrentSong;
                    var time = 2000;
                    var stopwatch = new Stopwatch();

                    if (song == null)
                        return;

                    message = await channel.SendMessageAsync(string.Empty, false, song.GetEmbed("", true, true, GetVolume()));

                    while (_songPlayer.PlayerState == PlayerState.Playing || _songPlayer.PlayerState == PlayerState.Loading)
                    {
                        // Song changed or stopped. Stop updating song info.
                        if (CurrentSong == null || CurrentSong.Identifier != song.Identifier)
                            break;

                        await message.ModifyAsync(m => m.Embed = song.GetEmbed("", true, true, GetVolume()));
                        if (stopwatch.ElapsedMilliseconds < time)
                        {
                            await Task.Delay(time - (int)stopwatch.ElapsedMilliseconds);
                        }
                        stopwatch.Restart();
                    }

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
                        await message.DeleteAsync();
                }
            }), TaskCreationOptions.LongRunning);
        }

        public bool Stop()
        {
            if (_songPlayer.PlayerState == PlayerState.Stopped || _tokenSource == null)
            {
                return false;
            }

            _songList.Clear();

            _tokenSource.Cancel();
            _songPlayer.Stop();
            return true;
        }

        public bool Pause()
        {
            if (_songPlayer.PlayerState != PlayerState.Playing && _songPlayer.PlayerState != PlayerState.Loading || _tokenSource == null)
                return false;

            _tokenSource.Cancel();
            _songPlayer.Pause();

            return true;
        }

        public bool Skip()
        {
            if (_songPlayer.PlayerState != PlayerState.Playing && _songPlayer.PlayerState != PlayerState.Loading || _tokenSource == null)
                return false;
            
            _songPlayer.Stop();
            return true;
        }

        public void QueueSong(Song song)
        {
            _songList.Add(song);
        }

        public Song RemoveSong(int songNumber)
        {
            if (songNumber < 0 || songNumber >= _songList.Count)
                return null;

            if (songNumber == 0)
                Stop();

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