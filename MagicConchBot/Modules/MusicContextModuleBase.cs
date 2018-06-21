using System;
using Discord;
using Discord.Commands;
using MagicConchBotApp.Common.Interfaces;
using MagicConchBotApp.Common.Types;
using MagicConchBotApp.Services.Music;

namespace MagicConchBotApp.Modules
{
    public class ConchCommandContext : CommandContext
    {
        private readonly MusicServiceProvider _provider;
        private readonly GuildSettingsProvider _settingsProvider;

        public ConchCommandContext(IDiscordClient client, IUserMessage msg, IServiceProvider map) : base(client, msg)
        {
            _provider = map.Get<MusicServiceProvider>();
            _settingsProvider = map.Get<GuildSettingsProvider>();
        }

        public IMusicService MusicService => _provider.GetService(Guild.Id);

        private GuildSettings _settings;
        public GuildSettings Settings => _settings ?? (_settings = _settingsProvider.GetSettings(Guild.Id));

        public void SaveSettings() => _settingsProvider.UpdateSettings(Guild.Id, _settings);
    }
}