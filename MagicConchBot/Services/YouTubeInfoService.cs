namespace MagicConchBot.Services
{
    using System;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    using Discord.Commands;

    using MagicConchBot.Common.Interfaces;
    using MagicConchBot.Common.Types;

    public class YouTubeInfoService : IMusicInfoService
    {
        private readonly GoogleApiService googleApiService;

        public YouTubeInfoService(IDependencyMap map)
        {
            googleApiService = map.Get<GoogleApiService>();
        }

        public Regex Regex { get; } 
            = new Regex(@"(?:https?:\/\/)?(?:www\.)?(?:youtu\.be\/|youtube\.com(?:\/embed\/|\/v\/|\/watch\?v=))(?<VideoId>[\w-]{10,12})(?:[\&\?]?t=)?(?<Time>[\d]+)?s?(?<TimeAlt>(\d+h)?(\d+m)?(\d+s)?)?", RegexOptions.IgnoreCase);
        
        public async Task<Song> GetSongInfoAsync(string url)
        {
            var match = Regex.Match(url);

            if (!match.Success)
            {
                return null;
            }

            var videoId = match.Groups["VideoId"].Value;
            var song = await googleApiService.GetVideoInfoByIdAsync(videoId);
            if (match.Groups["Time"].Value != "")
            {
                song.StartTime = TimeSpan.FromSeconds(Convert.ToInt32(match.Groups["Time"].Value));
            }
            else if (match.Groups["TimeAlt"].Value != "")
            {
                song.StartTime = TimeSpan.Parse(match.Groups["TimeAlt"].Value);
            }

            return song;
        }
    }
}
