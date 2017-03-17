using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord.Commands;

namespace MagicConchBot.Modules
{
    public class FunModule : ModuleBase
    {
        [Command("ghaussi")]
        public async Task GhaussiSpeak([Remainder]string input)
        {
            var words = input.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i] == "")
                    continue;
                
                words[i] = words[i].ToLower();

                if (words[i] == "third")
                {
                    words[i] = "turd";
                }
                else
                {
                    words[i] = words[i].Replace("th", "t");
                }

                words[i] = words[i].Replace("a", "u");

            }

            await ReplyAsync($"Ghaussi: {string.Join(" ", words)}");
        }

        private List<int> CapitalizedLetters(string s)
        {
            var capitalizedChars = new List<int>();

            for (int i = 0; i < s.Length; i++)
            {
                if (char.ToUpper(s[i]) == s[i])
                    capitalizedChars.Add(i);
            }

            return capitalizedChars;
        }

        private string Capitalize(string input, List<int> caps)
        {
            var charArray = input.ToCharArray();

            foreach (var index in caps)
            {
                charArray[index] = char.ToUpper(input[index]);
            }

            return new string(charArray);
        }
    }
}
