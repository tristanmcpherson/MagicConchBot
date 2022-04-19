using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly SoundCloudClient _client;

        public SoundCloudInfoService()
        {
            _client = new SoundCloudClient(Configuration.SoundCloudClientId, Configuration.SoundCloudClientSecret);
        }

        public Regex Regex { get; } = new Regex(@"(?:https?:\/\/)?soundcloud\.com\/(?:[a-z0-9-]+\/?)+",
            RegexOptions.IgnoreCase);
        
        private static Song TrackToSong(Track track)
        {
            return new Song(
                track.title,
                new SongTime(Length: new TimeSpan(0, 0, 0, 0, track.duration)),
                track.artwork_url ?? track.user?.avatar_url,
                track.uri,
                track.uri,
                MusicType.SoundCloud);
        }

        public async Task<Song> GetSongInfoAsync(string url)
        {
            var track = await _client.Get<Track>(url);
            Console.WriteLine(track.stream_url);

            return TrackToSong(track);
        }

        public async Task<List<Song>> Search(string query)
        {
            var tracks = await _client.Search(query);

            return tracks.Select(TrackToSong).ToList();
        }
    }
}