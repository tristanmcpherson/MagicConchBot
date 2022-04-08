using System;
using Discord;
using MagicConchBot.Common.Interfaces;
using MagicConchBot.Resources;

namespace MagicConchBot.Common.Types
{
    public record SongTime(TimeSpan StartTime, TimeSpan CurrenTime, TimeSpan Length);

    public class Song
    {
        public Song(string name, TimeSpan length, string url, string thumbnailUrl = "", TimeSpan? startTime = null, MusicType musicType = MusicType.Other, string identifier = null)
        {
            ThumbnailUrl = thumbnailUrl;
            Name = name;
            Length = length;
            Identifier = url;
            Url = identifier ?? url;
            StartTime = startTime ?? TimeSpan.Zero;
            MusicType = musicType;
        }

        public string Name { get; }

        public string Identifier { get; set; }

        public string Url { get; }

        public string StreamUri { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan CurrentTime { get; set; }

        public TimeSpan Length { get; }

        public MusicType MusicType { get; set; }

        private string ThumbnailUrl { get; }

        public string GetLengthPretty()
        {
            return Length >= TimeSpan.FromHours(1)
                ? Length.ToString(@"hh\:mm\:ss")
                : (Length == TimeSpan.Zero ? "??" : Length.ToString(@"mm\:ss"));
        }

        public string GetCurrentTimePretty()
        {
            return Length >= TimeSpan.FromHours(1)
                ? CurrentTime.ToString(@"hh\:mm\:ss")
                : CurrentTime.ToString(@"mm\:ss");
        }

        public Embed GetEmbed(string title = "", bool embedThumbnail = true, bool showDuration = false, double volume = 1)
        {
            const char progressChar = '─';
            const string currentHead = ":white_circle:";
            const int progressLength = 34;

            var progressIndex = Length == TimeSpan.Zero 
                ? 0
                : (int)(CurrentTime.TotalSeconds / Length.TotalSeconds * progressLength);

            var progressString = $"{new string(progressChar, progressIndex)}{currentHead}{new string(progressChar, progressLength - progressIndex)}";

            var length = GetLengthPretty();
            var currentTime = GetCurrentTimePretty();

            const char volumeChar = '─';
			const char volumeHead = '○';
			const int volumeLength = 4;
            var volumeIndex = (int)(volume * volumeLength);
            var volumeString = $"{new string(volumeChar, volumeIndex)}{volumeHead}{new string(volumeChar, volumeLength - volumeIndex)}";

            var timeString = $"\n{progressString}\n:loud_sound: {volumeString}　{currentTime.PadLeft(4, '⠀')} / {length.PadRight(20, '⠀')}\n\n";

            var builder = new EmbedBuilder {Color = Constants.MaterialBlue};
            builder.AddField((Action<EmbedFieldBuilder>)(field =>
            {
                field.WithName(title == string.Empty ? Name == string.Empty ? "Default" : Name : title)
                    .WithValue($"**Url**:\n{this.Identifier}\n\n**Duration**:\n" +
                               (showDuration ? timeString : $"{length}"));
            }));

            if (ThumbnailUrl != string.Empty && embedThumbnail)
                builder.WithThumbnailUrl(ThumbnailUrl);

            return builder.Build();
        }

        public string GetInfo()
        {
            return $"{Name} - **[{GetLengthPretty()}]**\n";
        }
    }
}