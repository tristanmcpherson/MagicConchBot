using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using MagicConchBot.Common.Interfaces;
using MagicConchBot.Common.Types;
using MagicConchBot.Resources;

namespace MagicConchBot.Services.Music
{
    public class LocalStreamResolver : ISongInfoService
    {
        // TODO: maybe remove
        public Regex Regex => new("adfgadfgadfgadfgadfg");

        public Task<Maybe<Song>> GetSongInfoAsync(string url)
        {
            throw new System.NotImplementedException();
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task<string> GetSongStreamUrl(Song song)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            if (File.Exists(song.Identifier))
            {
                return new FileInfo(song.Identifier).FullName;
            }

            if (string.IsNullOrEmpty(Configuration.LocalMusicPath))
                return null;
            Directory.CreateDirectory(Configuration.LocalMusicPath);
            var file = Path.Combine(Configuration.LocalMusicPath, song.Identifier);

            return File.Exists(file) ? file : null;
        }
    }
}
