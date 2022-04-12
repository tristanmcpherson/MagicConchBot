using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Interactions;
using MagicConchBot.Common.Enums;
using MagicConchBot.Common.Types;
using MagicConchBot.Helpers;
using MagicConchBot.Services;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.DependencyInjection;

namespace MagicConchBot.Modules
{
    public class PlaylistModule : InteractionModuleBase<ConchInteractionCommandContext>
    {
        [SlashCommand("why", "Why")]
        public async Task Why()
        {
            await RespondAsync("Why");
        }

        [SlashCommand("save", "Save a song to a playlist")]
        public async Task SaveToPlaylist(string name = Playlist.DefaultName)
        {
            if (Context.MusicService.CurrentSong.HasNoValue)
            {
                return;
            }

            var playlist = Context.Settings.GetPlaylistOrCreate(name);

            await Context.MusicService.CurrentSong.Execute(async song => {
                playlist.Songs.Add(song.Identifier);
                Context.SaveSettings();

                await RespondAsync($"Added {song.Name} to playlist {playlist.Name}");
            });
        }

        [Group("playlist", "Playlist commands")]
        public class PlaylistSubModule : InteractionModuleBase<ConchInteractionCommandContext>
        {

            private readonly ISongResolutionService _songResolution;

            public PlaylistSubModule(IServiceProvider serviceProvider)
            {
                _songResolution = serviceProvider.GetService<ISongResolutionService>();
            }

            [SlashCommand("play", "Play a playlist")]
            public async Task LoadPlaylist(string name = Playlist.DefaultName)
            {
                var playlist = Context.Settings.GetPlaylistOrCreate(name);
                if (playlist.Songs.Count == 0)
                {
                    await RespondAsync($"No songs found in playlist: {name}");
                    return;
                }

                await RespondAsync("Queueing songs from playlist. This may take awhile.");

                var tasks = new Task<Song>[playlist.Songs.Count];

                for (var i = 0; i < playlist.Songs.Count; i++)
                {
                    tasks[i] = _songResolution.ResolveSong(playlist.Songs[i], TimeSpan.Zero);
                }

                var songs = await Task.WhenAll(tasks);

                await SongHelper.DisplaySongsClean(songs, Context);

                foreach (var song in songs)
                {
                    Context.MusicService.QueueSong(song);
                }

                await RespondAsync($"Queued {playlist.Songs.Count} songs.");

                if (!Context.MusicService.IsPlaying)
                {
                    await Context.MusicService.Play(Context, Context.Settings);
                }
            }

            [SlashCommand("savequeue", "Save queue as a playlist")]
            public async Task SaveQueueToPlaylist(string name = Playlist.DefaultName)
            {
                if (Context.MusicService.GetSongs().Count == 0)
                {
                    return;
                }

                var playlist = Context.Settings.GetPlaylistOrCreate(name);

                foreach (var song in Context.MusicService.GetSongs())
                {
                    playlist.Songs.Add(song.Identifier);
                }

                await RespondAsync($"Saved {playlist.Songs.Count} songs to playlist {playlist.Name}");
            }

            // TODO: Comment all code, general cleanup
            /* TODO: Add some form of checking in order to make sure the
               TODO: specified string is a valid url, possibly a search term that is resolved */
            [SlashCommand("add", "Add a song to playlist by name or url")]
            public async Task AddToPlaylist(string song, string name = Playlist.DefaultName)
            {
                if (!WebHelper.UrlRegex.IsMatch(song))
                {
                    await RespondAsync($"The url {song} is invalid. Please enter a valid url to save to this playlist.");
                    return;
                }

                var playlist = Context.Settings.GetPlaylistOrCreate(name);
                playlist.Songs.Add(song);
                Context.SaveSettings();

                await RespondAsync($"Added {song} to playlist {playlist.Name}");
            }

            [SlashCommand("songs", "List songs in playlist")]
            public async Task ShowPlaylist(string name = Playlist.DefaultName)
            {
                var playlist = Context.Settings.GetPlaylistOrCreate(name);

                if (playlist.Songs.Count == 0)
                {
                    await RespondAsync($"No songs found in playlist: {playlist.Name}");
                    return;
                }

                await RespondAsync("Resolving songs from playlist. This may take awhile.");

                var tasks = new Task<Song>[playlist.Songs.Count];

                for (var i = 0; i < playlist.Songs.Count; i++)
                {
                    tasks[i] = _songResolution.ResolveSong(playlist.Songs[i], TimeSpan.Zero);
                }

                var songs = await Task.WhenAll(tasks);

                await SongHelper.DisplaySongsClean(songs, Context);
            }

            [SlashCommand("all", "Show all playlists")]
            public async Task ListAllPlaylists()
            {
                var playlists = Context.Settings.Playlists;
                if (playlists.Count == 0)
                {
                    await RespondAsync("No playlists found.");
                    return;
                }

                var reply = new StringBuilder();
                reply.Append("Playlists:\n");

                foreach (var p in playlists)
                {
                    reply.Append($"{p.Name} + {string.Join(",", p.Songs.Take(3).Where(s => s != null))}, ...\n");
                }

                await RespondAsync(reply.ToString());
            }

            [SlashCommand("delete", "Delete playlist with name")]
            public async Task DeletePlaylist(string name)
            {
                var playlist = Context.Settings.GetPlaylistOrNull(name);
                if (playlist == null)
                {
                    return;
                }

                Context.Settings.Playlists.Remove(playlist);
                Context.SaveSettings();
                await RespondAsync($"Deleted playlist by the name of: {playlist.Name}");
            }
        }
    }
}