namespace MagicConchBot.Modules
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    using Discord;
    using Discord.Commands;
    using Discord.WebSocket;

    using MagicConchBot.Resources;

    [Name("Default Commands")]
    public class PublicModule : ModuleBase
    {
        private static readonly string[] Replies =
        {
            "Maybe someday.",
            "I don't think so",
            "No",
            "N o o o o o!",
            "Try asking again"
        };

        private static int questionCount;

        private static int responseNumber;

        [Command("info"), Summary("Get info from the server.")]
        public async Task InfoAsync()
        {
            var application = await Context.Client.GetApplicationInfoAsync();

            var embed = new EmbedBuilder {Color = Constants.MaterialBlue};

            embed.AddField(f =>
            {
                f.WithName("Info")
                 .WithValue($"**Author:**\n{application.Owner.Username} (ID {application.Owner.Id})\n\n" +
                            $"**Library:**\nDiscord.Net ({DiscordConfig.Version})\n\n" +
                            $"**Runtime:**\n{RuntimeInformation.FrameworkDescription} {RuntimeInformation.OSArchitecture}\n\n" +
                            $"**Uptime:**\n{GetUptime()}\n\n" +
                            $"**GitHub:**\n{Constants.RepoLink}\n\n\n\n");
            });
            embed.AddField(f =>
            {
                f.WithName("Stats")
                 .WithValue($"**Heap Size:**\n{GetHeapSize()} MB\n\n" +
                            $"**Guilds:**\n{((DiscordSocketClient) Context.Client).Guilds.Count}\n\n" +
                            $"**Channels:**\n{((DiscordSocketClient) Context.Client).Guilds.Sum(g => g.Channels.Count)}\n\n" +
                            $"**Users:**\n{((DiscordSocketClient) Context.Client).Guilds.Sum(g => g.Users.Count)}");
            });

            await ReplyAsync("", false, embed.Build());
        }

        [Command("conch"), Alias("magicconch"), Summary("Have the Conch declare it's reign.")]
        public async Task MagicConchAsync()
        {
            await ReplyAsync("All hail the magic conch.", true);
        }

        [Command("conch"), Alias("magicconch"), Summary("Ask the magic conch a question.")]
        public async Task MagicConchAsync([Remainder, Summary("The question to ask.")] string question)
        {
            if (Regex.IsMatch(question, @"can i have something to eat\?*", RegexOptions.IgnoreCase))
            {
                switch (questionCount++)
                {
                    case 2:
                        await ReplyAsync("Try asking again.", true);
                        break;
                    case 3:
                        await ReplyAsync("Nooooooh. ~~", true);
                        break;
                    default:
                        await ReplyAsync("No", true);
                        break;
                }
            }
            else
            {
                await ReplyAsync($"{Replies[responseNumber++]}");
                if (responseNumber >= Replies.Length)
                {
                    responseNumber = 0;
                }
            }
        }

        private static string GetUptime()
            => (DateTime.Now - Process.GetCurrentProcess().StartTime).ToString(@"dd\.hh\:mm\:ss");

        private static string GetHeapSize() => Math.Round(GC.GetTotalMemory(true) / (1024.0 * 1024.0), 2).ToString(CultureInfo.InvariantCulture);
    }
}
