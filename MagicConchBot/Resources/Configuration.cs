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
using System.Linq;
using MagicConchBot.Common.Types;
using Newtonsoft.Json;

namespace MagicConchBot.Resources
{
    /// <summary>
    ///     The configuration file.
    /// </summary>
    public static class Configuration
    {
        /// <summary> The location of your bot's DLL, ignored by the JSON parser. </summary>
        [JsonIgnore] public static readonly string Appdir = AppContext.BaseDirectory;

        /// <summary> Gets or sets the bot's command prefix. Please don't pick `!`. </summary>
        public static string Prefix { get; set; } = "!";

        /// <summary> Gets or sets the ids of users who will have owner access to the bot. </summary>
        public static ulong[] Owners { get; set; } = 
            Environment.GetEnvironmentVariable(Constants.OwnerVariable)?
                .Split(',')
                .Select(x => ulong.Parse(x.Trim()))
                .ToArray() ?? new ulong[0];

        public static ulong[] Blacklist { get; set; } =
            Environment.GetEnvironmentVariable(Constants.BlacklistVariable)?
                .Split(',')
                .Select(x => ulong.Parse(x.Trim()))
                .ToArray() ?? new ulong[0];

        /// <summary> Gets or sets the bot's login token. </summary>
        public static string Token { get; set; } = 
            Environment.GetEnvironmentVariable(Constants.DiscordTokenVariable);

        /// <summary> Gets or sets the API key for searching YouTube. </summary>
        public static string GoogleApiKey { get; set; } = 
            Environment.GetEnvironmentVariable(Constants.GoogleApiKeyVariable);

		/// <summary> Gets or sets the name of this application for the Google API. </summary>
		public static string ApplicationName { get; set; } = "MagicConchBot";

		/// <summary> Gets or sets the destination path to copy music to. </summary>
		public static string ServerMusicPath { get; set; } = "";

		/// <summary> Gets or sets the base of the url ex. https://website.com/music/. </summary>
		public static string ServerMusicUrlBase { get; set; } = "https://www.magicconchbot.com/";

        /// <summary> Gets or sets the default playlist. </summary>
        public static Playlist DefaultPlaylist { get; set; }

        /// <summary> Gets or sets the error message shown if the user requests a command in the wrong channel. </summary>
        public static string WrongChannelError { get; set; }

		/// <summary> Gets or sets the name of the role required to use bot music commands. </summary>
		public static string RequiredRole { get; set; } = "ConchControl";
        
        /// <summary>
        /// Gets or sets the id of the bot owner's guild.
        /// </summary>
        public static ulong OwnerGuildId { get; set; }

        /// <summary>
        /// Gets or sets the channel name in which bot commands can be 
        /// </summary>
        public static string BotControlChannel { get; set; }

        /// <summary>
        /// Gets or sets the Client Secret for the SoundCloud API.
        /// </summary>
        public static string SoundCloudClientSecret { get; set; } =
            Environment.GetEnvironmentVariable(Constants.SoundCloudClientSecretVariable);

        /// <summary>
        /// Gets or sets the Client Id for the SoundCloud API.
        /// </summary>
        public static string SoundCloudClientId { get; set; } =
            Environment.GetEnvironmentVariable(Constants.SoundCloudClientIdVariable);

        /// <summary>
        /// Gets or sets the local path for music to be played from.
        /// </summary>
        public static string LocalMusicPath { get; set; }
    }
}