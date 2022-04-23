using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MagicConchBot.Common.Interfaces;
using MagicConchBot.Common.Types;

namespace MagicConchBot.Services.Music
{
    public class DirectPlaySongResolver : ISongInfoService
    {
        public Regex Regex => new(@"(.+)\.(webm|mp3|avi|wav|mp4|flac)$");

        public Task<Song> GetSongInfoAsync(string url)
        {
            return Task.FromResult(new Song(url));
        }

        // Output 
        public Task<Song> ResolveStreamUri(Song song)
        {
            return Task.FromResult(song with { StreamUri = song.OriginalUrl });
        }
    }
}
