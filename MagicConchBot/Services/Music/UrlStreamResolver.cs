﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using MagicConchBot.Common.Interfaces;
using NLog;
using YoutubeExtractor;

namespace MagicConchBot.Services.Music
{
    public class UrlStreamResolver : ISongResolver
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static readonly string[] DirectPlayFormats = { "webm", "mp3", "avi", "wav", "mp4", "flac" };

        public async Task<string> GetSongStreamUrl(string uri)
        {
            string streamUrl;

            if (DirectPlayFormats.Contains(uri.Split('.').LastOrDefault()))
            {
                streamUrl = uri;
            }
            else if (uri.Contains("youtube"))
            {
                var video = DownloadUrlResolver.GetDownloadUrls(uri)
                    .OrderByDescending(info => info.AudioBitrate)
                    .ThenBy(info => info.Resolution)
                    .FirstOrDefault();
                streamUrl = video?.DownloadUrl;
            }
            else
            {
                Log.Debug("Retrieving url using youtube-dl");
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                streamUrl = await GetUrlFromYoutubeDlAsync(uri).ConfigureAwait(false);

                stopwatch.Stop();

                if (streamUrl == null)
                    Log.Error("Failed to get url from youtube-dl. Possible update needed.");
                else
                    Log.Debug("Url source found: " + stopwatch.Elapsed);
            }

            return streamUrl;
        }

        private static async Task<string> GetUrlFromYoutubeDlAsync(string url)
        {
            var youtubeDl = new ProcessStartInfo
            {
                FileName = "youtube-dl",
                Arguments = $"-g --audio-quality 0 {url}",
                RedirectStandardOutput = true,
                RedirectStandardError = false,
                CreateNoWindow = true,
                UseShellExecute = false
            };

            var p = Process.Start(youtubeDl);
            if (p == null)
            {
                Log.Error("Unable to create youtube-dl process");
                return null;
            }

            var output = new List<string>();

            while (!p.StandardOutput.EndOfStream)
            {
                output.Add(await p.StandardOutput.ReadLineAsync());
            }

            return output.LastOrDefault();
        }
    }
}
