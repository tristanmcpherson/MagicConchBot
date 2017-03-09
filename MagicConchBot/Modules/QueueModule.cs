namespace MagicConchBot.Modules
{
    using System.Linq;
    using System.Threading.Tasks;

    using Discord.Commands;

    using MagicConchBot.Attributes;
    using MagicConchBot.Common.Enums;

    [RequireUserInVoiceChannel]
    [RequireBotControlRole]
    [Name("Queue Commands"), Group("queue")]
    public class QueueModule : ModuleBase<MusicCommandContext>
    {
        [Command, Summary("Lists all the songs in the queue.")]
        public async Task ListQueueAsync()
        {
            var songs = Context.MusicService.QueuedSongs();

            if (songs.Count == 0)
            {
                await ReplyAsync("There are no songs currently in queue.");
                return;
            }

            await ReplyAsync("", false, songs.First().GetEmbed());
            for (var i = 1; i < songs.Count; i++)
            {
                await ReplyAsync("", false, songs[i].GetEmbed($"{i}: {songs[i].Name}"));
            }
        }

        [Command("clear"), Summary("Clears all the songs from the queue")]
        public async Task ClearAsync()
        {
            Context.MusicService.ClearQueue();
            await ReplyAsync("Successfully removed all songs from queue.");
        }

        [Command("remove")]
        public async Task RemoveAsync([Summary("Song number to remove.")] int songNumber)
        {
            var song = Context.MusicService.DequeueSong(songNumber);
            await ReplyAsync("Successfully removed song from queue:", false, song.GetEmbed($"{song.Name}"));
        }

        [Command("mode"), Alias("changemode"), Summary("Change the queue mode to queue (removes songs after playing) or playlist (keeps on playing through the queue).")]
        public async Task ChangeModeAsync([Summary("The mode to change to, either `playlist` or `queue`.")] string mode)
        {
            Context.MusicService.ChangePlayMode(mode.ToLower() == "playlist" ? PlayMode.Playlist : PlayMode.Queue);
            await ReplyAsync($"Play mode changed to {mode}");
        }
    }
}