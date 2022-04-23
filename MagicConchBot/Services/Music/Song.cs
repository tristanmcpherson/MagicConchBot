using System;
using CSharpFunctionalExtensions;
using Discord;
using MagicConchBot.Common.Interfaces;
using MagicConchBot.Resources;

namespace MagicConchBot.Common.Types
{
    public class SongTime
    {
        public TimeSpan Length { get; set; }
        public Maybe<TimeSpan> StartTime { get; set; }
        public Maybe<TimeSpan> CurrentTime { get; set; } = Maybe.None;
        public SongTime(TimeSpan? StartTime = null, TimeSpan? Length = null, TimeSpan? CurrentTime = null) {
            this.StartTime = Maybe.From(StartTime.GetValueOrDefault());
            this.CurrentTime = Maybe.From(CurrentTime.GetValueOrDefault());
            this.Length = Length ?? TimeSpan.Zero;
        }
    }

    public readonly record struct Song(string Name, SongTime Time, string ThumbnailUrl = "", string OriginalUrl = "", string Identifier = "", MusicType MusicType = MusicType.Other, string StreamUri = null)
    {
        public Song(string url) : this(url, new SongTime(), OriginalUrl: url, StreamUri: url) { }
    }

    public static class SongExtensions {
        public static string GetLengthPretty(this Song song)
        {
            return song.Time.Length >= TimeSpan.FromHours(1)
                ? song.Time.Length.ToString(@"hh\:mm\:ss")
                : (song.Time.Length == TimeSpan.Zero ? "??" : song.Time.Length.ToString(@"mm\:ss"));
        }

        public static string GetCurrentTimePretty(this Song song)
        {
            var currentTime = song.Time.CurrentTime.GetValueOrDefault(TimeSpan.Zero);
            return song.Time.Length >= TimeSpan.FromHours(1)
                ? currentTime.ToString(@"hh\:mm\:ss")
                : currentTime.ToString(@"mm\:ss");
        }

        public static Embed GetEmbed(this Song song, string title = "", bool embedThumbnail = true, bool showDuration = false, double volume = 1)
        {
            const char progressChar = '─';
            const string currentHead = ":white_circle:";
            const int progressLength = 34;

            var progressIndex = song.Time.Length == TimeSpan.Zero 
                ? 0
                : (int)(song.Time.CurrentTime.GetValueOrDefault().TotalSeconds / song.Time.Length.TotalSeconds * progressLength);

            var progressString = $"{new string(progressChar, progressIndex)}{currentHead}{new string(progressChar, progressLength - progressIndex)}";

            var length = song.GetLengthPretty();
            var currentTime = song.GetCurrentTimePretty();

            const char volumeChar = '─';
			const char volumeHead = '○';
			const int volumeLength = 4;
            var volumeIndex = (int)(volume * volumeLength);
            var volumeString = $"{new string(volumeChar, volumeIndex)}{volumeHead}{new string(volumeChar, volumeLength - volumeIndex)}";

            var timeString = $"\n{progressString}\n:loud_sound: {volumeString}　{currentTime.PadLeft(4, '⠀')} / {length.PadRight(20, '⠀')}\n\n";

            var builder = new EmbedBuilder {Color = Constants.MaterialBlue};
            builder.AddField(field =>
            {
                field.WithName(title == string.Empty ? song.Name == string.Empty ? "Default" : song.Name : title)
                    .WithValue($"**Url**:\n{song.OriginalUrl}\n\n**Duration**:\n" +
                               (showDuration ? timeString : $"{length}"));
            });

            if (song.ThumbnailUrl != string.Empty && embedThumbnail)
                builder.WithThumbnailUrl(song.ThumbnailUrl);

            return builder.Build();
        }

        public static string GetInfo(this Song song)
        {
            return $"{song.Name.Replace("*", "\\*")} **[{song.GetLengthPretty()}]**\n";
        }
    }
}