using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MagicConchBot.Modules;
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

        public CommandService CommandService { get; }
        public DiscordSocketClient Client { get; }
        public IServiceProvider Services { get; }

        public AionModule(CommandService commandService, IServiceProvider services, DiscordSocketClient client)
        {
            CommandService = commandService;
            Services = services;
            Client = client;

            Client.Ready += Client_Ready;
        }

        private async Task Client_Ready()
        {
            await ResumeTimers(1101360116235767888);
        }

        public async Task ResumeTimers(ulong guildId)
        {
            var guild = Client.Guilds.FirstOrDefault(guild => guild.Id == guildId);
            if (guild == null) { return; }
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

                var messages = await channel.GetMessagesAsync().TakeWhile(m => m.Any(m => m.Timestamp > nowMinusInterval)).ToListAsync();

                var allMessages = messages.SelectMany(a => a).Where(m => m.Timestamp > nowMinusInterval);
                var deadMessage = allMessages.FirstOrDefault(m => m.Content.StartsWith("!dead"));

                if (deadMessage == null)
                {
                    continue;
                }

                // re-execute command
                var context = new ConchCommandContext(Client, deadMessage as IUserMessage, Services);
                await CommandService.ExecuteAsync(context, 1, Services, MultiMatchHandling.Best);
            }
        }

        private async Task GetOrSetTimer(IMessageChannel textChannel, string name, double hoursMillis, bool emitMessage = true)
        {
            if (Timers.TryGetValue(name, out var timerEvent))
            {
                var (timer, _) = timerEvent;
                if (timer.Interval != hoursMillis)
                {
                    timer.Interval = hoursMillis;
                }

                timer.Stop();
                timer.Start();
                Timers[name] = new(timer, Context.Message.Timestamp.LocalDateTime);
            }
            else
            {
                var newTimer = new Timer
                {
                    AutoReset = false,
                    Interval = hoursMillis
                };


                var textChannelId = textChannel.Id;

                newTimer.Elapsed += async (sender, args) =>
                {
                    await (Client.GetChannel(textChannelId) as ITextChannel).SendMessageAsync($"@here 30 minutes before {name} window");
                    Timers[name].Timer.Dispose();
                    Timers.Remove(name);
                };

                newTimer.Start();
                Timers.Add(name, new(newTimer, DateTime.Now));
            }

            if (emitMessage) await textChannel?.SendMessageAsync($"{name} has been killed. Timer set");
        }

     
        [Command("dead")]
        public async Task Dead(TimeSpan? offset = null)
        {
            var match = ChannelRegex.Match(Context.Channel.Name);

            if (match.Success)
            {
                var name = match.Groups["name"].Value;

                var hours = Convert.ToInt32(match.Groups["hours"].Value);
                var timeSinceMessage = DateTime.Now - Context.Message.Timestamp;
                var defaultInterval = TimeSpan.FromHours(hours - 0.5);
                var hoursMillis = (defaultInterval - timeSinceMessage - (offset ?? TimeSpan.Zero)).TotalMilliseconds;

                var emitMessage = timeSinceMessage < TimeSpan.FromMinutes(1);

                await GetOrSetTimer(Context.Channel, name, hoursMillis, emitMessage);
            }
        }
    
        [Command("checktimers")]
        public async Task CheckTimers()
        {
            if (Timers.Count == 0) { 
                await Context.Channel.SendMessageAsync("No timers set.");
                return;
            }

            var text = string.Join('\n', Timers.Select((kv) => {
                var time = TimeSpan.FromMilliseconds(kv.Value.Timer.Interval) - (DateTime.Now - kv.Value.StartTime);
                var formatted = FormatTimeSpan(time);
                return kv.Key + ": " + formatted;
            }));
            await Context.Channel.SendMessageAsync(text);
        }

        private static string FormatTimeSpan(TimeSpan timeSpan)
        {
            var components = new List<(int, string)>
            {
                ((int) timeSpan.TotalDays, "d"),
                (timeSpan.Hours, "h"),
                (timeSpan.Minutes, "m")
            };

            components.RemoveAll(i => i.Item1 == 0);

            return components.Select((a) => a.Item1 + a.Item2).Aggregate((a, b) => a + " " + b);
        }
    }
}
