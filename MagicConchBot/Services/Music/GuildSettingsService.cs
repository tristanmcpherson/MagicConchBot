using System.Collections.Generic;
using LiteDB;
using MagicConchBot.Common.Types;

namespace MagicConchBot.Services.Music
{
    public class GuildSettingsService
    {
        private readonly LiteDatabase _db;

        public GuildSettingsService()
        {
            _db = new LiteDatabase(@"Settings.db");
        }

        public GuildSettings GetSettings(ulong guildId)
        {
            var settings = _db.GetCollection<GuildSettings>().FindById(guildId);
            if (settings == null)
            {
                settings = new GuildSettings();
                UpdateSettings(guildId, settings);
            }
            return settings;
        }

        public void UpdateSettings(ulong guildId, GuildSettings settings)
        {
            _db.GetCollection<GuildSettings>().Upsert(guildId, settings);
        }
    }
}
