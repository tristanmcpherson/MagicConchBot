using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using MagicConchBot.Common.Interfaces;
using MagicConchBot.Common.Types;

namespace MagicConchBot.Services
{
    public class SongResolutionService : ISongResolutionService
    {
        private readonly IEnumerable<ISongInfoService> _musicInfoServices;

        public SongResolutionService(IEnumerable<ISongInfoService> musicInfoServices)
        {
            _musicInfoServices = musicInfoServices;
        }

        public async Task<Song> ResolveSong(string url, TimeSpan startTime)
        {
            Song song = null;
            foreach (var service in _musicInfoServices)
            {
                var match = service.Regex.Match(url);
                if (!match.Success)
                    continue;

                song = await service.GetSongInfoAsync(url);


                // url may contain time info but it is specified, overwrite
                if (startTime != TimeSpan.Zero)
                    song.StartTime = startTime;

                // song info found, stop info service search
                break;
            }

            if (song.Length == TimeSpan.Zero)
            {
                song.Length = await GetStreamLength(song.StreamUri);
            }

            // Song info not found from search or url
            return song ?? new Song(url, TimeSpan.Zero, url);
            // valid url but song information not found by any song info service
        }

        private static async Task<TimeSpan> GetStreamLength(string url)
        {
            var arguments = @$"-i ""{url}"" -show_entries format=duration -v quiet -of csv=""p=0""";
            var startInfo = new ProcessStartInfo
            {
                FileName = "ffprobe",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                UseShellExecute = false,
            };

            var process = new Process()
            {
                StartInfo = startInfo
            };

            process.Start();
            var seconds = await process.StandardOutput.ReadLineAsync();
            return TimeSpan.FromSeconds(double.Parse(seconds));
        }
    }
}
