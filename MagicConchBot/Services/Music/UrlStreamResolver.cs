﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using MagicConchBot.Common.Interfaces;
using MagicConchBot.Common.Types;
using NLog;
using YoutubeExplode;
using YoutubeExplode.Videos;

namespace MagicConchBot.Services.Music
{
    public class UrlStreamResolver : ISongResolver
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static readonly string[] DirectPlayFormats = { "webm", "mp3", "avi", "wav", "mp4", "flac" };

        private static readonly YoutubeClient client = new YoutubeClient();



        public async Task<string> GetSongStreamUrl(Song song)
        {
            string streamUrl;
            var musicType = song.MusicType;
            var data = song.Data;

            if (musicType == MusicType.Spotify)
            {
                // search and use as if youtube
                var results = client.Search.GetResultsAsync(song.Name);
                var res = await results.FirstAsync();
                data = res.Url;
                musicType = MusicType.YouTube;
            } 
            
            if (DirectPlayFormats.Contains(data.Split('.').LastOrDefault()))
            {
                streamUrl = data;
			} else if (musicType == MusicType.YouTube) {
                var manifest = await client.Videos.Streams.GetManifestAsync(VideoId.Parse(data));
                var streams = manifest.GetAudioOnlyStreams();
                var streamInfo = streams.OrderBy(s => s.Bitrate).FirstOrDefault();

				streamUrl = streamInfo.Url;
			} else
            {
                Log.Debug("Retrieving url using youtube-dl");
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                streamUrl = await GetUrlFromYoutubeDlAsync(data).ConfigureAwait(false);

                stopwatch.Stop();

                if (streamUrl == null)
                    Log.Error("Failed to get url from youtube-dl. Possible update needed.");
                else
                    Log.Debug("Data source found: " + stopwatch.Elapsed);
            }

            return streamUrl;
        }

        private static async Task<string> GetUrlFromYoutubeDlAsync(string url)
        {
            //-reconnect 1 -reconnect_streamed 1 -reconnect_delay_max 5
            var youtubeDl = new ProcessStartInfo
            {
                FileName = "youtube-dl",
                Arguments = $"-g -f bestaudio --audio-quality 0 {url}",
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
