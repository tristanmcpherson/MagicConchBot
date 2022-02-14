﻿using Discord;
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
    record TimerEvent(Timer Timer, DateTime StartTime);

    public class AionModule : ModuleBase
    {
        private static readonly Dictionary<string, TimerEvent> Timers = new();
        private readonly Regex ChannelRegex = new(@"💀(ㅣ|\|)(?<hours>\d+)(-\d+)?h(ㅣ|\|)(?<name>\w+(-\w+)?)💀?");

        public DiscordSocketClient Client { get; }

        public AionModule(DiscordSocketClient client)
        {
            Client = client;
            Client.Ready += Client_Ready;
        }

        private async Task Client_Ready()
        {
            await ResumeTimers(941776898298609725);
        }

        public async Task ResumeTimers(ulong guildId)
        {
            var guild = Client.Guilds.First(guild => guild.Id == guildId);
            var matches = new List<Match>();
            var channels = new List<ITextChannel>();

            foreach (var channel in guild.TextChannels)
            {
                var match = ChannelRegex.Match(channel.Name);
                if (match.Success)
                {
                    matches.Add(match);
                    channels.Add(channel);
                }
            }

            var channelMatch = matches.Zip(channels);

            foreach (var kv in channelMatch)
            {
                var (match, channel) = kv;

                var hours = Convert.ToInt32(match.Groups["hours"].Value);
                var defaultInterval = TimeSpan.FromHours(hours - 0.5);

                var nowMinusInterval = DateTime.Now - defaultInterval;

                var messages = await channel.GetMessagesAsync().TakeWhile(m => m.All(m => m.Timestamp > nowMinusInterval)).ToListAsync();
                var allMessages = messages.SelectMany(a => a);
                var deadMessage = allMessages.FirstOrDefault(m => m.Content == "!dead");

                if (deadMessage != null)
                {
                    var messageAge = deadMessage.Timestamp - DateTime.Now;

                    await GetOrSetTimer(deadMessage.Channel, match.Groups["name"].Value, (defaultInterval - messageAge).TotalMilliseconds);
                }
            }

            Console.WriteLine("debug");
        }

        private async Task GetOrSetTimer(IMessageChannel textChannel, string name, double hoursMillis)
        {
            if (Timers.TryGetValue(name, out var tuple))
            {
                var (timer, _) = tuple;
                if (timer.Interval != hoursMillis)
                {
                    timer.Interval = hoursMillis;
                }

                timer.Stop();
                timer.Start();
                Timers[name] = new(timer, DateTime.Now);
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
                    await textChannel?.SendMessageAsync($"@here 30 minutes before {name} window");
                };

                newTimer.Start();
                Timers.Add(name, new(newTimer, DateTime.Now));
            }

            await textChannel?.SendMessageAsync($"{name} has been killed. Timer set");
        }

     
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

                await GetOrSetTimer(Context.Channel, name, hoursMillis);
            }
        }
    
        [Command("checktimers")]
        public async Task CheckTimers()
        {
            var text = string.Join('\n', Timers.Select((kv) => kv.Key + ": " + (DateTime.Now - kv.Value.StartTime)));
            await Context.Channel.SendMessageAsync(text);
        }
    }
}
