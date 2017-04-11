using System.Collections.Generic;
using Newtonsoft.Json;

namespace MagicConchBot.Common.Types
{
    public class PomfFile
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("fullurl")]
        public string FullUrl { get; set; }

        [JsonProperty("size")]
        public int Size { get; set; }
    }

    public class PomfResult
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("files")]
        public List<PomfFile> Files { get; set; }
    }
}