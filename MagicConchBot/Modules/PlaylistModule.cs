using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using MagicConchBot.Common.Enums;
using MagicConchBot.Common.Types;
using MagicConchBot.Helpers;
using MagicConchBot.Services;

namespace MagicConchBot.Modules
{
    [Group("playlist"), Alias("pl")]
    public class PlaylistModule : ModuleBase<ConchCommandContext>
    {
        private readonly SongResolutionService _songResolution;

        public PlaylistModule(SongResolutionService songResolution)
        {
            _songResolution = songResolution;
        }

        [Command, Alias("l", "play", "load")]
        public async Task LoadPlaylist(string playlist = Playlist.DefaultName)
        {
            var pl = Context.Settings.Playlists.FirstOrDefault(p => p.Name == playlist);
            if (pl == null)
            {
                await ReplyAsync($"No songs found in playlist: {playlist}");
                return;
            }

            await ReplyAsync("Queueing songs from playlist. This may take awhile.");

            var tasks = new Task<Song>[pl.Songs.Count];

            for (var i = 0; i < pl.Songs.Count; i++)
            {
                tasks[i] = _songResolution.ResolveSong(pl.Songs[i], TimeSpan.Zero);
            }

            var songs = await Task.WhenAll(tasks);

            await SongHelper.DisplaySongsClean(songs, Context.Channel);

            foreach (var song in songs)
            {
                Context.MusicService.QueueSong(song);
            }

            await ReplyAsync($"Queued {pl.Songs.Count} songs.");

            if (Context.MusicService.PlayerState == PlayerState.Stopped ||
                Context.MusicService.PlayerState == PlayerState.Paused)
            {
                await Context.MusicService.PlayAsync(Context.Message);
            } 
        }

        [Command("save"), Alias("s")]
        public async Task SaveToPlaylist(string playlist = Playlist.DefaultName)
        {
            if (Context.MusicService.CurrentSong == null)
            {
                return;
            }
            
            var pl = Context.Settings.Playlists.FirstOrDefault(p => p.Name == playlist);
            if (pl == null)
            {
                pl = new Playlist(playlist);
                Context.Settings.Playlists.Add(pl);
            }

            pl.Songs.Add(Context.MusicService.CurrentSong?.Url);
            Context.SaveSettings();

            await ReplyAsync($"Added {Context.MusicService.CurrentSong.Name} to playlist {playlist}");
        }

        // TODO: Comment all code, general cleanup
        /* TODO: Add some form of checking in order to make sure the
           TODO: specified string is a valid url, possibly a search term that is resolved */
        [Command("add")]
        public async Task AddToPlaylist(string song, string playlist = Playlist.DefaultName)
        {
            var pl = Context.Settings.Playlists.FirstOrDefault(p => p.Name == playlist);
            if (pl == null)
            {
                pl = new Playlist(playlist);
                Context.Settings.Playlists.Add(pl);
            }
            pl.Songs.Add(song);
            Context.SaveSettings();

            await ReplyAsync($"Added {song} to playlist {playlist}");
        }

        [Command, Alias("show", "list")]
        public async Task ShowPlaylist(string playlist = Playlist.DefaultName)
        {
            var pl = Context.Settings.Playlists.FirstOrDefault(p => p.Name == playlist);

            if (pl == null)
            {
                await ReplyAsync($"No songs found in playlist: {playlist}");
                return;
            }

            await ReplyAsync("Resolving songs from playlist. This may take awhile.");

            var tasks = new Task<Song>[pl.Songs.Count];

            for (var i = 0; i < pl.Songs.Count; i++)
            {
                tasks[i] = _songResolution.ResolveSong(pl.Songs[i], TimeSpan.Zero);
            }

            var songs = await Task.WhenAll(tasks);

            await SongHelper.DisplaySongsClean(songs, Context.Channel);
        }
    }
}
