using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using MagicConchBot.Common.Enums;
using MagicConchBot.Common.Types;
using MagicConchBot.Helpers;
using MagicConchBot.Services;
using MagicConchBot.Services.Music;

namespace MagicConchBot.Modules
{
    public class PlaylistModule : ModuleBase<MusicCommandContext>
    {
        private readonly GuildSettingsService _guildSettingsService;
        private readonly SongResolutionService _songResolution;

        public PlaylistModule(GuildSettingsService guildSettingsService, SongResolutionService songResolution)
        {
            _guildSettingsService = guildSettingsService;
            _songResolution = songResolution;
        }

        [Command("load"), Alias("l", "playlist load", "playlist l")]
        public async Task LoadPlaylist(string playlist = null)
        {
            var name = playlist ?? Playlist.DefaultName;
            var settings = _guildSettingsService.GetSettings(Context.Guild.Id);
            var pl = settings.Playlists.FirstOrDefault(p => p.Name == name);
            if (pl == null)
            {
                await ReplyAsync($"No songs found in playlist: {name}");
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
        public async Task SaveToPlaylist(string playlist = null)
        {
            if (Context.MusicService.CurrentSong == null)
            {
                return;
            }

            var name = playlist ?? Playlist.DefaultName;
            var settings = _guildSettingsService.GetSettings(Context.Guild.Id);
            var pl = settings.Playlists.FirstOrDefault(p => p.Name == name);
            if (pl == null)
            {
                pl = new Playlist();
                settings.Playlists.Add(pl);
            }

            pl.Songs.Add(Context.MusicService.CurrentSong?.Url);
            _guildSettingsService.UpdateSettings(Context.Guild.Id, settings);

            await ReplyAsync($"Added {Context.MusicService.CurrentSong.Name} to playlist {name}");
        }
    }
}
