using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using MagicConchBot.Attributes;
using MagicConchBot.Common.Enums;

namespace MagicConchBot.Modules
{
    [RequireUserInVoiceChannel]
    [RequireBotControlRole]
    [Name("Queue Commands"), Group("queue")]
    public class QueueModule : ModuleBase<MusicCommandContext>
    {
        [Command, Summary("Lists all the songs in the queue.")]
        public async Task ListQueueAsync()
        {
            var songs = Context.MusicService.SongList;

            if (songs.Count == 0)
            {
                await ReplyAsync("There are no songs currently in queue.");
                return;
            }

            if (songs.Count < 10)
            {
                await ReplyAsync(string.Empty, false, songs.First().GetEmbed());
                for (var i = 1; i < songs.Count; i++)
                {
                    await ReplyAsync(string.Empty, false, songs[i].GetEmbed($"{i}: {songs[i].Name}"));
                }
            }
            else
            {
                var sb = new StringBuilder();
                for (var i = 0; i < songs.Count; i++)
                {
                    if (sb.Length > 1500)
                    {
                        await ReplyAsync(sb.ToString());
                        sb.Clear();
                    }

                    sb.Append($"`{i + 1}` {songs[i].Name} - **[{songs[i].LengthPretty}]**\n");
                }

                await ReplyAsync(sb.ToString());
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
            var song = Context.MusicService.RemoveSong(songNumber);
            if (song == null)
            {
                await ReplyAsync($"No song at position: {songNumber}");
            }
            else
            {
                await ReplyAsync("Successfully removed song from queue:", false, song.GetEmbed($"{song.Name}"));
            }
        }

        [Command("mode"), Alias("changemode"), Summary("Change the queue mode to queue (removes songs after playing) or playlist (keeps on playing through the queue).")]
        public async Task ChangeModeAsync([Summary("The mode to change to, either `playlist` or `queue`.")] string mode)
        {
            if (mode.ToLower() == "queue")
            {
                Context.MusicService.PlayMode = PlayMode.Queue;
                await ReplyAsync("Successfully changed mode to queue mode.");
            }
            else if (mode.ToLower() == "playlist")
            {
                Context.MusicService.PlayMode = PlayMode.Playlist;
                await ReplyAsync("Successfully changed mode to playlist mode.");
            }
            else
            {
                await ReplyAsync("Invalid play mode. Available play modes: Queue, Playlist");
            }
        }
    }
}