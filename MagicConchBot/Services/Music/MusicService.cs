using System;
using System.Collections.Generic;
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
    public class MusicService : IMusicService
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly ISongPlayer _songPlayer;
        private readonly List<ISongResolver> _songResolvers;

        private int _songIndex;

        private CancellationTokenSource _tokenSource;

        public MusicService(List<ISongResolver> songResolvers, ISongPlayer songPlayer)
        {
            _songResolvers = songResolvers;
            _songPlayer = songPlayer;
            SongList = new List<Song>();
            PlayMode = PlayMode.Queue;
            CurrentSong = null;
            LastSong = null;
        }

        public PlayerState PlayerState => _songPlayer.PlayerState;

        public float Volume
        {
            get => _songPlayer.Volume;
            set => _songPlayer.Volume = value;
        }

        public List<Song> SongList { get; }

        public PlayMode PlayMode { get; set; }

        public Song LastSong { get; private set; }

        public Song CurrentSong { get; private set; }

        public async Task PlayAsync(IUserMessage msg)
        {
            if (_tokenSource == null || _tokenSource.Token.IsCancellationRequested)
            {
                _tokenSource = new CancellationTokenSource();
            }

            IAudioClient audioClient = null;

            await Task.Factory.StartNew(async () =>
            {
                try
                {
                    
                    audioClient = await AudioHelper.JoinChannelAsync(msg).ConfigureAwait(false);

                    if (audioClient == null)
                    {
                        await msg.Channel.SendMessageAsync("Failed to join voice channel.");
                        return;
                    }

                    while (!_tokenSource.IsCancellationRequested)
                    {
                        if (CurrentSong != null)
                            LastSong = CurrentSong;

                        if (_songIndex < 0 || _songIndex >= SongList.Count)
                            return;

                        CurrentSong = SongList[_songIndex];
                        CurrentSong.TokenSource = new CancellationTokenSource();

                        _tokenSource.Token.ThrowIfCancellationRequested();

                        string streamUri = null;
                        foreach (var resolver in _songResolvers)
                        {
                            var uri = await resolver.GetSongStreamUrl(CurrentSong.Url);
                            if (uri != null)
                            {
                                streamUri = uri;
                                break;
                            }
                        }

                        CurrentSong.StreamUri = streamUri ?? throw new Exception($"Failed to resolve song: {CurrentSong.Url}");
                        await StatusUpdaterAsync(msg.Channel).ConfigureAwait(false);

                        try
                        {
                            Log.Info($"Playing song {CurrentSong.Name} on channel {msg.Channel.Name}");
                            await Task.Run(async () => await _songPlayer.PlaySong(audioClient, CurrentSong));
                        }
                        catch (Exception ex)
                        {
                            Log.Debug(ex.ToString());
                        }
                        finally
                        {
                            Log.Info($"Song ended at {CurrentSong.CurrentTimePretty} / {CurrentSong.LengthPretty}");

                            if (_songPlayer.PlayerState != PlayerState.Paused)
                            {
                                if (PlayMode == PlayMode.Queue)
                                {
                                    SongList.Remove(CurrentSong);
                                }
                                else
                                {
                                    if (++_songIndex == SongList.Count)
                                        _songIndex = 0;
                                }
                            }
                        }
                    }
                }
                finally
                {
                    await AudioHelper.LeaveChannelAsync(audioClient).ConfigureAwait(false);
                }
            }, _tokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private async Task StatusUpdaterAsync(IMessageChannel channel)
        {
            await Task.Factory.StartNew(async () =>
            {
                IUserMessage message = null;
                try
                {
                    var song = CurrentSong;
                    if (song == null)
                        return;

                    message = await channel.SendMessageAsync(string.Empty, false, song.GetEmbed("", false, true));

                    while (_songPlayer.PlayerState == PlayerState.Playing || _songPlayer.PlayerState == PlayerState.Loading)
                    {
                        // Song changed. Stop updating song info.
                        if (CurrentSong.Url != song.Url)
                            break;

                        song.Token.ThrowIfCancellationRequested();

                        await message.ModifyAsync(m => m.Embed = song.GetEmbed("", false, true));
                        await Task.Delay(4700);
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
            }, CurrentSong.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public bool Stop()
        {
            if (_songPlayer.PlayerState == PlayerState.Stopped || _tokenSource == null)
            {
                return false;
            }

            SongList.Clear();

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
            SongList.Add(song);
        }

        public Song RemoveSong(int songNumber)
        {
            if (songNumber < 0 || songNumber > SongList.Count)
                return null;

            if (songNumber == 0)
                Stop();

            var removedSong = SongList[songNumber];
            SongList.RemoveAt(songNumber);

            return removedSong;
        }

        public void ClearQueue()
        {
            SongList.Clear();
        }
    }
}