using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using log4net;
using MagicConchBot.Attributes;
using MagicConchBot.Common.Interfaces;
using MagicConchBot.Common.Types;
using MagicConchBot.Services;

namespace MagicConchBot.Modules
{
    [RequireUserInVoiceChannel]
    [RequireBotControlRole]
    [Name("Music Commands")]
    public class MusicModule : ModuleBase<MusicCommandContext>
    {
        private static readonly ILog Log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly Regex UrlRegex =
            new Regex(@"(\b(https?):\/\/)?[-A-Za-z0-9+\/%?=_!.]+\.[-A-Za-z0-9+&#\/%=_]+");

        private readonly GoogleApiService _googleApiService;
        private readonly List<IMusicInfoService> _musicInfoServices;
        private readonly ChanService _chanService;
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
        
        [Command("play"), Summary("Plays a song from YouTube or SoundCloud. Alternatively uses the search terms to find a corresponding video on YouTube.")]
        public async Task PlayAsync()
        {
            if (Context.MusicService.CurrentSong != null)
            {
                await ReplyAsync("Song already playing.");
            }
            else if (Context.MusicService.QueuedSongs().Count > 0)
            {
                await Context.MusicService.PlayAsync(Context.Message);
                await ReplyAsync("Resuming queue.");
            }
            else
            {
                await ReplyAsync("No songs currently in the queue.");
            }
        }
        
        [Command("play"), Summary("Plays a song from YouTube or SoundCloud. Alternatively uses the search terms to find a corresponding video on YouTube.")]
        public async Task PlayAsync([Remainder, Summary("The url or search terms optionally followed by a time to start at (e.g. 00:01:30 for 1m 30s.)")] string urlOrQuery)
        {
            var terms = urlOrQuery.Split(' ');
            var startTime = TimeSpan.Zero;
            string url;
            Song song = null;

            if (terms.Length > 1)
            {
                if (TimeSpan.TryParseExact(terms.Last(), new []{@"mm\:ss", @"hh\:mm\:ss"}, CultureInfo.InvariantCulture,  out startTime))
                {
                    // last term is a time mark, remove it
                    urlOrQuery = urlOrQuery.Replace(" " + terms.Last(), string.Empty);
                }
                else
                {
                    startTime = TimeSpan.Zero;
                }
            }

            if (UrlRegex.IsMatch(urlOrQuery))
            {
                url = urlOrQuery;
            }
            else
            {
                // input is not a url, search for it on YouTube
                url = await _googleApiService.GetFirstVideoByKeywordsAsync(urlOrQuery);
            }

            foreach (var service in _musicInfoServices)
            {
                if (!service.Regex.IsMatch(url))
                {
                    continue;
                }

                song = await service.GetSongInfoAsync(url);
                
                // url may contain time info but it is specified, overwrite
                if (startTime != TimeSpan.Zero)
                {
                    song.StartTime = startTime;
                }
                
                // song info found, stop info service search
                break;
            }

            // Song info not found from search or url
            if (song == null)
            {
                // url invalid
                if (url == string.Empty)
                {
                    await ReplyAsync("Incorrect input or invalid url specified.");
                    return;
                }

                // url valid
                song = new Song(url);
            }

            // valid url but song information not found by any song info service
            Log.Info($"Queued song: {song.Name} - {song.Url} at {song.StartTime}.");

            // add to queue
            Context.MusicService.QueueSong(song);

            await ReplyAsync("Queued song:", false, song.GetEmbed($"{song.Name}"));

            // if not playing, start playing and then the player service
            if (Context.MusicService.CurrentSong == null)
            {
                Log.Info("No song currently playing, playing.");
                await Context.MusicService.PlayAsync(Context.Message);
            }
        }
        
        [Command("stop"), Summary("Stops the bot if it is playing music and disconnects it from the voice channel.")]
        public async Task StopAsync()
        {
            Context.MusicService.Stop();
            await ReplyAsync(Context.MusicService.CurrentSong != null ? "Music stopped playing." : "No music currently playing.");
        }
        
        [Command("pause"), Summary("Pauses the current song.")]
        public async Task PauseAsync()
        {
            var paused = Context.MusicService.Pause();
            await ReplyAsync(paused ? "Music paused successfully." : "No music currently playing.");
        }
        
        [Command("skip"), Summary("Skips the current song if one is playing.")]
        public async Task SkipAsync()
        {
            var skipped = Context.MusicService.Skip();
            await ReplyAsync(skipped ? "Skipped current song." : "No song available to skip");
        }

        [Command("volume"), Alias("vol"), Summary("Gets the current volume.")]
        public async Task ChangeVolumeAsync()
        {
            await ReplyAsync($"{Context.MusicService.Volume}");
        }

        [Command("volume"), Alias("vol"), Summary("Changes the volume of the current playing song and future songs.")]
        public async Task ChangeVolumeAsync([Summary("The volume to set the song to from between 0 and 100.")] int volume)
        {
            Context.MusicService.Volume = volume;
            await ReplyAsync($"Current volume set to {Context.MusicService.Volume}.");
        }
        
        [Command("current"), Summary("Displays the current song")]
        public async Task CurrentSongAsync()
        {
            var song = Context.MusicService.CurrentSong;
            if (song == null)
            {
                await ReplyAsync("No song is currently playing.");
            }
            else
            {
                await ReplyAsync(string.Empty, false, song.GetEmbed());
            }
        }
            
        [Command("mp3"), Summary("Generates a link to the mp3 of the current song playing or the last song played.")]
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
            {
                mp3Service.Recipients.Add(Context.User);
            }

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
                var song = !songUrl.EndsWith("webm") ? await _youtubeInfoService.GetSongInfoAsync(songUrl) : new Song(songUrl);
                Context.MusicService.QueueSong(song);
            }

            if (Context.MusicService.CurrentSong == null)
            {
                await Context.MusicService.PlayAsync(Context.Message);
            }
        }
    }
}
