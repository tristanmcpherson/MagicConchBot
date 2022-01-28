using System.Linq;
using System.Threading.Tasks;
using Discord.Interactions;

using MagicConchBot.Attributes;
using MagicConchBot.Common.Enums;
using MagicConchBot.Helpers;
using GroupAttribute = Discord.Interactions.GroupAttribute;

namespace MagicConchBot.Modules
{
    [RequireUserInVoiceChannel]
    [RequireBotControlRole]
    [Group("queue", "Queue Commands")]
    public class QueueModule : InteractionModuleBase<ConchInteractionCommandContext>
    {
        [SlashCommand("list", "Lists all the songs in the queue.")]
        public async Task ListQueueAsync()
        {
            var songs = Context.MusicService.SongList;

            if (songs.Count == 0)
            {
                await RespondAsync("There are no songs currently in queue.");
                return;
            }

            if (songs.Count < 5)
            {
                await RespondAsync(string.Empty, embeds: new[] { songs.First().GetEmbed($"[Current]: {songs.First().Name}") });
                for (var i = 1; i < songs.Count; i++)
                    await RespondAsync(string.Empty, embeds: new[] { songs[i].GetEmbed($"{i}: {songs[i].Name}") });
            }
            else
            {
                await SongHelper.DisplaySongsClean(songs.ToArray(), Context);
            }
        }

        [SlashCommand("clear", "Clears all the songs from the queue")]
        public async Task ClearAsync()
        {
            Context.MusicService.ClearQueue();
            await RespondAsync("Successfully removed all songs from queue.");
        }

        [SlashCommand("remove", "Song number to remove.")]
        public async Task RemoveAsync(int songNumber)
        {
            var song = Context.MusicService.RemoveSong(songNumber);
            if (song == null)
                await RespondAsync($"No song at position: {songNumber}");
            else
                await RespondAsync("Successfully removed song from queue:", embeds: new[] { song.GetEmbed($"{song.Name}") });
        }

        [SlashCommand("mode", "Change the queue mode to queue (remove after playing) or playlist (repeat).")]
        public async Task ChangeModeAsync(PlayMode mode)
        {
            Context.MusicService.PlayMode = mode;
            await RespondAsync($"Successfully changed mode to {mode} mode.");
        }
    }
}