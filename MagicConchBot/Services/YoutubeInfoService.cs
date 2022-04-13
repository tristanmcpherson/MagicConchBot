using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
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

        public async Task<Song> GetVideoInfoByIdAsync(string id, TimeSpan? startTime = null)
        {
            Log.Info("Looking up song.");
            try
            {
                var video = await _youtubeClient.Videos.GetAsync(VideoId.Parse(id)).ConfigureAwait(false);

                Log.Info("Song info found.");
                return ParseVideo(video, startTime);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to fetch video info for ${id}");
                return await Task.FromException<Song>(ex);
            }
        }

        private static Song ParseVideo(Video video, TimeSpan? startTime = null)
        {
            return new Song(video.Title, new SongTime(StartTime: startTime, Length: video.Duration.Value), video.Thumbnails[0].Url, video.Url, video.Id, MusicType.YouTube);
        }

        private static Song ParseVideo(PlaylistVideo video)
        {
            return new Song(video.Title, new SongTime(Length: video.Duration.Value), video.Thumbnails[0].Url, video.Url, video.Id, MusicType.YouTube);
        }

        public async Task<List<Song>> GetSongsByPlaylistAsync(string id)
        {

            var videos = _youtubeClient.Playlists.GetVideosAsync(PlaylistId.Parse(id));
            return await videos.Select(ParseVideo).ToListAsync();
        }

        public Regex Regex { get; } = new Regex(@"(?:https?:\/\/)?(?:www\.)?(?:youtu\.be\/|youtube\.com(?:\/embed\/|\/v\/|\/watch\?v=|\/playlist))(?<VideoId>[\w-]{11})?([&\?]list=)?(?<PlaylistId>[\w-]+)?(?:[\&\?]?t=)?(?<Time>[\d]+)?s?(?<TimeAlt>(\d+h)?(\d+m)?(\d+s)?)?", RegexOptions.IgnoreCase);
        public async Task<Song> GetSongInfoAsync(string url)
        {
            var match = Regex.Match(url);
            var startTime = TimeSpan.Zero;

            if (match.Groups["Time"].Value != string.Empty)
                startTime = TimeSpan.FromSeconds(Convert.ToInt32(match.Groups["Time"].Value));
            else if (match.Groups["TimeAlt"].Value != string.Empty)
                startTime = TimeSpan.Parse(match.Groups["TimeAlt"].Value);

            var videoId = match.Groups["VideoId"].Value;
            return await GetVideoInfoByIdAsync(videoId, startTime);
        }

        public async Task<Song> ResolveStreamUri(Song song)
        {
            var manifest = await _youtubeClient.Videos.Streams.GetManifestAsync(VideoId.Parse(song.Identifier));
            var streams = manifest.GetAudioOnlyStreams();
            var streamInfo = streams.OrderByDescending(s => s.Bitrate).FirstOrDefault();

            return song with { StreamUri = streamInfo.Url };
        }
    }
}