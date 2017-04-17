using Discord;
using Discord.Commands;
using MagicConchBot.Common.Interfaces;
using MagicConchBot.Services.Music;

namespace MagicConchBot.Modules
{
    public class MusicCommandContext : CommandContext
    {
        private readonly MusicServiceProvider _provider;

        public MusicCommandContext(IDiscordClient client, IUserMessage msg, MusicServiceProvider provider) : base(client, msg)
        {
            _provider = provider;
        }

        public IMusicService MusicService => _provider.GetService(Guild.Id);
    }
}