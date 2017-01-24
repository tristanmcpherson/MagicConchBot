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

namespace MagicConchBot.Modules
{
    [RequireUserInVoiceChannel]
    [RequireUserRole(Program.RequiredRoleName)]
    [Name("Music Commands")]
    public class MusicModule : ModuleBase
    {
        private readonly IMusicService _musicService;
        private readonly GoogleApiService _googleApiService;
        private readonly List<IMusicInfoService> _musicInfoServices;

        private static readonly ILog Log =
            LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public MusicModule(IDependencyMap map)
        {
            _musicInfoServices = new List<IMusicInfoService>
            {
                map.Get<YouTubeInfoService>(),
                map.Get<SoundCloudInfoService>()
            };
            _musicService = map.Get<FfmpegMusicService>();
            _googleApiService = map.Get<GoogleApiService>();
        }
        
        [Command("play"), Summary("Plays a song from YouTube or SoundCloud. Alternatively uses the search terms to find a corresponding video on YouTube.")]
        public async Task PlayAsync()
        {
            if (_musicService.QueuedSongs().Count > 0)
            {
                await _musicService.PlayAsync(Context.Message);
                await ReplyAsync("Resuming queue.");
            }
            else
            {
                await ReplyAsync("No songs currently in the queue.");
            }
        }
        
        [Command("play"), Summary("Plays a song from YouTube or SoundCloud. Alternatively uses the search terms to find a corresponding video on YouTube.")]
        public async Task PlayAsync([Remainder, Summary("The url or search terms.")] string urlOrQuery)
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
            _musicService.QueueSong(song);
            await ReplyAsync("Queued song:", false, song.GetEmbed($"{song.Name}"));

            // if not playing, start playing and then the player service
            if (_musicService.GetCurrentSong() == null)
            {
                Log.Info("No song currently playing, playing.");
                await _musicService.PlayAsync(Context.Message);
            }
        }
        
        [Command("stop"), Summary("Stops the bot if it is playing music and disconnects it from the voice channel.")]
        public async Task StopAsync()
        {
            var stopped = _musicService.Stop();
            await ReplyAsync(stopped ? "Music stopped playing." : "No music currently playing.");
        }
        
        [Command("pause"), Summary("Pauses the current song.")]
        public async Task PauseAsync()
        {
            var paused = _musicService.Pause();
            await ReplyAsync(paused ? "Music paused successfully." : "No music currently playing.");
        }
        
        [Command("skip"), Summary("Skips the current song if one is playing.")]
        public async Task SkipAsync()
        {
            var skipped = _musicService.Skip();
            await ReplyAsync(skipped ? "Skipped current song." : "No song available to skip");
        }

        [Command("volume"), Alias("vol"), Summary("Changes the volume of the current playing song and future songs.")]
        public async Task ChangeVolumeAsync([Summary("The volume to set the song to from between 0 and 100.")] int volume)
        {
            var currentVol = _musicService.ChangeVolume(volume);
            await ReplyAsync($"Current volume set to {currentVol}.");
        }
        
        [Command("current"), Summary("Displays the current song")]
        public async Task CurrentSongAsync()
        {
            var song = _musicService.GetCurrentSong();
            if (song == null)
            {
                await ReplyAsync("No song is currently playing.");
            }
            else
            {
                await ReplyAsync("", false, song.GetEmbed());
            }
        }

        [Command("mp3"), Summary("Generates a link to the mp3 of the song last played.")]
        public async Task GenerateMp3Async()
        {
            var url = await _musicService.GenerateMp3Async();
            if (url == null)
            {
                await ReplyAsync("Generating mp3 file... please wait.");
                return;
            }
            var dm = await Context.User.CreateDMChannelAsync();
            await dm.SendMessageAsync($"Requested url at: {url}");
        }
    }
}
