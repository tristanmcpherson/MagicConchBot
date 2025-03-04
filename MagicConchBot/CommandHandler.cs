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

        private readonly InteractionService _interactionService;
        private readonly IServiceProvider _services;

        public CommandHandler(InteractionService interactionService, DiscordSocketClient client, IServiceProvider services)
        {
            _interactionService = interactionService;
            _client = client;
            _services = services;
        }

        public void SetupEvents()
        {
            _interactionService.Log += LogAsync;

            _client.InteractionCreated += HandleInteraction;
            //_client.MessageReceived += HandleCommandAsync;
            _client.GuildAvailable += HandleGuildAvailableAsync;
            _client.JoinedGuild += HandleJoinedGuildAsync;
            _client.Ready += ClientReady;
        }

        private async Task ClientReady()
        {
            try
            {
#if DEBUG
                try
                {
                    // Try to register commands to a specific guild for faster testing
                    // This requires the bot to have the applications.commands scope in that guild
                    const ulong testGuildId = 1101360116235767888; // Your test guild ID
                    
                    await _interactionService.RegisterCommandsToGuildAsync(testGuildId);
                    Log.Info($"Registered commands to test guild {testGuildId} in debug mode");
                }
                catch (Discord.Net.HttpException ex) when (ex.DiscordCode == DiscordErrorCode.MissingPermissions) // Missing Access
                {
                    Log.Warn("Could not register commands to test guild due to missing permissions.");
                    Log.Warn("Make sure the bot has the 'applications.commands' scope in the guild.");
                    Log.Warn("Falling back to global command registration...");
                    
                    // Fall back to global registration
                    await _interactionService.RegisterCommandsGloballyAsync();
                    Log.Info("Registered commands globally as fallback");
                }
#else
                // Register commands globally in production
                await _interactionService.RegisterCommandsGloballyAsync();
                Log.Info("Registered commands globally");
#endif

                Log.Info($"Connected as -> {_client.CurrentUser}");
                Log.Info($"We are on {_client.Guilds.Count} servers");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during command registration");
            }
        }

        public async Task InstallAsync()
        {
            //await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
            await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }

        private async Task HandleInteraction(SocketInteraction interaction)
        {
            try
            {
                // Create an execution context
                var ctx = new ConchInteractionCommandContext(_client, interaction, _services);
                
                // Execute the command directly with our custom context
                var result = await _interactionService.ExecuteCommandAsync(ctx, _services);
                
                if (!result.IsSuccess)
                {
                    switch (result.Error)
                    {
                        case InteractionCommandError.UnmetPrecondition:
                            await interaction.RespondAsync($"Unmet Precondition: {result.ErrorReason}", ephemeral: true);
                            break;
                        case InteractionCommandError.UnknownCommand:
                            await interaction.RespondAsync("Unknown command", ephemeral: true);
                            break;
                        case InteractionCommandError.BadArgs:
                            await interaction.RespondAsync("Invalid arguments", ephemeral: true);
                            break;
                        case InteractionCommandError.Exception:
                            await interaction.RespondAsync($"Command exception: {result.ErrorReason}", ephemeral: true);
                            break;
                        case InteractionCommandError.Unsuccessful:
                            await interaction.RespondAsync("Command could not be executed", ephemeral: true);
                            break;
                        default:
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                
                // Try to notify the bot owner
                try
                {
                    var ownerUser = await _client.GetUserAsync(Configuration.Owners.First());
                    await ownerUser.SendMessageAsync($"Error handling interaction: {ex}");
                }
                catch { /* Ignore errors in error handling */ }

                // If a Slash Command execution fails it is most likely that the original interaction acknowledgement will persist.
                if (interaction.Type == InteractionType.ApplicationCommand)
                {
                    try
                    {
                        await interaction.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
                    }
                    catch { /* Ignore errors in cleanup */ }
                }
            }
        }

        //private async Task HandleCommandAsync(SocketMessage parameterMessage)
        //{
        //    // Don't handle the command if it is a system message
        //    if (parameterMessage is not SocketUserMessage message)
        //        return;
        //    if (message.Source != MessageSource.User)
        //        return;

        //    // Mark where the prefix ends and the command begins
        //    var argPos = 0;

        //    // Determine if the message has a valid prefix, adjust argPos 
        //    if (!(message.HasMentionPrefix(_client.CurrentUser, ref argPos) || message.HasCharPrefix('!', ref argPos)))
        //        return;

        //    // Handle case of !! or !!! (some prefixes for other bots)
        //    if (message.Content.Split('!').Length > 2)
        //        return;

        //    // Create a Command Context
        //    var context = new ConchCommandContext(_client, message, _services);

        //    await Task.Factory.StartNew(async () =>
        //    {
        //        // Execute the Command, store the result
        //        //var result = await _commands.ExecuteAsync(context, argPos, _services, MultiMatchHandling.Best);

        //        // If the command failed, notify the user
        //        if (!result.IsSuccess)
        //            if (result.ErrorReason == Configuration.WrongChannelError)
        //                await message.Channel.SendMessageAsync($"{result.ErrorReason}", true);
        //            else
        //                await message.Channel.SendMessageAsync($"**Error:** {result.ErrorReason}");
        //    }, TaskCreationOptions.LongRunning).ConfigureAwait(false);
        //}

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

        private Task HandleGuildAvailableAsync(SocketGuild guild)
        {
            return Task.Run(() =>
            {
                var guildServiceProvider = _services.GetService<GuildServiceProvider>();
                guildServiceProvider
                    .AddService<YoutubeClient, YoutubeClient>(guild.Id)
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