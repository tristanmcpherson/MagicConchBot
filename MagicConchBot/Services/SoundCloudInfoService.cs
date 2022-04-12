using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
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
            Console.WriteLine(track.stream_url);
            return new Song(
                track.title,
                new SongTime(Length: new TimeSpan(0, 0, 0, 0, track.duration)),
                track.artwork_url ?? track.user?.avatar_url,
                url,
                url,
                MusicType.SoundCloud);
        }
    }
}