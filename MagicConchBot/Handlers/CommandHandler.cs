using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using MagicConchBotApp.Common.Interfaces;
using MagicConchBotApp.Modules;
using MagicConchBotApp.Resources;
using MagicConchBotApp.Services;
using MagicConchBotApp.Services.Music;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace MagicConchBotApp.Handlers
{
    public class CommandHandler
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private DiscordSocketClient _client;

        private CommandService _commands;

        private ServiceCollection _services;
        public IServiceProvider ServiceProvider { get; set; }

        // I hate the way this code looks
        public void ConfigureServices(DiscordSocketClient client)
        {
            var googleApiInfoService = new GoogleApiInfoService();
            _services = new ServiceCollection();

            _services.AddSingleton(new SongResolutionService(new List<ISongInfoService>
            {
                googleApiInfoService,
                new SoundCloudInfoService(),
            }));

            _services.AddSingleton(googleApiInfoService);
            _services.AddSingleton(new MusicServiceProvider());
            _services.AddSingleton(new SoundCloudInfoService());
            _services.AddSingleton(new ChanService());
            _services.AddSingleton(new StardewValleyService());
            _services.AddSingleton(new GuildSettingsProvider());
            _services.AddSingleton(client);

            ServiceProvider = _services.BuildServiceProvider();
        }

        public async Task InstallAsync()
        {
            // Create Command Service, inject it into Dependency ServiceProvider
            _client = ServiceProvider.GetService<DiscordSocketClient>();
            _commands = new CommandService();

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
            if (!(parameterMessage is SocketUserMessage message))
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
            var context = new ConchCommandContext(_client, message, ServiceProvider);

            // Execute the Command, store the result
            var result = await _commands.ExecuteAsync(context, argPos, ServiceProvider, MultiMatchHandling.Best);

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
            var songPlayer = new FfmpegSongPlayer();

            var songResolvers = new List<ISongResolver>
            {
                new UrlStreamResolver(),
                new LocalStreamResolver()
            };

            var musicService = new MusicService(songResolvers, songPlayer);

            ServiceProvider.Get<MusicServiceProvider>().AddServices(guild.Id, musicService, new Mp3ConverterService());
            return Task.CompletedTask;
        }
    }
}