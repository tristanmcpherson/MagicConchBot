using System.Reflection;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using MagicConchBot.Services;
using MagicConchBot.Resources;

namespace MagicConchBot.Handlers
{
    public class CommandHandler
    {
        private CommandService _commands;
        private DiscordSocketClient _client;
        private IDependencyMap _map;

        public void ConfigureServices(IDependencyMap map)
        {
            map.Add(new MusicServiceProvider());
            map.Add(new GoogleApiService());
            map.Add(new YouTubeInfoService(map));
            map.Add(new SoundCloudInfoService());
        }

        public async Task InstallAsync(IDependencyMap map)
        {
            // Create Command Service, inject it into Dependency Map
            _client = map.Get<DiscordSocketClient>();
            _commands = new CommandService();
            _map = map;

            _map.Add(_commands);

            await _commands.AddModulesAsync(Assembly.GetEntryAssembly());

            _client.MessageReceived += HandleCommandAsync;
            _client.GuildAvailable += HandleGuildAvailable;
            _client.JoinedGuild += HandleJoinedGuildAsync;
        }

        private async Task HandleJoinedGuildAsync(SocketGuild arg)
        {
            var channel = arg.GetChannel(arg.DefaultChannelId) as ISocketMessageChannel;
            await channel.SendMessageAsync($"All hail the Magic Conch. In order to use the Music functions of this bot, please create a roll named '{Constants.RequiredRoleName}' and add that role to the users whom you want to be able to control the Music functions of this bot. Type !help for help.");
        }

        private Task HandleGuildAvailable(SocketGuild arg)
        {
            _map.Get<MusicServiceProvider>().AddService(arg.Id, new FfmpegMusicService());
            return Task.CompletedTask;
        }

        public async Task HandleCommandAsync(SocketMessage parameterMessage)
        {
            // Don't handle the command if it is a system message
            var message = parameterMessage as SocketUserMessage;
            if (message == null) return;

            // Mark where the prefix ends and the command begins
            var argPos = 0;
            // Determine if the message has a valid prefix, adjust argPos 
            if (!(message.HasMentionPrefix(_client.CurrentUser, ref argPos) || message.HasCharPrefix('!', ref argPos))) return;

            // Create a Command Context
            var context = new CommandContext(_client, message);
            // Execute the Command, store the result
            var result = await _commands.ExecuteAsync(context, argPos, _map);

            // If the command failed, notify the user
            if (!result.IsSuccess)
                await message.Channel.SendMessageAsync($"**Error:** {result.ErrorReason}");
        }
    }
}
