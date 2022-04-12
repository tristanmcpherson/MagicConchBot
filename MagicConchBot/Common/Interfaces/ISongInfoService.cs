using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using MagicConchBot.Common.Types;

namespace MagicConchBot.Common.Interfaces
{
    public interface ISongInfoService
    {
        Regex Regex { get; }

        Task<Song> GetSongInfoAsync(string url);

        /// <summary>
        /// Specifiy specific resolver to resolve Songs to a streamable Uri, defaults to youtube-dl
        /// </summary>
        /// <param name="song"></param>
        /// <returns></returns>
        async Task<Song> ResolveStreamUri(Song song)
        {
            var youtubeDlInfo = await DefaultSongInfo.GetUrlFromYoutubeDlAsync(song.Identifier);
            return youtubeDlInfo
                .Map(urls => urls.First())
                .Map(url => song with { StreamUri = url})
                .GetValueOrThrow($"Could not resolve song uri from youtube-dl for {song}");
        }
    }

    public static class DefaultSongInfo
    {
        public static async Task<Maybe<List<string>>> GetUrlFromYoutubeDlAsync(string url)
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
                return Maybe.None;
            }

            var output = new List<string>();

            while (!p.StandardOutput.EndOfStream)
            {
                output.Add(await p.StandardOutput.ReadLineAsync());
            }

            return Maybe.From(output);
        }
    }
} 