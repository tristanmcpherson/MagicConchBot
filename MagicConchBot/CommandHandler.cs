using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using MagicConchBot.Common.Interfaces;
using MagicConchBot.Modules;
using MagicConchBot.Resources;
using MagicConchBot.Services;
using MagicConchBot.Services.Music;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NLog;
using YoutubeExplode;

namespace MagicConchBot.Handlers
{
    public class CommandHandler
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly DiscordSocketClient _client;

        private readonly CommandService _commands;
        private readonly InteractionService _interactionService;
        private readonly IServiceProvider _services;

        public CommandHandler(InteractionService interactionService, DiscordSocketClient client, CommandService commands, IServiceProvider services)
        {
            _interactionService = interactionService;
            _client = client;
            _commands = commands;
            _services = services;
        }

        public void SetupEvents()
        {
            _commands.Log += LogAsync;
            _interactionService.Log += LogAsync;

            _client.InteractionCreated += HandleInteraction;
            _client.MessageReceived += HandleCommandAsync;
            _client.GuildAvailable += HandleGuildAvailableAsync;
            _client.JoinedGuild += HandleJoinedGuildAsync;
            _client.Ready += ClientReady;
        }

        private async Task ClientReady()
        {
#if DEBUG
            await _interactionService.RegisterCommandsToGuildAsync(1101360116235767888);
#else
            await _interactionService.RegisterCommandsGloballyAsync();
#endif
        }

        public async Task InstallAsync()
        {
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
            await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }

        private async Task HandleInteraction(SocketInteraction arg)
        {
            try
            {
                // Create an execution context that matches the generic type parameter of your InteractionModuleBase<T> modules
                var ctx = new ConchInteractionCommandContext(_client, arg, _services);
                await _interactionService.ExecuteCommandAsync(ctx, _services);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                var ownerUser = await _client.GetUserAsync(Configuration.Owners.First());
                await ownerUser.SendMessageAsync(ex.ToString());


                // If a Slash Command execution fails it is most likely that the original interaction acknowledgement will persist. It is a good idea to delete the original
                // response, or at least let the user know that something went wrong during the command execution.
                if (arg.Type == InteractionType.ApplicationCommand)
                    await arg.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
            }
        }

        private async Task HandleCommandAsync(SocketMessage parameterMessage)
        {
            // Don't handle the command if it is a system message
            if (parameterMessage is not SocketUserMessage message)
                return;
            if (message.Source != MessageSource.User)
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
            var context = new ConchCommandContext(_client, message, _services);

            await Task.Factory.StartNew(async () =>
            {
                // Execute the Command, store the result
                var result = await _commands.ExecuteAsync(context, argPos, _services, MultiMatchHandling.Best);

                // If the command failed, notify the user
                if (!result.IsSuccess)
                    if (result.ErrorReason == Configuration.WrongChannelError)
                        await message.Channel.SendMessageAsync($"{result.ErrorReason}", true);
                    else
                        await message.Channel.SendMessageAsync($"**Error:** {result.ErrorReason}");
            }, TaskCreationOptions.LongRunning).ConfigureAwait(false);
        }

        public async Task LogAsync(LogMessage logMessage) {
            if (logMessage.Exception is Exception exception)
            {
                var ownerUser = await _client.GetUserAsync(Configuration.Owners.First());
                await ownerUser.SendMessageAsync(exception.ToString());
            }
            else if (logMessage.Exception is CommandException cmdException) {
                // We can tell the user that something unexpected has happened
                await cmdException.Context.Channel.SendMessageAsync("Something went catastrophically wrong!");

                var ownerUser = await _client.GetUserAsync(Configuration.Owners.First());
                await ownerUser.SendMessageAsync(cmdException.ToString());

                // We can also log this incident
                Console.WriteLine($"{cmdException.Context.User} failed to execute '{cmdException.Command.Name}' in {cmdException.Context.Channel}.");
                Console.WriteLine(cmdException.ToString());
            }
        }

        private async Task HandleJoinedGuildAsync(SocketGuild arg)
        {
            await _interactionService.RegisterCommandsToGuildAsync(arg.Id);
            await HandleGuildAvailableAsync(arg);
            await arg.DefaultChannel.SendMessageAsync($"All hail the Magic Conch. In order to use the Music functions of this bot, please create a role named '{Configuration.RequiredRole}' and add that role to the users whom you want to be able to control the Music functions of this bot. Type !help for help.");
        }

        private static readonly List<Cookie> cookies = ParseCookiesFromJson(Environment.GetEnvironmentVariable("YOUTUBE_COOKIE"));
        public record CookieData(string Name, string Value, string Domain, string Path, bool Secure, bool HttpOnly, double? ExpirationDate);

        public static List<Cookie> ParseCookiesFromJson(string json)
        {
            var cookieData = JsonConvert.DeserializeObject<CookieData[]>(json);
            var cookies = new List<Cookie>();

            foreach (var data in cookieData)
            {
                var cookie = new Cookie
                {
                    Name = data.Name,
                    Value = data.Value,
                    Domain = data.Domain,
                    Path = data.Path,
                    Secure = data.Secure,
                    HttpOnly = data.HttpOnly
                };

                // Set expiration if available and not a session cookie
                if (data.ExpirationDate.HasValue)
                {
                    cookie.Expires = DateTimeOffset.FromUnixTimeSeconds((long)data.ExpirationDate.Value).UtcDateTime;
                }

                cookies.Add(cookie);
            }

            return cookies;
        }

        private Task HandleGuildAvailableAsync(SocketGuild guild)
        {
            return Task.Run(() =>
            {
                var guildServiceProvider = _services.GetService<GuildServiceProvider>();
                guildServiceProvider
                    //.AddService<YoutubeClient>(new YoutubeClient(cookies))
                    .AddService<YoutubeClient, YoutubeClient>(guild.Id, new YoutubeClient(cookies))
                    .AddService<ISongInfoService, YoutubeInfoService>(guild.Id)
                    .AddService<ISongInfoService, SoundCloudInfoService>(guild.Id)
                    .AddService<ISongInfoService, SpotifyResolveService>(guild.Id)
                    .AddService<ISongInfoService, BandcampResolveService>(guild.Id)
                    .AddService<ISongInfoService, DirectPlaySongResolver>(guild.Id)
                    .AddService<ISongInfoService, LocalStreamResolver>(guild.Id)
                    .AddService<ISongPlayer, FfmpegSongPlayer>(guild.Id)
                    .AddService<IMusicService, MusicService>(guild.Id)
                    .AddService<IMp3ConverterService, Mp3ConverterService>(guild.Id);

            });
        }
    }
}