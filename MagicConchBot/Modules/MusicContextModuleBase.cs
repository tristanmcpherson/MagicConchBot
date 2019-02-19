using System;
using Discord;
using Discord.Commands;
using MagicConchBot.Common.Interfaces;
using MagicConchBot.Common.Types;
using MagicConchBot.Services;
using MagicConchBot.Services.Music;
using Microsoft.Extensions.DependencyInjection;

namespace MagicConchBot.Modules
{
    public class ConchCommandContext : CommandContext
    {
        private readonly GuildServiceProvider _provider;
        private readonly GuildSettingsProvider _settingsProvider;

        public ConchCommandContext(IDiscordClient client, IUserMessage msg, IServiceProvider map) : base(client, msg)
        {
            _provider = map.GetService<GuildServiceProvider>();
            _settingsProvider = map.GetService<GuildSettingsProvider>();
        }

        public IMusicService MusicService => _provider.GetService<IMusicService>(Guild.Id);
        public IMp3ConverterService Mp3Service => _provider.GetService<IMp3ConverterService>(Guild.Id);

        private GuildSettings _settings;
        public GuildSettings Settings => _settings ?? (_settings = _settingsProvider.GetSettings(Guild.Id));

        public void SaveSettings() => _settingsProvider.UpdateSettings(Guild.Id, _settings);
    }
}