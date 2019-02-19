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
using MagicConchBot.Helpers;
using MagicConchBot.Resources;

namespace MagicConchBot.Modules
{
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

        private static int _questionCount;

        private static int _responseNumber;

        [Command("info")]
        [Summary("Get info from the server.")]
        public async Task InfoAsync()
        {
            var application = await Context.Client.GetApplicationInfoAsync();

            var embed = new EmbedBuilder {Color = Constants.MaterialBlue};

            var osUptime = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? await GetLinuxUptime() : "";
            var upToDate = AppHelper.Version.Contains("dev") || await WebHelper.UpToDateWithGitHub();

            embed.AddField(f =>
            {
                f.WithName("Info")
                    .WithValue($"**Author:**\n{application.Owner.Username} (ID {application.Owner.Id})\n\n" +
                               $"**Version:**\n{AppHelper.Version} - " + (upToDate ? "Up to date! :)" : "Update needed") + "\n\n" +
                               $"**Library:**\nDiscord.Net ({DiscordConfig.Version})\n\n" +
                               $"**Runtime:**\n{RuntimeInformation.FrameworkDescription} {RuntimeInformation.OSArchitecture}\n\n" +
                               $"**Uptime:**\n{GetUptime()}\n\n" +
                               (osUptime == "" ? "" : $"**OS Uptime:**\n{osUptime}\n\n") +
                               $"**GitHub:**\n{Constants.RepoLink}\n\n\n\n");
            });
            embed.AddField(f =>
            {
                f.WithName("Stats")
                        .WithValue($"**Heap Size:**\n{GetHeapSize()} MB\n\n" +
                                   $"**Guilds:**\n{((DiscordShardedClient) Context.Client).Guilds.Count}\n\n" +
                                   $"**Channels:**\n{((DiscordShardedClient) Context.Client).Guilds.Sum(g => g.Channels.Count)}\n\n" +
                                   $"**Users:**\n{((DiscordShardedClient) Context.Client).Guilds.Sum(g => g.Users.Count)}\n\n" +
                                   $"**Shards:**\n{((DiscordShardedClient) Context.Client).Shards.Count}");
            });

            await ReplyAsync(string.Empty, false, embed.Build());
        }

        [Command("conch")]
        [Alias("magicconch")]
        [Summary("Have the Conch declare it's reign.")]
        public async Task MagicConchAsync()
        {
            await ReplyAsync("All hail the magic conch.", true);
        }

        [Command("conch")]
        [Alias("magicconch")]
        [Summary("Ask the magic conch a question.")]
        public async Task MagicConchAsync([Remainder] [Summary("The question to ask.")] string question)
        {
            if (Regex.IsMatch(question, @"can i have something to eat\?*", RegexOptions.IgnoreCase))
            {
                switch (_questionCount++)
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
                await ReplyAsync($"{Replies[_responseNumber++]}");
                if (_responseNumber >= Replies.Length)
                    _responseNumber = 0;
            }
        }

        private static string GetUptime()
            => (DateTime.Now - Process.GetCurrentProcess().StartTime).ToString(@"dd\.hh\:mm\:ss");

        private static string GetHeapSize()
            => Math.Round(GC.GetTotalMemory(true) / (1024.0 * 1024.0), 2).ToString(CultureInfo.InvariantCulture);

        private static async Task<string> GetLinuxUptime()
        {
            var processInfo = new ProcessStartInfo("uptime", "-s")
            {
                RedirectStandardOutput =  true,
                UseShellExecute = false
            };
            var uptimeProcess = Process.Start(processInfo);
            var output = await uptimeProcess.StandardOutput.ReadLineAsync();
            var upSince = DateTime.ParseExact(output, "yyyy-MM-dd HH:mm:ss", DateTimeFormatInfo.InvariantInfo);
            return (upSince - DateTime.Now).ToString(@"dd\.hh\:mm\:ss");
        }
    }
}