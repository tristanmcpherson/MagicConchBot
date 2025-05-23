using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MagicConchBot.Common.Interfaces;
using MagicConchBot.Common.Types;

namespace MagicConchBot.Services.Music
{
    public class DirectPlaySongResolver : ISongInfoService
    {
        public Regex Regex => new(@"(.+)\.(webm|mp3|avi|wav|mp4|flac)");

        public Task<Song> GetSongInfoAsync(string url)
        {
            return Task.FromResult(new Song(url.Split("/").Last(), new SongTime(), OriginalUrl: url, StreamUri: url));
        }

        // Output 
        public Task<Song> ResolveStreamUri(Song song)
        {
            // TODO: Add song info from container?
            // parse title from format (global) tags using ffprobe
            // ex: ./ffprobe -i "E:\Music\love is not dying (deluxe)\02 we're fucked, it's fine.mp3" -hide_banner -loglevel fatal -show_error -show_entries format_tags=title -of csv="p=0"
            return Task.FromResult(song with { StreamUri = song.OriginalUrl });
        }
    }
}
