using Discord;
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

    [FirestoreData]
    public class FirestoreTimerEvent
    {
        [FirestoreProperty("timerId")]
        public string TimerId { get; set; }

        [FirestoreProperty("startTime")]
        public Timestamp StartTime { get; set; }

        [FirestoreProperty("interval")]
        public string Interval { get; set; }

        [FirestoreProperty("endTime")]
        public Timestamp EndTime { get; set; }

        [FirestoreProperty("textChannelId")]
        public ulong TextChannelId { get; set; }

        [FirestoreProperty("channelOffset")]
        public string ChannelOffset { get; set;}

        [FirestoreProperty("guildId")]
        public ulong GuildId { get; set; }
    }

    public partial class AionModule : InteractionModuleBase<ConchInteractionCommandContext>
    {
        private static readonly Dictionary<(ulong, string), TimerEvent> Timers = new();
        private static readonly Regex ChannelRegex = GenerateChannelRegex();
        private static readonly TimeSpan DefaultOffset = TimeSpan.FromMinutes(30);

        [GeneratedRegex("💀(ㅣ|\\|)(?<hours>\\d+)h?-?(?<hoursEnd>\\d+)?h?(?<minutesEnd>\\d+)?m?(ㅣ|\\|)(?<name>\\w+(-\\w+)?)💀?(?<reminderTimeHours>\\d+)?h?(?<reminderTimeMinutes>\\d+)?m?")]
        private static partial Regex GenerateChannelRegex();
        public DiscordSocketClient Client { get; }
        public FirestoreDb Firestore { get; }

        public AionModule(FirestoreDb firestore, DiscordSocketClient client)
        {
            Firestore = firestore;
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
                if (timerEvent.TimerId == null) continue;

                // calculate how much time is left
                var timeLeft = TimeSpan.Parse(timerEvent.Interval) - (DateTime.UtcNow - timerEvent.StartTime.ToDateTime());
                if (timeLeft.TotalMilliseconds <= 0)
                {
                    // time has passed, alert is outdated
                    // log this event to the console
                }

                // create and start the timer
                var newTimer = new Timer
                {
                    AutoReset = false,
                    Interval = timeLeft.TotalMilliseconds > 0 ? timeLeft.TotalMilliseconds : 1
                };

                newTimer.Elapsed += async (obj, e) => await TimerElapsed(timerEvent);

                newTimer.Start();

                // add the timer to the in-memory dictionary
                Timers[(timerEvent.GuildId, timerEvent.TimerId)] = new TimerEvent(newTimer, timerEvent.StartTime.ToDateTime());
            }
        }


        private async Task GetOrSetTimer(IMessageChannel textChannel, string timerId, double hoursMillis, DateTime hoursEndTime, TimeSpan channelOffset, bool emitMessage = true)
        {

            var guildId = (textChannel as IGuildChannel).GuildId;
            var timersCollection = Firestore.Collection("timers");


            if (Timers.TryGetValue((guildId, timerId), out var timer))
            {
                timer.Timer.Stop();  // stop the existing timer

                // remove timer from Firestore
                await timersCollection.Document(timerId).DeleteAsync();
            }

            // Creating a new timer
            var newTimer = new Timer
            {
                AutoReset = false,
                Interval = hoursMillis
            };

            var textChannelId = textChannel.Id;
            // save new timer to Firestore
            var newTimerEvent = new FirestoreTimerEvent
            {
                TimerId = timerId,
                StartTime = Timestamp.FromDateTime(DateTime.UtcNow),
                EndTime = Timestamp.FromDateTime(hoursEndTime),
                Interval = TimeSpan.FromMilliseconds(hoursMillis).ToString(),
                TextChannelId = textChannelId,
                ChannelOffset = channelOffset.ToString(),
                GuildId = (textChannel as IGuildChannel).GuildId
            };


            newTimer.Elapsed += async (obj, e) => await TimerElapsed(newTimerEvent); 

            newTimer.Start();


            var docRef = await timersCollection.Document(newTimerEvent.TimerId).SetAsync(newTimerEvent);
            

            if (emitMessage) await RespondAsync($"{timerId} has been killed. Timer set");
        }

        private async Task TimerElapsed(FirestoreTimerEvent timerEvent)
        {
            var startTime = timerEvent.StartTime.ToDateTime() + TimeSpan.Parse(timerEvent.Interval) + TimeSpan.FromMinutes(30);
            await (Client.GetChannel(timerEvent.TextChannelId) as ITextChannel).SendMessageAsync(
                $"@here 30 minutes before {timerEvent.TimerId} window. " +
                $"The window will start at {startTime.ToShortEST()} EST. " +
                $"The window will end at {timerEvent.EndTime.ToDateTime().ToShortEST()} EST.");

            // remove timer from Firestore when it expires
            var timersCollection = Firestore.Collection("timers");
            await timersCollection.Document(timerEvent.TimerId).DeleteAsync();
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
                var timeSinceMessage = DateTime.UtcNow - Context.Interaction.CreatedAt;

                var channelOffsetHoursMatch = match.Groups["reminderTimeHours"];
                var channelOffsetMinutesMatch = match.Groups["reminderTimeMinutes"];

                var channelOffsetHours = channelOffsetHoursMatch.Success ? Convert.ToInt32(channelOffsetHoursMatch.Value) : DefaultOffset.Hours;
                var channelOffsetMinutes = channelOffsetMinutesMatch.Success ? Convert.ToInt32(channelOffsetMinutesMatch.Value) : DefaultOffset.Minutes;

                var channelOffset = new TimeSpan(channelOffsetHours, channelOffsetMinutes, 0);

                var defaultInterval = TimeSpan.FromHours(hours) - channelOffset;
                var hoursMillis = (defaultInterval - timeSinceMessage - (offset ?? TimeSpan.Zero)).TotalMilliseconds;

                if (hoursMillis < 0)
                {
                    await RespondAsync("Offset is bigger than timer window - 30m. Bot should prob be updated to just say the current window.");
                    return;
                }

                var hoursEnd = hoursEndMatch.Success ? Convert.ToInt32(hoursEndMatch.Value) : hours;
                var minutesEnd = minutesEndMatch.Success ? TimeSpan.FromMinutes(Convert.ToInt32(minutesEndMatch.Value)) : TimeSpan.Zero;
                var hoursEndTimeSpan = TimeSpan.FromHours(hoursEnd) + minutesEnd - timeSinceMessage - (offset ?? TimeSpan.Zero);

                var windowTime = DateTime.UtcNow + hoursEndTimeSpan;

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

            // get the timers collection
            var timersCollection = Firestore.Collection("timers");

            // create a query that filters by the guild ID
            var query = timersCollection.WhereEqualTo("guildId", guildId);

            // run the query
            var snapshot = await query.GetSnapshotAsync();

            if (snapshot.Count == 0)
            {
                await Context.Channel.SendMessageAsync("No timers set.");
                await DeferAsync();
                return;
            }

            var text = string.Join('\n', snapshot.Documents.Select(doc =>
            {
                var timerEvent = doc.ConvertTo<FirestoreTimerEvent>();
                var timeLeft = TimeSpan.Parse(timerEvent.Interval) - (DateTime.UtcNow - timerEvent.StartTime.ToDateTime());
                var formatted = FormatTimeSpan(timeLeft);

                var channelOffset = string.IsNullOrEmpty(timerEvent.ChannelOffset) ? DefaultOffset : TimeSpan.Parse(timerEvent.ChannelOffset);

                var windowStart = timerEvent.StartTime.ToDateTime() + TimeSpan.Parse(timerEvent.Interval) + channelOffset;
                var windowEnd = timerEvent.EndTime.ToDateTime();
                var windowText = $"The window start at: {windowStart.ToShortEST()} EST and ends at: {windowEnd.ToShortEST()} EST.";

                return $"{timerEvent.TimerId}: {formatted} - {windowText}";
            }));

            await RespondAsync(text);
        }

        private static string FormatTimeSpan(TimeSpan timeSpan)
        {
            var components = new List<(int value, string suffix)>
            {
                ((int) timeSpan.TotalDays, "d"),
                (timeSpan.Hours, "h"),
                (timeSpan.Minutes, "m"),
            };

            var nonZeroComponents = components.Where(i => i.value != 0).ToList();

            return nonZeroComponents.Count == 0 ? "less than 1m" : string.Join(" ", nonZeroComponents.Select((a) => a.value + a.suffix));
        }
    }

    public static class DateTimeExtensions
    {
        public static string ToShortEST(this DateTime date)
        {
            var estZone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
            var estTime = TimeZoneInfo.ConvertTimeFromUtc(date, estZone);
            return estTime.ToString("h:mm tt");
        }
    }
}
