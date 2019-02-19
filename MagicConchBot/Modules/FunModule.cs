using System.Threading.Tasks;
using Discord.Commands;

namespace MagicConchBot.Modules
{
    public class FunModule : ModuleBase
    {
        [Command("ghaussi")]
        public async Task GhaussiSpeak([Remainder] string input)
        {
            var words = input.Split(' ');
            for (var i = 0; i < words.Length; i++)
            {
                if (words[i] == "")
                    continue;

                var replace = words[i].ToLower();

                if (replace == "third")
                    replace = "turd";
                else
                    replace = replace.Replace("th", "t");

                words[i] = replace.Replace("a", "u");
            }

            await ReplyAsync($"Ghaussi: {string.Join(" ", words)}");
        }
    }
}