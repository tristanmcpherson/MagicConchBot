namespace MagicConchBot.Common.Types
{
    using System.Collections.Generic;

    public class PomfFile
    {
        public string name { get; set; }
        public string url { get; set; }
        public string fullurl { get; set; }
        public int size { get; set; }
    }

    public class PomfResult
    {
        public bool success { get; set; }
        public List<PomfFile> files { get; set; }
    }
}
