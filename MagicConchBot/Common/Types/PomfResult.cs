using System.Collections.Generic;

namespace MagicConchBot.Common.Types
{
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
