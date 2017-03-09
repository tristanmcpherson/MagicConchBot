namespace MagicConchBot.Common.Types
{
    using System;
    using System.Threading;

    using Discord;

    using MagicConchBot.Resources;

    public class Song
    {
        public Song(string name, TimeSpan length, string url, string thumbnailUrl = "") : this(name, length, url, thumbnailUrl, TimeSpan.Zero)
        {
        }

        public Song(string url) : this("", TimeSpan.Zero, url)
        {
        }

        public Song(string name, TimeSpan length, string url, string thumbnailUrl, TimeSpan startTime)
        {
            ThumbnailUrl = thumbnailUrl;
            Name = name;
            Length = length;
            Url = url;
            this.StartTime = startTime;
        }

        public bool IsPaused { get; set; }

        public string Name { get; }

        public string Url { get; }

        public string StreamUrl { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan CurrentTime { get; set; }

        public CancellationTokenSource TokenSource { get; set; }

        private TimeSpan Length { get; } // Length in seconds

        private string ThumbnailUrl { get; }

        private string TotalTimePretty =>
            Length > new TimeSpan(0, 59, 59)
                ? Length.ToString(@"hh\:mm\:ss")
                : (Length == TimeSpan.Zero ? "??" : Length.ToString(@"mm\:ss"));

        private string CurrentTimePretty
            => Length > new TimeSpan(0, 59, 59) ? CurrentTime.ToString(@"hh\:mm\:ss") : CurrentTime.ToString(@"mm\:ss");

        public Embed GetEmbed(string title = "", bool embedThumbnail = true, bool showDuration = false)
        {
            var builder = new EmbedBuilder { Color = Constants.MaterialBlue };
            builder.AddField(x =>
            {
                x.WithName(title == "" ? Name == "" ? "Default" :  Name : title)
                    .WithValue($"**Url**:\n{Url}\n\n**Duration**:\n" + (showDuration ? $"{CurrentTimePretty} / {TotalTimePretty}" : $"{TotalTimePretty}"));
            });

            if (ThumbnailUrl != "" && embedThumbnail)
            {
                builder.WithThumbnailUrl(ThumbnailUrl);
            }

            return builder.Build();
        }
    }
}
