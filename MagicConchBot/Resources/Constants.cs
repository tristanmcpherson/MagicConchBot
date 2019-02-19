using Discord;

namespace MagicConchBot.Resources
{
    public static class Constants
    {
        public const string RepoLink = "https://github.com/tristanmcpherson/MagicConchBot";

        public static Color MaterialBlue { get; } = new Color(33, 150, 243);

		public const string DiscordTokenVariable = "DISCORD_TOKEN";
		public const string OwnerVariable = "DISCORD_BOT_OWNER";
        public const string BlacklistVariable = "BLACKLIST";
		public const string GoogleApiKeyVariable = "GOOGLE_API_KEY";
        public const string SoundCloudClientSecretVariable = "SOUNDCLOUD_CLIENTSECRET";
        public const string SoundCloudClientIdVariable = "SOUNDCLOUD_CLIENTID";
	}
}