using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using MagicConchBot.Common.Enums;
using MagicConchBot.Common.Interfaces;
using MagicConchBot.Common.Types;
using MagicConchBot.Helpers;
using MagicConchBot.Services;
using MagicConchBot.Services.Music;
using Microsoft.Extensions.DependencyInjection;

namespace MagicConchBot.Modules
{
    public class PlaylistModule : ModuleBase<ConchCommandContext>
    {
        [Command("save"), Alias("s")]
        public async Task SaveToPlaylist(string name = Playlist.DefaultName)
        {
            if (Context.MusicService.CurrentSong == null)
            {
                return;
            }

            var playlist = Context.Settings.GetPlaylistOrCreate(name);

            playlist.Songs.Add(Context.MusicService.CurrentSong.Url);
            Context.SaveSettings();

            await ReplyAsync($"Added {Context.MusicService.CurrentSong.Name} to playlist {playlist.Name}");
        }

        [Group("playlist"), Alias("pl")]
        public class PlaylistSubModule : ModuleBase<ConchCommandContext>
        {

            private readonly ISongResolutionService _songResolution;

            public PlaylistSubModule(IServiceProvider serviceProvider)
            {
                _songResolution = serviceProvider.GetService<ISongResolutionService>();
            }

            [Command("play"), Alias("load")]
            public async Task LoadPlaylist(string name = Playlist.DefaultName)
            {
                var playlist = Context.Settings.GetPlaylistOrCreate(name);
                if (playlist.Songs.Count == 0)
                {
                    await ReplyAsync($"No songs found in playlist: {name}");
                    return;
                }

                await ReplyAsync("Queueing songs from playlist. This may take awhile.");

                var tasks = new Task<Song>[playlist.Songs.Count];

                for (var i = 0; i < playlist.Songs.Count; i++)
                {
                    tasks[i] = _songResolution.ResolveSong(playlist.Songs[i], TimeSpan.Zero);
                }

                var songs = await Task.WhenAll(tasks);

                await SongHelper.DisplaySongsClean(songs, Context.Channel);

                foreach (var song in songs)
                {
                    Context.MusicService.QueueSong(song);
                }

                await ReplyAsync($"Queued {playlist.Songs.Count} songs.");

                if (Context.MusicService.PlayerState == PlayerState.Stopped ||
                    Context.MusicService.PlayerState == PlayerState.Paused)
                {
                    await Context.MusicService.Play(Context);
                }
            }

            [Command("save queue")]
            public async Task SaveQueueToPlaylist(string name = Playlist.DefaultName)
            {
                if (Context.MusicService.SongList.Count == 0)
                {
                    return;
                }

                var playlist = Context.Settings.GetPlaylistOrCreate(name);

                foreach (var song in Context.MusicService.SongList)
                {
                    playlist.Songs.Add(song.Url);
                }

                await ReplyAsync($"Saved {playlist.Songs.Count} songs to playlist {playlist.Name}");
            }

            // TODO: Comment all code, general cleanup
            /* TODO: Add some form of checking in order to make sure the
               TODO: specified string is a valid url, possibly a search term that is resolved */
            [Command("add")]
            public async Task AddToPlaylist(string song, string name = Playlist.DefaultName)
            {
                if (!WebHelper.UrlRegex.IsMatch(song))
                {
                    await ReplyAsync($"The url {song} is invalid. Please enter a valid url to save to this playlist.");
                    return;
                }
                 
                var playlist = Context.Settings.GetPlaylistOrCreate(name);
                playlist.Songs.Add(song);
                Context.SaveSettings();

                await ReplyAsync($"Added {song} to playlist {playlist.Name}");
            }

            [Command("songs"), Alias("show")]
            public async Task ShowPlaylist(string name = Playlist.DefaultName)
            {
                var playlist = Context.Settings.GetPlaylistOrCreate(name);

                if (playlist.Songs.Count == 0)
                {
                    await ReplyAsync($"No songs found in playlist: {playlist.Name}");
                    return;
                }

                await ReplyAsync("Resolving songs from playlist. This may take awhile.");

                var tasks = new Task<Song>[playlist.Songs.Count];

                for (var i = 0; i < playlist.Songs.Count; i++)
                {
                    tasks[i] = _songResolution.ResolveSong(playlist.Songs[i], TimeSpan.Zero);
                }

                var songs = await Task.WhenAll(tasks);

                await SongHelper.DisplaySongsClean(songs, Context.Channel);
            }

            [Command("showall"), Alias("list")]
            public async Task ListAllPlaylists()
            {
                var playlists = Context.Settings.Playlists;
                if (playlists.Count == 0)
                {
                    await ReplyAsync("No playlists found.");
                    return;
                }

                var reply = new StringBuilder();
                reply.Append("Playlists:\n");

                foreach (var p in playlists)
                {
                    reply.Append($"{p.Name} + {string.Join(",", p.Songs.Take(3).Where(s => s != null))}, ...\n");
                }
                
                await ReplyAsync(reply.ToString());
            }

            [Command("delete"), Alias("delet")]
            public async Task DeletePlaylist(string name)
            {
                var playlist = Context.Settings.GetPlaylistOrNull(name);
                if (playlist == null)
                {
                    return;
                }

                Context.Settings.Playlists.Remove(playlist);
                Context.SaveSettings();
                await ReplyAsync($"Deleted playlist by the name of: {playlist.Name}");
            }
        }
    }
}
