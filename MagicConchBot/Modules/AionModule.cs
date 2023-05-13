using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Google.Cloud.Firestore;
using MagicConchBot.Modules;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;

namespace MagicConchBot.Services.Games
{
    record TimerEvent(Timer Timer, DateTime StartTime);

    public class FirestoreTimerEvent
    {
        public string TimerId { get; set; }
        public string Name { get; set; }
        public DateTime StartTime { get; set; }
        public TimeSpan Interval { get; set; }
        public TimeSpan EndTime { get; set; }
        public ulong TextChannelId { get; set; }
        public ulong GuildId { get; set; } 
    }

    public class AionModule : InteractionModuleBase<ConchInteractionCommandContext>
    {
        private static readonly Dictionary<(ulong, string), TimerEvent> Timers = new();
        private readonly Regex ChannelRegex = new(@"💀(ㅣ|\|)(?<hours>\d+)-?(?<hoursEnd>\d+)?h(?<minutesEnd>\d+)?m?(ㅣ|\|)(?<name>\w+(-\w+)?)💀?");

        public DiscordSocketClient Client { get; }
        public FirestoreDb Firestore { get; }
        public IServiceProvider Services { get; }

        public AionModule(FirestoreDb firestore, IServiceProvider services, DiscordSocketClient client)
        {
            Firestore = firestore;
            Services = services;
            Client = client;

            Client.Ready += Client_Ready;
        }

        private async Task Client_Ready()
        {
            await LoadTimers();
        }

        private async Task LoadTimers()
        {
            // get the timers collection
            var timersCollection = Firestore.Collection("timers");

            // get all documents
            var snapshot = await timersCollection.GetSnapshotAsync();

            foreach (var document in snapshot.Documents)
            {
                var timerEvent = document.ConvertTo<FirestoreTimerEvent>();

                // calculate how much time is left
                var timeLeft = timerEvent.Interval - (DateTime.Now - timerEvent.StartTime);
                if (timeLeft.TotalMilliseconds <= 0) continue;

                // create and start the timer
                var newTimer = new Timer
                {
                    AutoReset = false,
                    Interval = timeLeft.TotalMilliseconds
                };

                newTimer.Elapsed += async (sender, args) =>
                {
                    await (Client.GetChannel(timerEvent.TextChannelId) as ITextChannel).SendMessageAsync($"@here 30 minutes before {timerEvent.Name} window. The window will end at {timerEvent.StartTime + timerEvent.Interval}");
                    await timersCollection.Document(timerEvent.TimerId).DeleteAsync();
                };

                newTimer.Start();

                // add the timer to the in-memory dictionary
                Timers[(timerEvent.GuildId, timerEvent.TimerId)] = new TimerEvent(newTimer, timerEvent.StartTime);
            }
        }


        private async Task GetOrSetTimer(IMessageChannel textChannel, string name, double hoursMillis, DateTime hoursEndTime, bool emitMessage = true)
        {
            var guildId = (textChannel as IGuildChannel).GuildId;
            if (Timers.TryGetValue((guildId, name), out var timer))
            {
                timer.Timer.Stop();  // stop the existing timer

                // remove timer from Firestore
                var timersCollection = Firestore.Collection("timers");
                await timersCollection.Document(name).DeleteAsync();
            }
            else
            {
                // Creating a new timer
                var newTimer = new Timer
                {
                    AutoReset = false,
                    Interval = hoursMillis
                };

                var textChannelId = textChannel.Id;

                newTimer.Elapsed += async (sender, args) =>
                {
                    await (Client.GetChannel(textChannelId) as ITextChannel).SendMessageAsync($"@here 30 minutes before {name} window. The window will end at {hoursEndTime.ToShortTimeString()} EST");

                    // remove timer from Firestore when it expires
                    var timersCollection = Firestore.Collection("timers");
                    await timersCollection.Document(name).DeleteAsync();
                };

                newTimer.Start();

                // save new timer to Firestore
                var newTimerEvent = new FirestoreTimerEvent
                {
                    TimerId = name,
                    Name = name,
                    StartTime = DateTime.Now,
                    Interval = TimeSpan.FromMilliseconds(hoursMillis),
                    TextChannelId = textChannelId,
                    GuildId = (textChannel as IGuildChannel).GuildId
                };

                var timersCollection = Firestore.Collection("timers");
                await timersCollection.Document(newTimerEvent.TimerId).SetAsync(newTimerEvent);
            }

            if (emitMessage) await RespondAsync($"{name} has been killed. Timer set");
        }

     
        [SlashCommand("dead", "Reports the channel's npc as dead")]
        public async Task Dead(TimeSpan? offset = null)
        {
            var match = ChannelRegex.Match(Context.Channel.Name);

            if (match.Success)
            {
                var name = match.Groups["name"].Value;

                var hours = Convert.ToInt32(match.Groups["hours"].Value);
                var hoursEndMatch = match.Groups["hoursEnd"];
                var minutesEndMatch = match.Groups["minutesEnd"];
                var timeSinceMessage = DateTime.Now - Context.Interaction.CreatedAt;
                Console.WriteLine(timeSinceMessage);
                var defaultInterval = TimeSpan.FromHours(hours - 0.5);
                var hoursMillis = (defaultInterval - timeSinceMessage - (offset ?? TimeSpan.Zero)).TotalMilliseconds;

                var hoursEnd = hoursEndMatch.Success ? Convert.ToInt32(hoursEndMatch.Value) : hours;
                var minutesEnd = minutesEndMatch.Success ? TimeSpan.FromMinutes(Convert.ToInt32(minutesEndMatch.Value)) : TimeSpan.Zero;
                var hoursEndTimeSpan = (TimeSpan.FromHours(hoursEnd) + minutesEnd - timeSinceMessage) - (offset ?? TimeSpan.Zero);
                var windowTimeSpan = hoursEndTimeSpan - TimeSpan.FromHours(hours);

                var windowTime = DateTime.Now + windowTimeSpan;

                var emitMessage = timeSinceMessage < TimeSpan.FromMinutes(1);

                await GetOrSetTimer(Context.Channel, name, hoursMillis, windowTime, emitMessage);
            } 
            else
            {
                await RespondAsync("Channel name invalid. Please fix the name and try again. The format is: " + ChannelRegex);
            }
        }

        [SlashCommand("checktimers", "Checks the timers")]
        public async Task CheckTimers()
        {
            var guildId = (Context.Channel as IGuildChannel).GuildId;
            var guildTimers = Timers.Where(kv => kv.Key.Item1 == guildId).ToList();

            if (!guildTimers.Any())
            {
                await Context.Channel.SendMessageAsync("No timers set.");
                await RespondAsync();
                return;
            }

            var text = string.Join('\n', guildTimers.Select((kv) => {
                var time = TimeSpan.FromMilliseconds(kv.Value.Timer.Interval) - (DateTime.Now - kv.Value.StartTime);
                var formatted = FormatTimeSpan(time);
                return kv.Key.Item2 + ": " + formatted;
            }));

            await RespondAsync(text);
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
