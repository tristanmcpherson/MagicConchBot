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
            Client = SoundCloudConnector.AuthorizedConnect(Configuration.SoundCloudClientId, Configuration.SoundCloudClientSecret);
        }

        public IAuthorizedSoundCloudClient Client { get; set; }

        public Regex Regex { get; } = new Regex(@"(?:https?:\/\/)?soundcloud\.com\/(?:[a-z0-9-]+\/?)+",
            RegexOptions.IgnoreCase);
        
        public async Task<Song> GetSongInfoAsync(string url)
        {
            var track = await Client.Resolve.GetTrack(url);
            return new Song(
                track.title, 
                new TimeSpan(0,0,0,0, track.duration),
                url,
                track.artwork_url ?? track.user?.avatar_url,
                null,
                MusicType.SoundCloud);
        }
    }
}