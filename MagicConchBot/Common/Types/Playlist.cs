namespace MagicConchBot.Common.Types
{
    using System.Collections.Generic;

    public class Playlist
    {
        public Playlist(string name = "Default")
        {
            Name = name;
            Songs = new List<string>();
        }

        public string Name { get; set; }

        public List<string> Songs { get; set; }
    }
}
