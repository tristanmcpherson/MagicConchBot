using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using MagicConchBot.Attributes;
using MagicConchBot.Common.Enums;
using MagicConchBot.Common.Interfaces;
using MagicConchBot.Common.Types;
using MagicConchBot.Services;
using MagicConchBot.Services.Music;
using NLog;

namespace MagicConchBot.Modules
{
    [RequireUserInVoiceChannel]
    [RequireBotControlRole]
    [Name("Music Commands")]
    public class MusicModule : ModuleBase<MusicCommandContext>
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static readonly Regex UrlRegex =
            new Regex(@"(\b(https?):\/\/)?[-A-Za-z0-9+\/%?=_!.]+\.[-A-Za-z0-9+&#\/%=_]+");

        private readonly ChanService _chanService;

        private readonly GoogleApiService _googleApiService;
        private readonly List<IMusicInfoService> _musicInfoServices;
        private readonly YouTubeInfoService _youtubeInfoService;

        public MusicModule(IDependencyMap map)
        {
            _musicInfoServices = new List<IMusicInfoService>
            {
                map.Get<YouTubeInfoService>(),
                map.Get<SoundCloudInfoService>()
            };
            _googleApiService = map.Get<GoogleApiService>();
            _chanService = map.Get<ChanService>();
            _youtubeInfoService = map.Get<YouTubeInfoService>();
        }

        [Command("play")]
        [Summary(
            "Plays a song from YouTube or SoundCloud. Alternatively uses the search terms to find a corresponding video on YouTube."
        )]
        public async Task PlayAsync()
        {
            if (Context.MusicService.AudioState == AudioState.Playing || Context.MusicService.AudioState == AudioState.Loading)
            {
                await ReplyAsync("Song already playing.");
            }
            else if (Context.MusicService.SongList.Count > 0)
            {
                await Context.MusicService.PlayAsync(Context.Message);
                await ReplyAsync("Resuming queue.");
            }
            else
            {
                await ReplyAsync("No songs currently in the queue.");
            }
        }

        [Command("play")]
        [Summary(
            "Plays a song from YouTube or SoundCloud. Alternatively uses the search terms to find a corresponding video on YouTube."
        )]
        public async Task PlayAsync(
            [Remainder] [Summary("The url or search terms optionally followed by a time to start at (e.g. 00:01:30 for 1m 30s.)")] string query)
        {
            var terms = query.Split(' ');
            var startTime = TimeSpan.Zero;
            string url;
            Song song = null;

            if (terms.Length > 1)
                if (TimeSpan.TryParseExact(terms.Last(), new[] {@"mm\:ss", @"hh\:mm\:ss"}, CultureInfo.InvariantCulture,
                    out startTime))
                    query = query.Replace(" " + terms.Last(), string.Empty);
                else
                    startTime = TimeSpan.Zero;

            if (UrlRegex.IsMatch(query))
                url = query;
            else
                url = await _googleApiService.GetFirstVideoByKeywordsAsync(query);

            // url invalid
            if (string.IsNullOrEmpty(url))
            {
                await ReplyAsync($"Could not find any videos for: {query}");
                return;
            }

            var youtubeMatch = _youtubeInfoService.Regex.Match(url);
            var playlistId = youtubeMatch.Groups["PlaylistId"].Value;
            if (playlistId != "")
            {
                await ReplyAsync("Queueing songs from playlist. This may take a while, please wait.");
                var songs = await _googleApiService.GetSongsByPlaylistAsync(playlistId);

                Context.MusicService.SongList.AddRange(songs);

                await ReplyAsync($"Queued {songs.Count} songs from playlist.");
            }
            else
            {
                foreach (var service in _musicInfoServices)
                {
                    var match = service.Regex.Match(url);
                    if (!match.Success)
                        continue;

                    song = await service.GetSongInfoAsync(url);

                    // url may contain time info but it is specified, overwrite
                    if (startTime != TimeSpan.Zero)
                        song.StartTime = startTime;

                    // song info found, stop info service search
                    break;
                }

                // Song info not found from search or url
                if (song == null)
                    song = new Song(url);

                // valid url but song information not found by any song info service
                Log.Info($"Queued song: {song.Name} - {song.Url} at {song.StartTime}.");

                // add to queue
                Context.MusicService.QueueSong(song);

                await ReplyAsync("Queued song:", false, song.GetEmbed());
            }

            // if not playing, start playing and then the player service
            if (Context.MusicService.AudioState == AudioState.Stopped || Context.MusicService.AudioState == AudioState.Paused)
            {
                Log.Info("No song currently playing, playing.");
                await Context.MusicService.PlayAsync(Context.Message);
            }
        }

