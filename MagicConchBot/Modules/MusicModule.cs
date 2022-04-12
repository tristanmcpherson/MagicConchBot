using System;
using System.IO;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Discord.Commands;
using Discord.Interactions;
using MagicConchBot.Attributes;
using MagicConchBot.Common.Enums;
using MagicConchBot.Common.Types;
using MagicConchBot.Helpers;
using MagicConchBot.Resources;
using MagicConchBot.Services;
using NLog;
using RunMode = Discord.Interactions.RunMode;

namespace MagicConchBot.Modules
{
    [RequireUserInVoiceChannel]
    [RequireBotControlRole]
    [Name("Music Commands")]
    public class MusicModule : InteractionModuleBase<ConchInteractionCommandContext>
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly IMp3ConverterService _mp3Service;
        private readonly ISongResolutionService _songResolutionService;
        private readonly YoutubeInfoService _googleApiInfoService;

        public MusicModule(IMp3ConverterService mp3Service, ISongResolutionService songResolutionService, YoutubeInfoService googleApiInfoService) {
            _mp3Service = mp3Service;
            _songResolutionService = songResolutionService;
            _googleApiInfoService = googleApiInfoService;
        }

        [SlashCommand(
            "resume",
            "Plays a song from YouTube or SoundCloud or search for a song on YouTube",
            runMode: RunMode.Async), Alias("p")]
        public async Task Resume() {
            if (Context.MusicService.IsPlaying) {
                await RespondAsync("Song already playing.");
            } else if (Context.MusicService.GetSongs().Count > 0) {
                await Context.MusicService.Play(Context, Context.Settings);
                await RespondAsync("Resuming queue.");
            } else {
                await RespondAsync("No songs currently in the queue.");
            }
        }

        [SlashCommand(
            "play",
            "Plays a song from YouTube or SoundCloud or search for a song on YouTube",
            runMode: RunMode.Async), Alias("p")]
        public async Task Play(
            string queryOrUrl,
            TimeSpan? startTime = null)
        {
            if (queryOrUrl == null)
            {
                await Resume();
                return;
            }
            
            string url;

            if (!WebHelper.UrlRegex.IsMatch(queryOrUrl))
            {

                url = await _googleApiInfoService.GetFirstVideoByKeywordsAsync(queryOrUrl);
            }
            else
            {
                url = queryOrUrl;
            }

            // url invalid
            if (string.IsNullOrEmpty(url))
            {
                await RespondAsync($"Could not find any videos for: {queryOrUrl}");
                return;
            }

            var youtubeMatch = _googleApiInfoService.Regex.Match(url);
            var playlistId = youtubeMatch.Groups["PlaylistId"].Value;
            if (playlistId != "")
            {
                await RespondAsync("Queueing songs from playlist. This may take a while, please wait.");
                var songs = await _googleApiInfoService.GetSongsByPlaylistAsync(playlistId);

                songs.ForEach(Context.MusicService.QueueSong);

                await RespondAsync($"Queued {songs.Count} songs from playlist.");
            }
            else
            {
                Log.Info("Resolving song");
                var song = await _songResolutionService.ResolveSong(url, startTime ?? TimeSpan.Zero);

                // add to queue
                Log.Debug("Queueing song");
                Context.MusicService.QueueSong(song);

                try
                {
                    await RespondAsync(embed: song.GetEmbed());
                }
                catch { }
            }

            // if not playing, start playing and then the player service
            if (!Context.MusicService.IsPlaying)
            {
                Log.Info("No song currently playing, playing.");
                await Context.MusicService.Play(Context, Context.Settings);
            }
        }

        [SlashCommand("stop", "Stops the bot if it is playing music and disconnects it from the voice channel.")]
        public async Task StopAsync()
        {
            await Context.MusicService.Stop();
            await RespondAsync("Music stopped playing.");
        }

        [SlashCommand("pause", "Pauses the current song.")]
        public async Task PauseAsync()
        {
            await Context.MusicService.Pause();
            await RespondAsync("Music paused successfully.");
        }

        [SlashCommand("skip", "Skips the current song if one is playing.")]
        public async Task SkipAsync()
        {
            var skipped = Context.MusicService.Skip();
            await RespondAsync(skipped ? "Skipped current song." : "No song available to skip");
        }


        [SlashCommand("volume", "Gets or changes the volume of the current playing song and future songs.")]
        public async Task ChangeVolumeAsync([MinValue(0)][MaxValue(100)] int? volume = null)
        {
            if (volume == null)
            {
                await RespondAsync($"Current volume: {Context.MusicService.GetVolume() * 100}%.");
                return;
            }
            Context.MusicService.SetVolume(volume == 0 ? 0 : volume.Value / 100f);
            await RespondAsync($"Current volume set to {Context.MusicService.GetVolume() * 100}%.");
        }

        [SlashCommand("current", "Displays the current song")]
        public async Task CurrentSongAsync()
        {
            await Context.MusicService.CurrentSong
                .Map(async song =>
                    await RespondAsync(embed: song.GetEmbed())
                )
                .ExecuteNoValue(
                    async () => await RespondAsync("No song is currently playing.")
                );
        }

        [SlashCommand("loop", "Loops")]
        public async Task Loop()
        {
            Context.MusicService.PlayMode = PlayMode.Playlist;
            await RespondAsync(
                "Successfully changed mode to playlist mode. Songs will not be removed from queue after they are done playing.");
        }

        [SlashCommand("mp3", "Generates a link to the mp3 of the current song playing or the last song played.", runMode: RunMode.Async)]
        public async Task GenerateMp3Async()
        {
            await Context.MusicService.CurrentSong.Or(Context.MusicService.LastSong)
                .Map(async song => {
                    await RespondAsync("Generating mp3 file... please wait.");
                    _mp3Service.GetMp3(new(song.Name, song.StreamUri), Context.User);
                })
                .ExecuteNoValue(async () =>
                    await RespondAsync("No songs recently played.")
                );
        }

        [SlashCommand("changeintro", "...", runMode: RunMode.Async)]
        public async Task ChangeIntro(string file)
        {
            if (File.Exists(file))
            {
                Context.Settings.IntroPCM = file;
                Context.SaveSettings();
                await RespondAsync("Changed intro");
            }
            else
            {
                await RespondAsync();
            }
        }
    }
}