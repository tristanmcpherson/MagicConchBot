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
        public Regex Regex => new(".(webm|mp3|avi|wav|mp4|flac)$");

        public Task<Maybe<Song>> GetSongInfoAsync(string url)
        {
            return Task.FromResult(Maybe.From(new Song(url, new SongTime())));
        }

        // Output 
        public Task<Song> ResolveStreamUri(Song song)
        {
            return Task.FromResult(song with { StreamUri = song.Identifier });
        }
    }
}
