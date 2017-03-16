using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using MagicConchBot.Common.Types;
using MagicConchBot.Resources;

namespace MagicConchBot.Services
{
    public class GoogleApiService
    {
        private readonly YouTubeService _youtubeService;

        public GoogleApiService()
        {
            _youtubeService = new YouTubeService(new BaseClientService.Initializer
            {
                ApiKey = Configuration.Load().GoogleApiKey,
                ApplicationName = Configuration.Load().ApplicationName
            });
        }

        public async Task<string> GetFirstVideoByKeywordsAsync(string keywords)
        {
            var searchListRequest = _youtubeService.Search.List("snippet");
            searchListRequest.Q = keywords;
            searchListRequest.MaxResults = 1;
            searchListRequest.Type = "video";
            var videos = await searchListRequest.ExecuteAsync();
            var video = videos.Items.First();

            return $"https://www.youtube.com/watch?v={video.Id.VideoId}";
        }

        public async Task<Song> GetVideoInfoByIdAsync(string id)
        {
            var search = _youtubeService.Videos.List("snippet,contentDetails");
            search.Id = id;
            var video = (await search.ExecuteAsync()).Items.First();
            var regex = new Regex(@"PT((?<H>\d+)H)?(?<M>\d+)M(?<S>\d+)S");

            var match = regex.Match(video.ContentDetails.Duration);

            var h = match.Groups["H"].Value;
            var m = match.Groups["M"].Value;
            var s = match.Groups["S"].Value;

            var totalDuration = new TimeSpan(h == string.Empty ? 0 : Convert.ToInt32(h), 
                                             m == string.Empty ? 0 : Convert.ToInt32(m),
                                             s == string.Empty ? 0 : Convert.ToInt32(s));

            return new Song(video.Snippet.Title, totalDuration, $"https://www.youtube.com/watch?v={video.Id}", video.Snippet.Thumbnails.Default__.Url);
        }
    }
}
