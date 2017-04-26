﻿using Discord;
using Discord.Commands;
using MagicConchBot.Common.Interfaces;
using MagicConchBot.Common.Types;
using MagicConchBot.Services.Music;

namespace MagicConchBot.Modules
{
    public class ConchCommandContext : CommandContext
    {
        private readonly MusicServiceProvider _provider;
        private readonly GuildSettingsProvider _settingsProvider;

        public ConchCommandContext(IDiscordClient client, IUserMessage msg, IDependencyMap map) : base(client, msg)
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