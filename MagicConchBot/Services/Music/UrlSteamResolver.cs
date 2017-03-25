using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using MagicConchBot.Common.Interfaces;
using NLog;

namespace MagicConchBot.Services.Music
{
    public class UrlSteamResolver : ISongResolver
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static readonly string[] DirectPlayFormats = { "webm", "mp3", "avi", "wav", "mp4", "flac" };

        public async Task<string> GetSongStreamUrl(string url)
        {
            string streamUrl;

            if (DirectPlayFormats.Contains(url.Split('.').LastOrDefault()))
            {
                streamUrl = url;
            }
            else
            {
                Log.Debug("Retrieving url using youtube-dl");
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                streamUrl = await GetUrlFromYoutubeDlAsync(url).ConfigureAwait(false);

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
                Arguments = $"{url} -g -f bestaudio --audio-quality 0",
                RedirectStandardOutput = true,
                RedirectStandardError = false,
                CreateNoWindow = true,
                UseShellExecute = false
            };

            var p = Process.Start(youtubeDl);

            if (p != null)
                return await p.StandardOutput.ReadLineAsync();

            Log.Error("Unable to create youtube-dl process");
            return null;
        }
    }
}
