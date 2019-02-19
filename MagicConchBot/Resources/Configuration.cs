// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Configuration.cs" company="None">
//   None
// </copyright>
// <summary>
//   The configuration file.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.IO;
using MagicConchBot.Common.Types;
using Newtonsoft.Json;

namespace MagicConchBot.Resources
{
    /// <summary>
    ///     The configuration file.
    /// </summary>
    public class Configuration
    {
        /// <summary> The location to this config file relative to the launch directory. </summary>
        [JsonIgnore] public const string JsonPath = "Configuration.json";

        /// <summary> The location of your bot's DLL, ignored by the JSON parser. </summary>
        [JsonIgnore] public static readonly string Appdir = AppContext.BaseDirectory;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Configuration" /> class.
        /// </summary>
        public Configuration()
        {
            Owners = new ulong[] {0};
            Blacklist = new ulong[] {0};
            Token = string.Empty;
            GoogleApiKey = string.Empty;
            ApplicationName = string.Empty;
            ServerMusicPath = string.Empty;
            ServerMusicUrlBase = string.Empty;
            DefaultPlaylist = new Playlist();
            WrongChannelError = string.Empty;
            RequiredRole = string.Empty;
            OwnerGuildId = 0;
            BotControlChannel = string.Empty;
            LocalMusicPath = string.Empty;
        }

        /// <summary> Gets or sets the bot's command prefix. Please don't pick `!`. </summary>
        public string Prefix { get; set; } = "!";

        /// <summary> Gets or sets the ids of users who will have owner access to the bot. </summary>
        public ulong[] Owners { get; set; }

        public ulong[] Blacklist { get; set; }

        /// <summary> Gets or sets the bot's login token. </summary>
        public string Token { get; set; }

        /// <summary> Gets or sets the API key for searching YouTube. </summary>
        public string GoogleApiKey { get; set; }

		/// <summary> Gets or sets the name of this application for the Google API. </summary>
		public string ApplicationName { get; set; } = "MagicConchBot";

		/// <summary> Gets or sets the destination path to copy music to. </summary>
		public string ServerMusicPath { get; set; } = "";

		/// <summary> Gets or sets the base of the url ex. https://website.com/music/. </summary>
		public string ServerMusicUrlBase { get; set; } = "http://magicconchbot.com/";

        /// <summary> Gets or sets the default playlist. </summary>
        public Playlist DefaultPlaylist { get; set; }

        /// <summary> Gets or sets the error message shown if the user requests a command in the wrong channel. </summary>
        public string WrongChannelError { get; set; }

		/// <summary> Gets or sets the name of the role required to use bot music commands. </summary>
		public string RequiredRole { get; set; } = "ConchControl";
        
        /// <summary>
        /// Gets or sets the id of the bot owner's guild.
        /// </summary>
        public ulong OwnerGuildId { get; set; }

        /// <summary>
        /// Gets or sets the channel name in which bot commands can be 
        /// </summary>
        public string BotControlChannel { get; set; }

        /// <summary>
        /// Gets or sets the Client Secret for the SoundCloud API.
        /// </summary>
        public string SoundCloudClientSecret { get; set; }

        /// <summary>
        /// Gets or sets the Client Id for the SoundCloud API.
        /// </summary>
        public string SoundCloudClientId { get; set; }

        /// <summary>
        /// Gets or sets the local path for music to be played from.
        /// </summary>
        public string LocalMusicPath { get; set; }

        /// <summary>
        ///     Load the configuration from the specified file location.
        /// </summary>
        /// <param name="dir">
        ///     The optional path to save the JSON to.
        /// </param>
        /// <returns>
        ///     The <see cref="Configuration" /> instance loaded.
        /// </returns>
        public static Configuration Load(string dir = JsonPath)
        {
            var file = Path.Combine(Appdir, dir);
            var config = JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(file));

			var token = Environment.GetEnvironmentVariable(Constants.DiscordTokenVariable);
			var apiKey = Environment.GetEnvironmentVariable(Constants.GoogleApiKeyVariable);
			var owner = Environment.GetEnvironmentVariable(Constants.OwnerVariable);

			if (token != null) {
				config.Token = token;
			}
			if (apiKey != null) {
				config.GoogleApiKey = apiKey;
			}
			if (owner != null) {
				config.Owners = new ulong[] { ulong.Parse(owner) };
			}

			return config;
        }

        /// <summary>
        ///     Convert the configuration to a JSON string.
        /// </summary>
        /// <returns>
        ///     The serialized <see cref="string" /> JSON of this instance.
        /// </returns>
        public string ToJson()
            => JsonConvert.SerializeObject(this, Formatting.Indented);

        /// <summary>
        ///     Save the configuration to the specified file location.
        /// </summary>
        /// <param name="dir">
        ///     The optional path to save the JSON to.
        /// </param>
        public void Save(string dir = JsonPath)
        {
            var file = Path.Combine(Appdir, dir);
            File.WriteAllText(file, ToJson());
        }
    }
}