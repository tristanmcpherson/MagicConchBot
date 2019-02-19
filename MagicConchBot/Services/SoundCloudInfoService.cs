using System;
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
            var connector = new SoundCloudConnector(config.SoundCloudClientId, config.SoundCloudClientSecret);

            Client = connector.UnauthorizedConnect();
        }

        public IUnauthorizedSoundCloudClient Client { get; set; }

        public Regex Regex { get; } = new Regex(@"(?:https?:\/\/)?soundcloud\.com\/(?:[a-z0-9-]+\/?)+",
            RegexOptions.IgnoreCase);
        
        public async Task<Song> GetSongInfoAsync(string url)
        {
            var track = await Client.Resolve.GetTrack(url);
            return new Song(
                track.Title, 
                new TimeSpan(0,0,0,0,track.Duration), 
                url,
                track.ArtworkUrl);
        }
    }
}