        [Command("stop")]
        [Summary("Stops the bot if it is playing music and disconnects it from the voice channel.")]
        public async Task StopAsync()
        {
            var stop = Context.MusicService.Stop();
            await ReplyAsync(stop ? "Music stopped playing." : "No music currently playing.");
        }

        [Command("pause")]
        [Summary("Pauses the current song.")]
        public async Task PauseAsync()
        {
            var paused = Context.MusicService.Pause();
            await ReplyAsync(paused ? "Music paused successfully." : "No music currently playing.");
        }

        [Command("skip")]
        [Summary("Skips the current song if one is playing.")]
        public async Task SkipAsync()
        {
            var skipped = Context.MusicService.Skip();
            await ReplyAsync(skipped ? "Skipped current song." : "No song available to skip");
        }

        [Command("volume")]
        [Alias("vol")]
        [Summary("Gets the current volume.")]
        public async Task ChangeVolumeAsync()
        {
            await ReplyAsync($"Current volume: {Context.MusicService.Volume*100}%.");
        }

        [Command("volume")]
        [Alias("vol")]
        [Summary("Changes the volume of the current playing song and future songs.")]
        public async Task ChangeVolumeAsync([Summary("The volume to set the song to from between 0 and 100.")] int volume)
        {
            Context.MusicService.Volume = volume == 0 ? 0 : volume / 100f;
            await ReplyAsync($"Current volume set to {Context.MusicService.Volume*100}%.");
        }

        [Command("current")]
        [Summary("Displays the current song")]
        public async Task CurrentSongAsync()
        {
            var song = Context.MusicService.CurrentSong;
            if (song == null)
                await ReplyAsync("No song is currently playing.");
            else
                await ReplyAsync(string.Empty, false, song.GetEmbed());
        }

        [Command("loop")]
        [Alias("repeat")]
        public async Task Loop()
        {
            Context.MusicService.PlayMode = PlayMode.Playlist;
            await ReplyAsync(
                "Successfully changed mode to playlist mode. Songs will not be removed from queue after they are done playing.");
        }

        [Command("mp3")]
        [Summary("Generates a link to the mp3 of the current song playing or the last song played.")]
        public async Task GenerateMp3Async()
        {
            var currentSong = Context.MusicService.CurrentSong ?? Context.MusicService.LastSong;
            if (currentSong == null)
            {
                await ReplyAsync("No songs recently played.");
                return;
            }

            await ReplyAsync("Generating mp3 file... please wait.");

            var mp3Service = MusicServiceProvider.GetMp3Service(Context.Guild.Id);

            if (!mp3Service.Recipients.Contains(Context.User))
                mp3Service.Recipients.Add(Context.User);

            if (!mp3Service.GeneratingMp3)
            {
                var url = await mp3Service.GenerateMp3Async(currentSong);
                while (mp3Service.Recipients.TryTake(out IUser user))
                {
                    var dm = await user.CreateDMChannelAsync();
                    await dm.SendMessageAsync($"Requested url at: {url}");
                }
            }
        }

        [Command("ygyl")]
        public async Task YouGrooveYouLoseAsync(string board = "wsg")
        {
            var songUrls = await _chanService.GetPostsWithVideosAsync(board);

            foreach (var songUrl in songUrls)
            {
                var song = !songUrl.EndsWith("webm")
                    ? await _youtubeInfoService.GetSongInfoAsync(songUrl)
                    : new Song(songUrl);
                Context.MusicService.QueueSong(song);
            }

            if (Context.MusicService.CurrentSong == null)
                await Context.MusicService.PlayAsync(Context.Message);
        }
    }
}