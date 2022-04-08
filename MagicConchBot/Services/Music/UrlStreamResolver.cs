using System.Collections.Generic;
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

        private static readonly YoutubeClient client = new();

        // Output 
        public async Task<string> GetSongStreamUrl(Song song)
        {
            string streamUrl;
            var musicType = song.MusicType;
            
            if (musicType == MusicType.Spotify)
            {
                // search and use as if youtube
                var results = client.Search.GetResultsAsync(song.Identifier);
                var res = await results.FirstAsync();
                song.Identifier = res.Url;
                song.StreamUri = res.Url;
                musicType = MusicType.YouTube;
            } 
            
            if (DirectPlayFormats.Contains(song.Identifier.Split('.').LastOrDefault()))
            {
                streamUrl = song.Identifier;
			} 
            else if (musicType == MusicType.YouTube) 
            {
                var manifest = await client.Videos.Streams.GetManifestAsync(VideoId.Parse(song.Identifier));
                var streams = manifest.GetAudioOnlyStreams();
                var streamInfo = streams.OrderBy(s => s.Bitrate).FirstOrDefault();

				streamUrl = streamInfo.Url;
			} 
            else
            {
                Log.Debug("Retrieving url using youtube-dl");
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                var resolvedSongs = await GetUrlFromYoutubeDlAsync(song.Identifier).ConfigureAwait(false);

                streamUrl = resolvedSongs.FirstOrDefault();

                stopwatch.Stop();

                if (streamUrl == null)
                    Log.Error("Failed to get url from youtube-dl. Possible update needed.");
                else
                    Log.Debug("Data source found: " + stopwatch.Elapsed);
            }

            return streamUrl;
        }

        private static async Task<List<string>> GetUrlFromYoutubeDlAsync(string url)
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

            return output;
        }
    }
}
