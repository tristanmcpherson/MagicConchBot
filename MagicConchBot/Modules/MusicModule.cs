using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Discord.Commands;
using System.Threading.Tasks;
using log4net;
using MagicConchBot.Attributes;
using MagicConchBot.Common.Interfaces;
using MagicConchBot.Common.Types;
using MagicConchBot.Services;
using MagicConchBot.Resources;

namespace MagicConchBot.Modules
{
    [RequireUserInVoiceChannel]
    [RequireUserRole(Constants.RequiredRoleName)]
    [Name("Music Commands")]
    public class MusicModule : ModuleBase
    {
        private readonly MusicServiceProvider _musicServiceProvider;
        private readonly GoogleApiService _googleApiService;
        private readonly List<IMusicInfoService> _musicInfoServices;
        private readonly ChanService _chanService;
        private readonly YouTubeInfoService _youtubeInfoService;

        private static readonly ILog Log =
            LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public MusicModule(IDependencyMap map)
        {
            _musicInfoServices = new List<IMusicInfoService>
            {
                map.Get<YouTubeInfoService>(),
                map.Get<SoundCloudInfoService>()
            };
            _musicServiceProvider = map.Get<MusicServiceProvider>();
            _googleApiService = map.Get<GoogleApiService>();
            _chanService = map.Get<ChanService>();
            _youtubeInfoService = map.Get<YouTubeInfoService>();
        }
        
        [Command("play"), Summary("Plays a song from YouTube or SoundCloud. Alternatively uses the search terms to find a corresponding video on YouTube.")]
        public async Task PlayAsync()
        {
            var service = _musicServiceProvider.GetService(Context.Guild.Id);
            if (service.GetCurrentSong() != null)
            {
                await ReplyAsync("Song already playing.");
            }
            else if (service.QueuedSongs().Count > 0)
            {
                await service.PlayAsync(Context.Message);
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
            var musicService = _musicServiceProvider.GetService(Context.Guild.Id);
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
                            seekTo = TimeSpan.Zero;
                    }
                    break;
            }

            // input is not a url, search for it on YouTube
            if (url == "")
            {
                url = await _googleApiService.GetFirstVideoByKeywordsAsync(urlOrQuery);
            }

            foreach (var service in _musicInfoServices)
            {
                if (!service.Regex.IsMatch(url))
                    continue;
                song = await service.GetSongInfoAsync(url);
                // if url contains time info but it is specified, overwrite
                if (seekTo != TimeSpan.Zero)
                    song.SeekTo = seekTo;
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
                song = new Song("Unknown", TimeSpan.Zero, url);
            }

            // valid url but song information not found by any song info service

            Log.Info($"Queued song: {song.Name} - {song.Url} at {song.SeekTo}.");
            // add to queue
            musicService.QueueSong(song);
            await ReplyAsync("Queued song:", false, song.GetEmbed($"{song.Name}"));

            // if not playing, start playing and then the player service
            if (musicService.GetCurrentSong() == null)
            {
                Log.Info("No song currently playing, playing.");
                await musicService.PlayAsync(Context.Message);
            }
        }
        
        [Command("stop"), Summary("Stops the bot if it is playing music and disconnects it from the voice channel.")]
        public async Task StopAsync()
        {
            var musicService = _musicServiceProvider.GetService(Context.Guild.Id);
            musicService.Stop();
            await ReplyAsync(musicService.GetCurrentSong() != null ? "Music stopped playing." : "No music currently playing.");
        }
        
        [Command("pause"), Summary("Pauses the current song.")]
        public async Task PauseAsync()
        {
            var musicService = _musicServiceProvider.GetService(Context.Guild.Id);
            var paused = musicService.Pause();
            await ReplyAsync(paused ? "Music paused successfully." : "No music currently playing.");
        }
        
        [Command("skip"), Summary("Skips the current song if one is playing.")]
        public async Task SkipAsync()
        {
            var musicService = _musicServiceProvider.GetService(Context.Guild.Id);
            var skipped = musicService.Skip();
            await ReplyAsync(skipped ? "Skipped current song." : "No song available to skip");
        }

        [Command("volume"), Alias("vol"), Summary("Gets the current volume.")]
        public async Task ChangeVolumeAsync()
        {
            var musicService = _musicServiceProvider.GetService(Context.Guild.Id);
            await ReplyAsync($"{musicService.Volume}");
        }

        [Command("volume"), Alias("vol"), Summary("Changes the volume of the current playing song and future songs.")]
        public async Task ChangeVolumeAsync([Summary("The volume to set the song to from between 0 and 100.")] int volume)
        {
            var musicService = _musicServiceProvider.GetService(Context.Guild.Id);
            var currentVol = musicService.ChangeVolume(volume);
            await ReplyAsync($"Current volume set to {currentVol}.");
        }
        
        [Command("current"), Summary("Displays the current song")]
        public async Task CurrentSongAsync()
        {
            var musicService = _musicServiceProvider.GetService(Context.Guild.Id);
            var song = musicService.GetCurrentSong();
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
            var musicService = _musicServiceProvider.GetService(Context.Guild.Id);
            var url = await musicService.GenerateMp3Async().ContinueWith(async t =>
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
        //    var musicService = _musicServiceProvider.GetService(Context.Guild.Id);
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
        public async Task YouGrooveYouLoseAsync(string board = "wsg")
        {
            var musicService = _musicServiceProvider.GetService(Context.Guild.Id);
            var songUrls = await _chanService.GetPostsWithVideosAsync(board);

            foreach (var songUrl in songUrls)
            {
                Song song;
                if (!songUrl.EndsWith("webm"))
                {
                    song = await _youtubeInfoService.GetSongInfoAsync(songUrl);
                }
                else
                {
                    song = new Song("Unknown", TimeSpan.Zero, songUrl);
                }

                musicService.QueueSong(song);
            }

            await musicService.PlayAsync(Context.Message);
        }
    }
}
