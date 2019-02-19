using System;
using System.Collections.Generic;
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

            // Song info not found from search or url
            return song ?? new Song(url);

            // valid url but song information not found by any song info service
        }
    }
}
