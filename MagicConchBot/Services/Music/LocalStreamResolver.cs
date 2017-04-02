using System.IO;
using System.Threading.Tasks;
using MagicConchBot.Common.Interfaces;
using MagicConchBot.Resources;

namespace MagicConchBot.Services.Music
{
    public class LocalStreamResolver : ISongResolver
    {
        public async Task<string> GetSongStreamUrl(string uri)
        {
            if (File.Exists(uri))
                return new FileInfo(uri).FullName;

            var config = Configuration.Load();
            Directory.CreateDirectory(config.LocalMusicPath);
            var file = Path.Combine(config.LocalMusicPath, uri);
            return File.Exists(file) ? file : null;
        }
    }
}
