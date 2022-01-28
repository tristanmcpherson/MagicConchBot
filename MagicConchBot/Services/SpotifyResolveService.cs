using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MagicConchBot.Common.Interfaces;
using MagicConchBot.Common.Types;
using MagicConchBot.Resources;
using SpotifyAPI.Web;

namespace MagicConchBot.Services
{
    public class SpotifyResolveService : ISongInfoService
    {
        public SpotifyResolveService()
        {
            var authenticator = new ClientCredentialsAuthenticator(
                Configuration.SpotifyClientId, 
                Configuration.SpotifyClientSecret
            );

            Client = new SpotifyClient(
                SpotifyClientConfig
                .CreateDefault()
                .WithAuthenticator(authenticator)
            );
        }

        public SpotifyClient Client { get; set; }

        //https://open.spotify.com/track/712uvW1Vezq8WpQi38v2L9?si=b963989009c74cd8
        public Regex Regex { get; } = new Regex(@"(?:https?:\/\/)?open.spotify\.com\/track/(?<trackId>.+)?\?.+",
            RegexOptions.IgnoreCase);

        public async Task<Song> GetSongInfoAsync(string url)
        {
            var trackId = Regex.Match(url).Groups["trackId"];

            var songUrl = $"https://open.spotify.com/track/{trackId}";

            var track = await Client.Tracks.Get(trackId.Value);
            return new Song(
                track.Name,
                new TimeSpan(0, 0, 0, 0, track.DurationMs),
                songUrl,
                track.Album.Images.FirstOrDefault()?.Url,
                null,
                MusicType.Spotify);
        }
    }
}