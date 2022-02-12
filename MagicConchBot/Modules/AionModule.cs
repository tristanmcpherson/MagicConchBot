using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;

namespace MagicConchBot.Services.Games
{
    public class AionModule : ModuleBase
    {
        private static readonly Dictionary<string, Timer> Timers = new();
        private readonly Regex ChannelRegex = new(@"💀(ㅣ|\|)(?<hours>\d+)(-\d+)?h(ㅣ|\|)(?<name>\w+(-\w+)?)💀?");
     
        [Command("dead")]
        public async Task Dead(TimeSpan? offset = null)
        {
            var match = ChannelRegex.Match(Context.Channel.Name);

            if (match.Success)
            {
                var name = match.Groups["name"].Value;

                var hours = Convert.ToInt32(match.Groups["hours"].Value);
                var defaultInterval = TimeSpan.FromHours(hours - 0.5);
                var hoursMillis = (defaultInterval - (offset ?? TimeSpan.Zero)).TotalMilliseconds;

                if (Timers.TryGetValue(name, out var timer))
                {
                    if (timer.Interval != hoursMillis)
                    {
                        timer.Interval = hoursMillis;
                    }

                    timer.Stop();
                    timer.Start();
                }
                else
                {

                    var newTimer = new Timer
                    {
                        AutoReset = false,
                        Interval = hoursMillis
                    };

                    newTimer.Elapsed += async (sender, args) =>
                    {
                        await Context.Channel.SendMessageAsync($"@here 30 minutes before {name} window");

                    };

                    newTimer.Start();
                    Timers.Add(name, newTimer);
                }
                await Context.Channel?.SendMessageAsync($"{name} has been killed. Timer set");
            }
        }
    }
}
