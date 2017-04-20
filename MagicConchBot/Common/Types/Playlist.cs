using System.Collections.Generic;

namespace MagicConchBot.Common.Types
{
    public class Playlist
    {
        public const string DefaultName = "Default";

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
    }
}