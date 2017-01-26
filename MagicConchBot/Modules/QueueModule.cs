using System.Linq;
using Discord.Commands;
using System.Threading.Tasks;
using MagicConchBot.Attributes;
using MagicConchBot.Common.Enums;
using MagicConchBot.Common.Interfaces;
using MagicConchBot.Services;
using MagicConchBot.Resources;

namespace MagicConchBot.Modules
{
    [RequireUserInVoiceChannel]
    [RequireUserRole(Constants.RequiredRoleName)]
    [Name("Queue Commands"), Group("queue")]
    public class QueueModule : ModuleBase
    {
        private readonly IMusicService _musicService;

        public QueueModule(IDependencyMap map)
        {
            _musicService = map.Get<FfmpegMusicService>();
        }

        [Command, Summary("Lists all the songs in the queue.")]
        public async Task ListQueueAsync()
        {
            var songs = _musicService.QueuedSongs();

            if (songs.Count == 0)
            {
                await ReplyAsync("There are no songs currently in queue.");
                return;
            }

            await ReplyAsync("", false, songs.First().GetEmbed());
            for (var i = 1; i < songs.Capacity; i++)
            {
                await ReplyAsync("", false, songs[i].GetEmbed($"{i}: {songs[i].Name}"));
            }
        }

        [Command("clear"), Summary("Clears all the songs from the queue")]
        public async Task ClearAsync()
        {
            _musicService.ClearQueue();
            await ReplyAsync("Successfully removed all songs from queue.");
        }

        [Command("remove")]
        public async Task RemoveAsync([Summary("Song number to remove.")] int songNumber)
        {
            var song = _musicService.DequeueSong(songNumber);
            await ReplyAsync("Successfully removed song from queue:", false, song.GetEmbed($"{song.Name}"));
        }

        [Command("mode"), Alias("changemode"), Summary("Change the queue mode to queue (removes songs after playing) or playlist (keeps on playing through the queue).")]
        public async Task ChangeModeAsync([Summary("The mode to change to, either `playlist` or `queue`.")] string mode)
        {
            _musicService.ChangePlayMode(mode.ToLower() == "playlist" ? PlayMode.Playlist : PlayMode.Queue);
            await ReplyAsync($"Play mode changed to {mode}");
        }
    }
}