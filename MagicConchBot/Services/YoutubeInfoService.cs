using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MagicConchBot.Common.Interfaces;
using MagicConchBot.Common.Types;
using MagicConchBot.Resources;
using NLog;
using YoutubeExplode;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos;

namespace MagicConchBot.Services
{
    public class YoutubeInfoService : ISongInfoService
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        YoutubeClient _youtubeClient;

        public YoutubeInfoService()
        {
            _youtubeClient = new YoutubeClient();
        }

        public async Task<string> GetFirstVideoByKeywordsAsync(string keywords)
        {
            Log.Info("Looking up song.");

            var videos = _youtubeClient.Search.GetVideosAsync(keywords);
            return (await videos.FirstAsync()).Url;
        }

        public async Task<Song> GetVideoInfoByIdAsync(string id)
        {
            Log.Info("Looking up song.");
            try {
                var video = await _youtubeClient.Videos.GetAsync(VideoId.Parse(id)).ConfigureAwait(false);

                Log.Info("Song info found.");
                return ParseVideo(video);
            } catch (Exception ex) {
                Log.Error($"Failed to fetch video info for ${id}");
                return await Task.FromException<Song>(ex);
            }
        }

        private static Song ParseVideo(Video video)
        {
            return new Song(video.Title, (TimeSpan)video.Duration, video.Url, video.Thumbnails[0].Url, null, MusicType.YouTube);
        }

        private static Song ParseVideo(PlaylistVideo video) {
            return new Song(video.Title, (TimeSpan)video.Duration, video.Url, video.Thumbnails[0].Url, null, MusicType.YouTube);
        }

        public async Task<List<Song>> GetSongsByPlaylistAsync(string id) {
            
            var videos = _youtubeClient.Playlists.GetVideosAsync(PlaylistId.Parse(id));
            return await videos.Select(ParseVideo).ToListAsync();
        }

        public Regex Regex { get; } = new Regex(@"(?:https?:\/\/)?(?:www\.)?(?:youtu\.be\/|youtube\.com(?:\/embed\/|\/v\/|\/watch\?v=|\/playlist))(?<VideoId>[\w-]{10,12})?([&\?]list=)?(?<PlaylistId>[\w-]{22,34})?(?:[\&\?]?t=)?(?<Time>[\d]+)?s?(?<TimeAlt>(\d+h)?(\d+m)?(\d+s)?)?", RegexOptions.IgnoreCase);
        public async Task<Song> GetSongInfoAsync(string url)
        {
            var match = Regex.Match(url);

            if (!match.Success)
                return null;

            var videoId = match.Groups["VideoId"].Value;
            var song = await GetVideoInfoByIdAsync(videoId);
            if (match.Groups["Time"].Value != string.Empty)
                song.StartTime = TimeSpan.FromSeconds(Convert.ToInt32(match.Groups["Time"].Value));
            else if (match.Groups["TimeAlt"].Value != string.Empty)
                song.StartTime = TimeSpan.Parse(match.Groups["TimeAlt"].Value);

            return song;
        }
    }
}