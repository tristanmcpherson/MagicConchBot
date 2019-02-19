using System;
using System.Threading;
using Discord;
using MagicConchBot.Resources;

namespace MagicConchBot.Common.Types
{
    public class Song
    {
        public Song(string name, TimeSpan length, string url, string thumbnailUrl = "")
            : this(name, length, url, thumbnailUrl, TimeSpan.Zero)
        {
        }

        public Song(string url) : this(string.Empty, TimeSpan.Zero, url)
        {
        }

        public Song(string name, TimeSpan length, string url, string thumbnailUrl, TimeSpan startTime)
        {
            ThumbnailUrl = thumbnailUrl;
            Name = name;
            Length = length;
            Url = url;
            StartTime = startTime;
        }

        public string Name { get; }

        public string Url { get; }

        public string StreamUri { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan CurrentTime { get; set; }

        public CancellationTokenSource TokenSource { get; set; }

        public CancellationToken Token => TokenSource.Token;

        public TimeSpan Length { get; } // Length in seconds

        private string ThumbnailUrl { get; }

        public string LengthPretty
            => Length >= TimeSpan.FromHours(1)
                ? Length.ToString(@"hh\:mm\:ss")
                : (Length == TimeSpan.Zero ? "??" : Length.ToString(@"mm\:ss"));

        public string CurrentTimePretty
            => Length >= TimeSpan.FromHours(1)
                ? CurrentTime.ToString(@"hh\:mm\:ss")
                : CurrentTime.ToString(@"mm\:ss");

        public Embed GetEmbed(string title = "", bool embedThumbnail = true, bool showDuration = false, double volume = 1)
        {
            var progressChar = '─';
            var currentHead = ":white_circle:";
            var progressLength = 41;
            var progressIndex = (int)((CurrentTime.TotalSeconds / (Math.Abs(Length.TotalSeconds) < 1 ? CurrentTime.TotalSeconds: Length.TotalSeconds)) * progressLength);
            var progressString = $"{new string(progressChar, progressIndex)}{currentHead}{new string(progressChar, progressLength - progressIndex)}";

            // volume ───○
            var volumeChar = '─';
            var volumeHead = '○';
            var volumeLength = 4;
            var volumeIndex = (int)(volume * volumeLength);
            var volumeString = $"{new string(volumeChar, volumeIndex)}{volumeHead}{new string(volumeChar, volumeLength - volumeIndex)}";

            var four = "⠀⠀　⠀⠀　⠀⠀　⠀⠀　";
            var playThings = "◄◄⠀▐▐ ⠀►►⠀⠀　";

            var testString = $@"
{progressString}
:loud_sound: {volumeString}　{CurrentTimePretty.PadLeft(4, '⠀')} / {LengthPretty.PadRight(20, '⠀')}⠀⠀　⠀⠀　⠀　⠀⠀⠀　

";

            var timeString = $"{CurrentTimePretty} / {LengthPretty}";
            timeString = testString;

            var builder = new EmbedBuilder {Color = Constants.MaterialBlue};
            builder.AddField(x =>
            {
                x.WithName(title == string.Empty ? Name == string.Empty ? "Default" : Name : title)
                    .WithValue($"**Url**:\n{Url}\n\n**Duration**:\n" +
                               (showDuration ? timeString : $"{LengthPretty}"));
            });

            if (ThumbnailUrl != string.Empty && embedThumbnail)
                builder.WithThumbnailUrl(ThumbnailUrl);

            return builder.Build();
        }

        public string GetInfo()
        {
            return $"{Name} - **[{LengthPretty}]**\n";
        }
    }
}