using System;
using Discord;
using MagicConchBot.Common.Interfaces;
using MagicConchBot.Resources;

namespace MagicConchBot.Common.Types
{
    public class Song
    {
        public Song(string name, TimeSpan length, string data, string thumbnailUrl = "", TimeSpan? startTime = null, MusicType musicType = MusicType.Other)
        {
            ThumbnailUrl = thumbnailUrl;
            Name = name;
            Length = length;
            Data = data;
            StartTime = startTime ?? TimeSpan.Zero;
            MusicType = musicType;
        }

        public string Name { get; }

        public string Data { get; }

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
            const int progressLength = 41;
            var progressIndex = (int)((CurrentTime.TotalSeconds / (Math.Abs(Length.TotalSeconds) < 1 ? CurrentTime.TotalSeconds: Length.TotalSeconds)) * progressLength);
            var progressString = $"{new string(progressChar, progressIndex)}{currentHead}{new string(progressChar, progressLength - progressIndex)}";

            var length = GetLengthPretty();
            var currentTime = GetCurrentTimePretty();

            const char volumeChar = '─';
			const char volumeHead = '○';
			const int volumeLength = 4;
            var volumeIndex = (int)(volume * volumeLength);
            var volumeString = $"{new string(volumeChar, volumeIndex)}{volumeHead}{new string(volumeChar, volumeLength - volumeIndex)}";

            var testString = $"\n{progressString}\n:loud_sound: {volumeString}　{currentTime.PadLeft(4, '⠀')} / {length.PadRight(20, '⠀')}\n\n";

            var timeString = $"{currentTime} / {length}";
            timeString = testString;

            var builder = new EmbedBuilder {Color = Constants.MaterialBlue};
            builder.AddField(field =>
            {
                field.WithName(title == string.Empty ? Name == string.Empty ? "Default" : Name : title)
                    .WithValue($"**Url**:\n{Data}\n\n**Duration**:\n" +
                               (showDuration ? timeString : $"{length}"));
            });

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