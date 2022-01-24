using System.Collections.Generic;
using System.Linq;

namespace MagicConchBot.Common.Types
{
    public class Playlist
    {
        public const string DefaultName = "default";

        public Playlist()
        {
            Name = DefaultName;
            Songs = new List<string>();
        }

        public Playlist(string name)
        {
            Name = name;
            Songs = new List<string>();
        }

        public string Name { get; set; }

        public List<string> Songs { get; set; }
    }

    public class GuildSettings
    {
        public List<Playlist> Playlists { get; set; }

        public GuildSettings()
        {
            Playlists = new List<Playlist>();
        }

        public Playlist GetPlaylistOrCreate(string name = Playlist.DefaultName)
        {
            if (Playlists == null)
            {
                Playlists = new List<Playlist>();
            }

            var playlist = Playlists.FirstOrDefault(p => p.Name == name);
            if (playlist == null)
            {
                playlist = new Playlist(name);
                Playlists.Add(playlist);
            }

            return playlist;
        }

        public Playlist GetPlaylistOrNull(string name = Playlist.DefaultName)
        {
            if (Playlists == null)
            {
                Playlists = new List<Playlist>();
            }

            return Playlists.FirstOrDefault(p => p.Name == name);
        }
    }
}