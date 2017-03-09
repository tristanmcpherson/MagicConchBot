namespace MagicConchBot.Modules
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    using Discord.Commands;

    using log4net;

    using MagicConchBot.Attributes;
    using MagicConchBot.Common.Interfaces;
    using MagicConchBot.Common.Types;
    using MagicConchBot.Services;

    [RequireUserInVoiceChannel]
    [RequireBotControlRole]
    [Name("Music Commands")]
    public class MusicModule : ModuleBase<MusicCommandContext>
    {
        private static readonly ILog Log =
            LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly GoogleApiService googleApiService;
        private readonly List<IMusicInfoService> musicInfoServices;
        private readonly ChanService chanService;
        private readonly YouTubeInfoService youtubeInfoService;

        public MusicModule(IDependencyMap map)
        {
            musicInfoServices = new List<IMusicInfoService>
            {
                map.Get<YouTubeInfoService>(),
                map.Get<SoundCloudInfoService>()
            };
            googleApiService = map.Get<GoogleApiService>();
            chanService = map.Get<ChanService>();
            youtubeInfoService = map.Get<YouTubeInfoService>();
        }
        
        [Command("play"), Summary("Plays a song from YouTube or SoundCloud. Alternatively uses the search terms to find a corresponding video on YouTube.")]
        public async Task PlayAsync()
        {
            if (Context.MusicService.GetCurrentSong() != null)
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
            var url = "";
            var seekTo = TimeSpan.Zero;
            Song song = null;

            var isUrlRegex = new Regex(@"(\b(https?):\/\/)?[-A-Za-z0-9+\/%?=_!.]+\.[-A-Za-z0-9+&#\/%=_]+");

            switch (terms.Length)
            {
                case 1:
                    if (isUrlRegex.IsMatch(urlOrQuery))
                    {
                        url = urlOrQuery;
                    }

                    break;
                case 2:
                    // first term is url, therefore second should be time, if not then ignore second argument
                    if (isUrlRegex.IsMatch(terms[0]))
                    {
                        url = terms[0];
                        if (!TimeSpan.TryParse(terms[1], out seekTo))
                        {
                            seekTo = TimeSpan.Zero;
                        }
                    }

                    break;
            }

            // input is not a url, search for it on YouTube
            if (url == "")
            {
                url = await googleApiService.GetFirstVideoByKeywordsAsync(urlOrQuery);
            }

            foreach (var service in musicInfoServices)
            {
                if (!service.Regex.IsMatch(url))
                {
                    continue;
                }

                song = await service.GetSongInfoAsync(url);
                
                // if url contains time info but it is specified, overwrite
                if (seekTo != TimeSpan.Zero)
                {
                    song.SeekTo = seekTo;
                }
                
                // song info found, stop info service search
                break;
            }

            // Song info not found from search or url
            if (song == null)
            {
                // url invalid
                if (url == "")
                {
                    await ReplyAsync("Incorrect input or invalid url specified.");
                    return;
                }

                // url valid
                song = new Song(url);
            }

            // valid url but song information not found by any song info service
            Log.Info($"Queued song: {song.Name} - {song.Url} at {song.SeekTo}.");

            // add to queue
            Context.MusicService.QueueSong(song);

            await ReplyAsync("Queued song:", false, song.GetEmbed($"{song.Name}"));

            // if not playing, start playing and then the player service
            if (Context.MusicService.GetCurrentSong() == null)
            {
                Log.Info("No song currently playing, playing.");
                await Context.MusicService.PlayAsync(Context.Message);
            }
        }
        
        [Command("stop"), Summary("Stops the bot if it is playing music and disconnects it from the voice channel.")]
        public async Task StopAsync()
        {
            Context.MusicService.Stop();
            await ReplyAsync(Context.MusicService.GetCurrentSong() != null ? "Music stopped playing." : "No music currently playing.");
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
            var currentVol = Context.MusicService.ChangeVolume(volume);
            await ReplyAsync($"Current volume set to {currentVol}.");
        }
        
        [Command("current"), Summary("Displays the current song")]
        public async Task CurrentSongAsync()
        {
            var song = Context.MusicService.GetCurrentSong();
            if (song == null)
            {
                await ReplyAsync("No song is currently playing.");
            }
            else
            {
                await ReplyAsync("", false, song.GetEmbed());
            }
        }

        [Command("mp3"), Summary("Generates a link to the mp3 of the current song playing or the last song played.")]
        public async Task GenerateMp3Async()
        {
            var url = await Context.MusicService.GenerateMp3Async().ContinueWith(async t =>
            {
                var dm = await Context.User.CreateDMChannelAsync();
                await dm.SendMessageAsync($"Requested url at: {t.Result}");
            });

            if (url == null)
            {
                await ReplyAsync("Generating mp3 file... please wait.");
            }
        }

        //[Command("ygyl")]
        //public async Task YouGrooveYouLoseAsync()
        //{
        //    var musicService = _musicServiceProvider.GetService(Context.OwnerGuildId.Id);
        //    var songUrls = await _chanService.GetPostsWithVideos("wsg");

        //    foreach (var songUrl in songUrls)
        //    {
        //        Song song;
        //        if (!songUrl.EndsWith("webm"))
        //        {
        //            song = await _youtubeInfoService.GetSongInfoAsync(songUrl);
        //        }
        //        else
        //        {
        //            song = new Song("Unknown", TimeSpan.Zero, songUrl);
        //        }

        //        musicService.QueueSong(song);
        //    }

        //    await musicService.PlayAsync(Context.Message);
        //}
        [Command("ygyl")]
        public async Task YouGrooveYouLoseAsync()
        {
            var songUrls = await chanService.GetPostsWithVideosAsync("wsg");

            foreach (var songUrl in songUrls)
            {
                var song = !songUrl.EndsWith("webm") ? await youtubeInfoService.GetSongInfoAsync(songUrl) : new Song(songUrl);
                Context.MusicService.QueueSong(song);
            }

            await Context.MusicService.PlayAsync(Context.Message);
        }

        [Command("ygyl")]
        public async Task YouGrooveYouLoseAsync(string board)
        {
            var songUrls = await chanService.GetPostsWithVideosAsync(board);

            foreach (var songUrl in songUrls)
            {
                var song = !songUrl.EndsWith("webm") ? await youtubeInfoService.GetSongInfoAsync(songUrl) : new Song(songUrl);
                Context.MusicService.QueueSong(song);
            }

            await Context.MusicService.PlayAsync(Context.Message);
        }
    }
}
