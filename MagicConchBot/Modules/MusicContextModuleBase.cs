using System;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using MagicConchBot.Common.Interfaces;
using MagicConchBot.Common.Types;
using MagicConchBot.Services.Music;
using Microsoft.Extensions.DependencyInjection;

namespace MagicConchBot.Modules
{
    public class ConchInteractionCommandContext : SocketInteractionContext
    {
        private readonly GuildServiceProvider _provider;
        private readonly GuildSettingsProvider _settingsProvider;

        public ConchInteractionCommandContext(
            DiscordSocketClient client, 
            SocketInteraction interaction, 
            IServiceProvider map) : base(client, interaction)
        {
            _provider = map.GetService<GuildServiceProvider>();
            _settingsProvider = map.GetService<GuildSettingsProvider>();
        }

        public IMusicService MusicService => _provider.GetService<IMusicService>(Guild.Id) ?? SetMusicService();

        private IMusicService SetMusicService()
        {
            _provider.AddService<IMusicService, MusicService>(Guild.Id);
            return _provider.GetService<IMusicService>(Guild.Id);
        }

        private GuildSettings _settings;
        public GuildSettings Settings => _settings ??= _settingsProvider.GetSettings(Guild.Id);

        public void SaveSettings() => _settingsProvider.UpdateSettings(Guild.Id, _settings);
    }

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

        private GuildSettings _settings;
        public GuildSettings Settings => _settings ?? (_settings = _settingsProvider.GetSettings(Guild.Id));

        public void SaveSettings() => _settingsProvider.UpdateSettings(Guild.Id, _settings);
    }
}