using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Discord.Interactions;
using MagicConchBot.Attributes;

namespace MagicConchBot.Modules
{
    [RequireBotOwner]
	[Group("admin", "Admin commands")]
	public class AdminModule : InteractionModuleBase<ConchInteractionCommandContext>
    {
        [SlashCommand("uptime", "Get the system uptime of the bot.")]
        public async Task GetUptimeAsync()
        {
            var uptime = DateTimeOffset.Now - Process.GetCurrentProcess().StartTime;
            var uptimeString = $"{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s";

            await RespondAsync($"Bot uptime: {uptimeString}");
        }
    }
}