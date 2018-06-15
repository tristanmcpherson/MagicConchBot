using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MagicConchBot.Common.Interfaces;
using MagicConchBot.Common.Types;
using MagicConchBot.Resources;

namespace MagicConchBot.Services
{
    public class SoundCloudInfoService : ISongInfoService
    {
        public SoundCloudInfoService()
        {
            var config = Configuration.Load();
            //var connector = new SoundCloudConnector();

           // Client = connector.UnauthorizedConnect(config.SoundCloudClientId, config.SoundCloudClientSecret);
        }

        public IUnauthorizedSoundCloudClient Client { get; set; }

        public Regex Regex { get; } = new Regex(@"(?:https?:\/\/)?soundcloud\.com\/(?:[a-z0-9-]+\/?)+",
            RegexOptions.IgnoreCase);
        
        public async Task<Song> GetSongInfoAsync(string url)
        {
            var track = await Client.Resolve.GetTrack(url);
            var artwork = track.Artwork.Url;

            return new Song(track.Title, track.Duration, url, artwork);
        }
    }
}