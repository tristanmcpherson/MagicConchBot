using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using MagicConchBot.Common.Interfaces;
using MagicConchBot.Common.Types;
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

        public YoutubeInfoService(YoutubeClient youtubeClient)
        {
            _youtubeClient = youtubeClient;
        }

        public async Task<string> GetFirstVideoByKeywordsAsync(string keywords)
        {
            Log.Info("Looking up song by keywords");

            var videos = _youtubeClient.Search.GetVideosAsync(keywords);
            return (await videos.FirstAsync()).Url;
        }

        public async Task<Song> GetVideoInfoByIdAsync(string id, TimeSpan? startTime = null)
        {
            Log.Info("Looking up song by id");
            try
            {
                var video = await _youtubeClient.Videos.GetAsync(VideoId.Parse(id));

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

        static async Task<string> GetAudioStreamUrl(string videoUrl)
        {
            Log.Info($"Getting audio stream URL for: {videoUrl}");
            
            // Try multiple yt-dlp paths in case one doesn't work
            string[] ytDlpPaths = new[] { "yt-dlp", "/usr/local/bin/yt-dlp", "/usr/bin/yt-dlp" };
            string arguments = $"-f bestaudio --get-url --no-warnings -q \"{videoUrl}\"";
            
            // Add cookies if available
            string cookiesPath = Environment.GetEnvironmentVariable("YOUTUBE_COOKIE_FILE");
            if (!string.IsNullOrEmpty(cookiesPath) && File.Exists(cookiesPath))
            {
                Log.Info($"Using cookies file: {cookiesPath}");
                arguments += $" --cookies \"{cookiesPath}\"";
            }

            int maxRetries = 3;
            for (int retry = 0; retry < maxRetries; retry++)
            {
                foreach (string ytDlpPath in ytDlpPaths)
                {
                    try
                    {
                        // Create the process start info
                        ProcessStartInfo startInfo = new ProcessStartInfo
                        {
                            FileName = ytDlpPath,
                            Arguments = arguments,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };

                        Log.Debug($"Executing: {ytDlpPath} {arguments}");

                        // Start the process
                        using Process process = new Process { StartInfo = startInfo };
                        process.Start();

                        // Capture the output (URL)
                        string output = await process.StandardOutput.ReadToEndAsync();
                        string error = await process.StandardError.ReadToEndAsync();
                        await process.WaitForExitAsync();

                        if (process.ExitCode != 0)
                        {
                            Log.Error($"yt-dlp failed with exit code {process.ExitCode}. Error: {error}");
                            continue; // Try next path
                        }

                        string url = output.Trim();
                        if (string.IsNullOrEmpty(url))
                        {
                            Log.Error("yt-dlp returned empty URL despite exit code 0");
                            continue; // Try next path
                        }

                        Log.Info($"Got audio URL: {url.Substring(0, Math.Min(url.Length, 50))}...");
                        return url;
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, $"Error executing yt-dlp at path {ytDlpPath}: {ex.Message}");
                        // Continue to next path
                    }
                }
                
                if (retry < maxRetries - 1)
                {
                    Log.Info($"Retrying yt-dlp (attempt {retry + 2}/{maxRetries})...");
                    await Task.Delay(1000); // Wait before retrying
                }
            }
            
            // If we get here, all attempts failed
            Log.Error($"All yt-dlp attempts failed for URL: {videoUrl}");
            return string.Empty;
        }

        public async Task<Song> ResolveStreamUri(Song song)
        {
            Log.Info($"Resolving stream URI for song: {song.Name} (ID: {song.Identifier})");
            
            try
            {
                string streamUrl = await GetAudioStreamUrl(song.OriginalUrl);
                
                if (string.IsNullOrEmpty(streamUrl))
                {
                    Log.Error($"Failed to get stream URL for {song.OriginalUrl}");
                    return song;
                }
                
                return song with { StreamUri = streamUrl };
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error resolving stream URI: {ex.Message}");
                return song;
            }
        }
    }
}