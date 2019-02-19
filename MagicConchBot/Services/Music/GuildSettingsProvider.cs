using LiteDB;
using MagicConchBot.Common.Types;

namespace MagicConchBot.Services.Music {
	public class GuildSettingsProvider {
		private readonly LiteDatabase _db;

		public GuildSettingsProvider() {
			_db = new LiteDatabase(@"Settings.db");
			//_db.Engine.EnsureIndex("GuildSettings", "");
		}

		public GuildSettings GetSettings(ulong guildId) {
			var settings = _db.GetCollection<GuildSettings>().FindById(guildId);
			if (settings == null) {
				settings = new GuildSettings();
				UpdateSettings(guildId, settings);
			}
			return settings;
		}

		public void UpdateSettings(ulong guildId, GuildSettings settings) {
			_db.GetCollection<GuildSettings>().Upsert(guildId, settings);
		}
	}
}
