using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using MagicConchBot.Attributes;
using MagicConchBot.Common.Enums;
using MagicConchBot.Common.Interfaces;
using MagicConchBot.Common.Types;
using MagicConchBot.Helpers;
using MagicConchBot.Services;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace MagicConchBot.Modules
{
    [RequireUserInVoiceChannel]
    [RequireBotControlRole]
    [Name("Music Commands")]
    public class MusicModule : ModuleBase<ConchCommandContext>
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly ISongResolutionService _songResolutionService;

        private readonly ChanService _chanService;

        private readonly GoogleApiInfoService _googleApiInfoService;

        public MusicModule(IServiceProvider serviceProvider)
        {
            _songResolutionService = serviceProvider.GetService<ISongResolutionService>();

            _googleApiInfoService = serviceProvider.GetService<GoogleApiInfoService>();
            _chanService = serviceProvider.GetService<ChanService>();
        }

        [Command("play"), Alias("resume")]
        [Summary(
            "Plays a song from YouTube or SoundCloud. Alternatively uses the search terms to find a corresponding video on YouTube."
        )]
        public async Task Play()
        {
            if (Context.MusicService.PlayerState == PlayerState.Playing || Context.MusicService.PlayerState == PlayerState.Loading)
            {
                await ReplyAsync("Song already playing.");
            }
            else if (Context.MusicService.SongList.Count > 0)
            {
                await Context.MusicService.Play(Context);
                await ReplyAsync("Resuming queue.");
            }
            else
            {
                await ReplyAsync("No songs currently in the queue.");
            }
        }

        [Command("play", RunMode = RunMode.Async), Alias("p")]
        [Summary("Plays a song from YouTube or SoundCloud. Alternatively uses the search terms to find a corresponding video on YouTube.")]
        public async Task Play([Remainder] [Summary("The url or search terms optionally followed by a time to start at (e.g. 00:01:30 for 1m 30s.)")] string query)
        {
            var terms = query.Split(' ');
            var startTime = TimeSpan.Zero;

            string url;

            if (terms.Length > 1)
            {
                if (TimeSpan.TryParseExact(terms.Last(), new[] {@"mm\:ss", @"hh\:mm\:ss"}, CultureInfo.InvariantCulture, out startTime))
                    query = query.Replace(" " + terms.Last(), string.Empty);
                else
                    startTime = TimeSpan.Zero;
                
            }

            if (!WebHelper.UrlRegex.IsMatch(query))
            {
                var firstTerm = terms.FirstOrDefault() ?? "";
                if (firstTerm == "yt")
                {
                    query = query.Replace(terms.First() + " ", string.Empty);
                }

                url = await _googleApiInfoService.GetFirstVideoByKeywordsAsync(query);
            }
            else
            {
                url = query;
            }

            // url invalid
            if (string.IsNullOrEmpty(url))
            {
                await ReplyAsync($"Could not find any videos for: {query}");
                return;
            }

            var youtubeMatch = _googleApiInfoService.Regex.Match(url);
            var playlistId = youtubeMatch.Groups["PlaylistId"].Value;
            if (playlistId != "")
            {
                await ReplyAsync("Queueing songs from playlist. This may take a while, please wait.");
                var songs = await _googleApiInfoService.GetSongsByPlaylistAsync(playlistId);

                Context.MusicService.SongList.AddRange(songs);

                await ReplyAsync($"Queued {songs.Count} songs from playlist.");
            }
            else
            {
                var song = await _songResolutionService.ResolveSong(url, startTime);

                // add to queue
                Context.MusicService.QueueSong(song);

                await ReplyAsync("Queued song:", false, song.GetEmbed());
            }

            // if not playing, start playing and then the player service
            if (Context.MusicService.PlayerState == PlayerState.Stopped || Context.MusicService.PlayerState == PlayerState.Paused)
            {
                Log.Info("No song currently playing, playing.");
                await Context.MusicService.Play(Context).ConfigureAwait(false);
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

        [Command("mp3", RunMode = RunMode.Async)]
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

			await Context.Mp3Service.GetMp3(currentSong, Context.User);
        }

        [Command("ygyl", RunMode = RunMode.Async)]
        public async Task YouGrooveYouLoseAsync(string board = "wsg")
        {
            var songUrls = await _chanService.GetPostsWithVideosAsync(board);

            foreach (var songUrl in songUrls)
            {
                var song = !songUrl.EndsWith("webm")
                    ? await _googleApiInfoService.GetSongInfoAsync(songUrl)
                    : new Song(MusicType.Other, songUrl);
                Context.MusicService.QueueSong(song);
            }

            if (Context.MusicService.CurrentSong == null)
                await Context.MusicService.Play(Context);
        }
    }
}