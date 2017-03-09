namespace MagicConchBot.Services
{
    using System;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    using MagicConchBot.Common.Interfaces;
    using MagicConchBot.Common.Types;

    public class SoundCloudInfoService : IMusicInfoService
    {
        public Regex Regex { get; } = new Regex(@"(?:https?:\/\/)?soundcloud\.com\/(?:[a-z0-9-]+\/?)+", RegexOptions.IgnoreCase);

        public async Task<Song> GetSongInfoAsync(string url)
        {
            await Task.Delay(1);
            return new Song("Unknown", TimeSpan.Zero, url);
        }
    }
}
