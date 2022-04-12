using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Discord.Interactions;

using MagicConchBot.Attributes;
using MagicConchBot.Common.Enums;
using MagicConchBot.Common.Types;
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
            var songs = Context.MusicService.GetSongs();

            if (songs.Count == 0)
            {
                await RespondAsync("There are no songs currently in queue.");
                return;
            }

            if (songs.Count < 5)
            {
                await RespondAsync(embeds: songs.Select((song, i) => song.GetEmbed($"{(i != 0 ? i : "[Current]")}: {song.Name}")).ToArray());
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
            var song = await Context.MusicService.RemoveSong(songNumber);
            await song
                .Map(async song => await RespondAsync("Successfully removed song from queue:", embed: song.GetEmbed($"{song.Name}")))
                .ExecuteNoValue(async () => await RespondAsync($"No song at position: {songNumber}"));
        }

        [SlashCommand("mode", "Change the queue mode to queue (remove after playing) or playlist (repeat).")]
        public async Task ChangeModeAsync(PlayMode mode)
        {
            Context.MusicService.PlayMode = mode;
            await RespondAsync($"Successfully changed mode to {mode} mode.");
        }
    }
}