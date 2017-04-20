using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using MagicConchBot.Common.Interfaces;
using MagicConchBot.Modules;
using MagicConchBot.Resources;
using MagicConchBot.Services;
using MagicConchBot.Services.Music;
using NLog;

namespace MagicConchBot.Handlers
{
    public class CommandHandler
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private DiscordShardedClient _client;

        private CmdSrv _commands;
        private IDependencyMap _map;

        // I hate the way this code looks
        public void ConfigureServices(IDependencyMap depMap)
        {
            var googleApiInfoService = new GoogleApiInfoService();

            _map = depMap;
            _map.Add(new SongResolutionService(new List<ISongInfoService>
            {
                googleApiInfoService,
                new SoundCloudInfoService(),
            }));

            _map.Add(googleApiInfoService);
            _map.Add(new MusicServiceProvider());
            _map.Add(new SoundCloudInfoService());
            _map.Add(new ChanService());
            _map.Add(new StardewValleyService());
            _map.Add(new GuildSettingsService());
        }

        public async Task InstallAsync()
        {
            // Create Command Service, inject it into Dependency Map
            _client = _map.Get<DiscordShardedClient>();
            _commands = new CmdSrv();

            //_map.Add(_commands);

            await _commands.AddModulesAsync(Assembly.GetEntryAssembly());

            _client.MessageReceived += HandleCommandAsync;
            _client.GuildAvailable += HandleGuildAvailableAsync;
            _client.JoinedGuild += HandleJoinedGuildAsync;
            _client.MessageReceived += HandleMessageReceivedAsync;
        }

        private async Task HandleCommandAsync(SocketMessage parameterMessage)
        {
            // Don't handle the command if it is a system message
            var message = parameterMessage as SocketUserMessage;
            if (message == null)
                return;

            // Mark where the prefix ends and the command begins
            var argPos = 0;

            // Determine if the message has a valid prefix, adjust argPos 
            if (!(message.HasMentionPrefix(_client.CurrentUser, ref argPos) || message.HasCharPrefix('!', ref argPos)))
                return;

            // Handle case of !! or !!! (some prefixes for other bots)
            if (message.Content.Split('!').Length > 2)
                return;

            // Create a Command Context
            var context = new MusicCommandContext(_client, message, _map.Get<MusicServiceProvider>());

            // Execute the Command, store the result
            var result = await _commands.ExecuteAsync(context, argPos, _map, MultiMatchHandling.Best);

            // If the command failed, notify the user
            if (!result.IsSuccess)
                if (result.ErrorReason == Configuration.Load().WrongChannelError)
                    await message.Channel.SendMessageAsync($"{result.ErrorReason}", true);
                else
                    await message.Channel.SendMessageAsync($"**Error:** {result.ErrorReason}");
        }

        private static async Task HandleMessageReceivedAsync(SocketMessage arg)
        {
            foreach (var attachment in arg.Attachments)
            {
                if (attachment.Filename.EndsWith(".webm"))
                {
                    Log.Info($"Url: {attachment.Url}");
                    Log.Info($"Proxy: {attachment.ProxyUrl}");
                    await Task.Delay(1);
                }
            }
        }

        private async Task HandleJoinedGuildAsync(SocketGuild arg)
        {
            await arg.DefaultChannel.SendMessageAsync($"All hail the Magic Conch. In order to use the Music functions of this bot, please create a role named '{Configuration.Load().RequiredRole}' and add that role to the users whom you want to be able to control the Music functions of this bot. Type !help for help.");
            await HandleGuildAvailableAsync(arg);
        }

        private Task HandleGuildAvailableAsync(SocketGuild guild)
        {
            var fileProvider = new StreamingFileProvider();
            var songPlayer = new FfmpegSongPlayer(fileProvider);
            var urlResolver = new UrlStreamResolver();
            var fileResolver = new LocalStreamResolver();

            var musicService = new MusicService(new List<ISongResolver> { fileResolver, urlResolver }, songPlayer);

            _map.Get<MusicServiceProvider>().AddServices(guild.Id, musicService, new Mp3ConverterService());
            return Task.CompletedTask;
        }
    }
}