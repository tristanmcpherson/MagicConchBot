using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using MagicConchBot.Services;
using MagicConchBot.Helpers;

namespace MagicConchBot.Modules
{
    public class WikiModule : ModuleBase
    {
        private readonly StardewValleyService _service;

        public WikiModule(StardewValleyService service)
        {
            _service = service;
        }

        [Command("sdv")]
        [Summary("Searches the StardewValley Wiki for information")]
        public async Task StardewValleySearchAsync([Remainder] [Summary("The query to look up.")] string query)
        {
            var split = query.Split('#');
            var section = "";

            if (split.Length > 1)
            {
                section = split.Last();
                query = query.Replace("#" + section, "");
            }

            var summary = await _service.GetSummaryFromSearchAsync(query, section);
            if (summary == null)
            {
                await ReplyAsync($"Sorry, we couldn't find anything on the wiki regarding: {query}");
            }
            else
            {
                var words = query.Split(' ').Select(w => w.ToLower());
                var capitalized = words.Select(w => char.ToUpper(w[0]) + w.Substring(1));
                var joined = string.Join(" ", capitalized);

                var title = joined + (section == "" ? "" : $"#{section}");

                var embedBuilder =
                    new EmbedBuilder().WithTitle($"Stardew Valley Wiki - {title}").WithDescription($"{summary}");

                if (summary.Length > 1500)
                {
                    var parts = summary.SplitByLength(1500);

                    foreach (var part in parts)
                    {
                        embedBuilder.WithDescription(part);
                        await ReplyAsync("", false, embedBuilder.Build());
                    }
                }
                else
                {
                    await ReplyAsync("", false, embedBuilder.Build());
                }
            }
        }
    }
}