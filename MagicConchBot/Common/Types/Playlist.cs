using System.Collections.Generic;

namespace MagicConchBot.Common.Types
{
    public class Playlist
    {
        public string Name { get; set; }
        public List<string> Songs { get; set; }

        public Playlist(string name = "Default")
        {
            Name = name;
            Songs = new List<string>();
        }
    }
}
