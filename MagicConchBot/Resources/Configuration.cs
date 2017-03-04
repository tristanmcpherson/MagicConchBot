using System;
using System.IO;
using Newtonsoft.Json;
using MagicConchBot.Common.Types;

namespace MagicConchBot.Resources
{
    public class Configuration
    {
        /// <summary> The location of your bot's dll, ignored by the json parser. </summary>
        [JsonIgnore]
        public static readonly string Appdir = AppContext.BaseDirectory;
        /// <summary> The location to this config file relative to the launch directory. </summary>
        [JsonIgnore]
        public const string JsonPath = "Configuration.json";

        /// <summary> Your bot's command prefix. Please don't pick `!`. </summary>
        public string Prefix { get; set; }
        /// <summary> Ids of users who will have owner access to the bot. </summary>
        public ulong[] Owners { get; set; }
        /// <summary> Your bot's login token. </summary>
        public string Token { get; set; }
        /// <summary> The api key for searching YouTube </summary>
        public string GoogleApiKey { get; set; }
        /// <summary> The name of this application for Google Api </summary>
        public string ApplicationName { get; set; }
        /// <summary> Local path to copy music to </summary>
        public string ServerMusicPath { get; set; }
        /// <summary> Base of the url ex. https://website.com/music/ </summary>
        public string ServerMusicUrlBase { get; set; }
        /// <summary>  </summary>
        public Playlist DefaultPlaylist { get; set; }

        public Configuration()
        {
            Prefix = "!";
            Owners = new ulong[] { 0 };
            Token = "";
            ApplicationName = "";
            GoogleApiKey = "";
            ServerMusicPath = "";
            ServerMusicUrlBase = "";
        }

        /// <summary> Save the configuration to the specified file location. </summary>
        public void Save(string dir = JsonPath)
        {
            var file = Path.Combine(Appdir, dir);
            File.WriteAllText(file, ToJson());
        }

        /// <summary> Load the configuration from the specified file location. </summary>
        public static Configuration Load(string dir = JsonPath)
        {
            var file = Path.Combine(Appdir, dir);
            return JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(file));
        }

        /// <summary> Convert the configuration to a json string. </summary>
        public string ToJson()
            => JsonConvert.SerializeObject(this, Formatting.Indented);
    }
}
