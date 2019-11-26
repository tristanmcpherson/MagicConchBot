using System;
using System.Threading;
using Discord;
using MagicConchBot.Common.Interfaces;
using MagicConchBot.Resources;

namespace MagicConchBot.Common.Types
{
    public class Song
    {
        public Song(string name, TimeSpan length, MusicType musicType, string data, string thumbnailUrl = "")
            : this(name, length, musicType, data, thumbnailUrl, TimeSpan.Zero)
        {
        }

        public Song(MusicType musicType, string data) : this(string.Empty, TimeSpan.Zero, musicType, data
        )
        {
        }

        public Song(string name, TimeSpan length, MusicType musicType, string data, string thumbnailUrl, TimeSpan startTime)
        {
            ThumbnailUrl = thumbnailUrl;
            Name = name;
            Length = length;
            Data = data;
            StartTime = startTime;
        }

        public string Name { get; }

        public string Data { get; }

        public MusicType MusicType { get; }

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
            const char progressChar = '─';
            const string currentHead = ":white_circle:";
            const int progressLength = 41;
            var progressIndex = (int)((CurrentTime.TotalSeconds / (Math.Abs(Length.TotalSeconds) < 1 ? CurrentTime.TotalSeconds: Length.TotalSeconds)) * progressLength);
            var progressString = $"{new string(progressChar, progressIndex)}{currentHead}{new string(progressChar, progressLength - progressIndex)}";

			// volume ───○
			const char volumeChar = '─';
			const char volumeHead = '○';
			const int volumeLength = 4;
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
                    .WithValue($"**Url**:\n{Data}\n\n**Duration**:\n" +
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