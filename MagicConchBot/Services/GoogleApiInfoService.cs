﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using MagicConchBot.Common.Interfaces;
using MagicConchBot.Common.Types;
using MagicConchBot.Resources;

namespace MagicConchBot.Services
{
    public class GoogleApiInfoService : ISongInfoService
    {
        private readonly YouTubeService _youtubeService;

        public GoogleApiInfoService()
        {
            var config = Configuration.Load();
            _youtubeService = new YouTubeService(new BaseClientService.Initializer
            {
                ApiKey = config.GoogleApiKey,
                ApplicationName = config.ApplicationName
            });
        }

        public async Task<string> GetFirstVideoByKeywordsAsync(string keywords)
        {
            var searchListRequest = _youtubeService.Search.List("snippet");
            searchListRequest.Q = keywords;
            searchListRequest.MaxResults = 1;
            searchListRequest.Type = "video";
            var videos = await searchListRequest.ExecuteAsync();
            var video = videos.Items.FirstOrDefault();

            return video == null ? null : $"https://www.youtube.com/watch?v={video.Id.VideoId}";
        }

        public async Task<Song> GetVideoInfoByIdAsync(string id)
        {
            var search = _youtubeService.Videos.List("snippet,contentDetails");
            search.Id = id;
            var video = (await search.ExecuteAsync()).Items.FirstOrDefault();
            if (video == null)
                return null;

            return ParseVideo(video);
        }

        public async Task<List<Song>> GetVideoInfoByIdAsync(List<string> ids)
        {
            var search = _youtubeService.Videos.List("snippet,contentDetails");
            search.Id = string.Join(",", ids.Where(id => id != ""));
            var videos = (await search.ExecuteAsync()).Items;

            return videos.Select(ParseVideo).ToList();
        }

        private static Song ParseVideo(Video video)
        {
            var regex = new Regex(@"PT((?<H>\d+)H)?((?<M>\d+)M)?((?<S>\d+)S)?");

            var match = regex.Match(video.ContentDetails.Duration);

            var h = match.Groups["H"].Value;
            var m = match.Groups["M"].Value;
            var s = match.Groups["S"].Value;

            var totalDuration = new TimeSpan(h == string.Empty ? 0 : Convert.ToInt32(h),
                m == string.Empty ? 0 : Convert.ToInt32(m),
                s == string.Empty ? 0 : Convert.ToInt32(s));

            return new Song(video.Snippet.Title, totalDuration, $"https://www.youtube.com/watch?v={video.Id}",
                video.Snippet.Thumbnails.Default__.Url);
        }

        public async Task<List<Song>> GetSongsByPlaylistAsync(string id)
        {
            // https://www.youtube.com/watch?v=TzU1PjYr0DA&list=LLHtXpGu4WCdfqE3TZ9rg2xg

            var search = _youtubeService.PlaylistItems.List("snippet,contentDetails");
            search.MaxResults = 50;
            search.PlaylistId = id;

            var songs = new List<Song>();

            PlaylistItemListResponse playlist;
            var count = 0;

            do
            {
                playlist = await search.ExecuteAsync();

                if (playlist.Items.Count == 0)
                    break;

                var videoIds = playlist.Items.Select(i => i.ContentDetails?.VideoId ?? "").ToList();

                songs.AddRange(await GetVideoInfoByIdAsync(videoIds));

                count += playlist.Items.Count;
                search.Id = null;
                search.PageToken = playlist.NextPageToken;
            } while (playlist.PageInfo.TotalResults > count);

            return songs;
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