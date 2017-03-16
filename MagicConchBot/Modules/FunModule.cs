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

                var capitalized = CapitalizedLetters(words[i]);

                if (Regex.IsMatch(words[i], "th", RegexOptions.IgnoreCase))
                {
                    words[i] = words[i].Replace("th", "t");
                    words[i] = words[i].Replace("Th", "T");
                }
            }

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

        private string Capitalize(string s)
        {
            return char.ToUpper(s[0]) + s.Substring(1);
        }
    }
}
