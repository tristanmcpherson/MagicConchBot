using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using MagicConchBot.Common.Interfaces;
using MagicConchBot.Common.Types;
using NLog;
using YoutubeExplode;
using YoutubeExplode.Videos;

namespace MagicConchBot.Services.Music
{
    public class DirectPlaySongResolver : ISongInfoService
    {
        public Regex Regex => new(@"(.+)\.(webm|mp3|avi|wav|mp4|flac)$");

        public Task<Song> GetSongInfoAsync(string url)
        {
            return Task.FromResult(new Song(url, new SongTime(), OriginalUrl: url));
        }

        // Output 
        public Task<Song> ResolveStreamUri(Song song)
        {
            return Task.FromResult(song with { StreamUri = song.Identifier });
        }
    }
}
