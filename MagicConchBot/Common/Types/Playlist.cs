using System.Collections.Generic;

namespace MagicConchBot.Common.Types
{
    public class Playlist
    {
        public const string DefaultName = "Default";

        public Playlist(string name = DefaultName)
        {
            Name = name;
            Songs = new List<string>();
        }

        public string Name { get; set; }
        
        public List<string> Songs { get; set; }
    }
}