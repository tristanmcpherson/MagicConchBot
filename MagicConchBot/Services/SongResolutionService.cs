using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using MagicConchBot.Common.Interfaces;
using MagicConchBot.Helpers;
using MagicConchBot.Common.Types;
using CSharpFunctionalExtensions;

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
            var song = (await _musicInfoServices.SelectFirst(async service =>
            {
                if (service.Regex.IsMatch(url))
                {
                    return await service.GetSongInfoAsync(url);
                }
                else
                {
                    return Maybe.None;
                }
            })).GetValueOrDefault(new Song(url, new SongTime(TimeSpan.Zero), url));

            // url may contain time info but it is specified, overwrite
            if (startTime != TimeSpan.Zero)
            {
                song.Time.StartTime = startTime;
            }

            // this will not work
            //if (song.Time.Length == TimeSpan.Zero)
            //{
            //    song.Time.Length = await GetStreamLength(song.StreamUri);
            //}

            return song;
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
