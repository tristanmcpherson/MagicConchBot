using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MagicConchBot.Services.Music;
using MagicConchBot.Common.Interfaces;
using MagicConchBot.Handlers;
using MagicConchBot.Helpers;
using MagicConchBot.Resources;
using MagicConchBot.Services;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System.Net.Http;
using Discord.Interactions;
using Google.Cloud.Firestore;
using System.Collections.Generic;
using System.Net;
using YoutubeExplode;
using Newtonsoft.Json;

namespace MagicConchBot
{
    public class Program
    {
        // Release: https://discord.com/api/oauth2/authorize?client_id=267000484420780045&permissions=8&scope=applications.commands%20bot
        // Debug:   https://discord.com/api/oauth2/authorize?client_id=295020167732396032&permissions=8&scope=applications.commands%20bot

        private static CancellationTokenSource _cts;
        private static DiscordSocketClient _client;

        private static Logger Log = LogManager.GetCurrentClassLogger();

        private static string Version => Assembly.GetEntryAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            .InformationalVersion;

        public static void Main()
        {
            Logging.ConfigureLogs();

			Log.Info("To add this bot, use the url: https://discordapp.com/oauth2/authorize?client_id=267000484420780045&scope=bot&permissions=540048384");

            Log.Info("Starting Magic Conch Bot. Press 'q' at any time to quit.");

            Log.Info($"Version: {Version}");
            CheckUpToDate().Wait();

            try
            {
                _cts = new CancellationTokenSource();
                Task.Factory.StartNew(async () => await MainAsync(_cts.Token), _cts.Token).Wait();

                while (!_cts.Token.IsCancellationRequested)
                {
                    if (!Console.IsInputRedirected && Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(true).Key;
                        if (key == ConsoleKey.Q) 
                        {
                            Stop();
                        }
                        else if (key == ConsoleKey.G)
                        {
                            Log.Info("Listing guilds: ");
                            foreach (var guild in _client.Guilds)
                            {
                                Log.Info($"{guild.Name} - '{guild?.Owner?.Username}:{guild?.Owner?.Id}'");
                            }
                        }
                        continue;
                    }

                    Thread.Sleep(100);
                }
            }
            finally
            {
                Log.Info("Bot sucessfully exited.");
                Console.WriteLine("Press enter to continue . . .");
                Console.ReadLine();
            }
        }

        public static void Stop()
        {
            _cts.Cancel();
        }

        private static async Task CheckUpToDate()
        {
            if (AppHelper.Version.Contains("dev"))
            {
                DebugTools.Debug = true;
                Log.Info("Bot is using a debug version.");
                return;
            }
            if (await WebHelper.UpToDateWithGitHub())
            {
                Log.Info("Bot is up to date! :)");
            }
            else
            {
                Log.Warn("Bot is not up to date, please update!");
            }
        }

        private static async Task MainAsync(CancellationToken cancellationToken)
        {
            try
            {
                Log.Info("Configuring services...");
                using var services = ConfigureServices();
                
                Log.Info("Initializing Discord client...");
                _client = services.GetService<DiscordSocketClient>();

                // Set up logging before anything else
                _client.Log += a => { Log.WriteToLog(a); return Task.CompletedTask; };
                
                try
                {
                    Log.Info("Setting up command handler...");
                    var commandHandler = services.GetService<CommandHandler>();
                    commandHandler.SetupEvents();
                    await commandHandler.InstallAsync();

                    // Check if token is valid
                    if (string.IsNullOrEmpty(Configuration.Token))
                    {
                        throw new InvalidOperationException("Discord token is null or empty. Please check your configuration.");
                    }
                    
                    Log.Info("Logging in to Discord...");
                    await _client.LoginAsync(TokenType.Bot, Configuration.Token);
                    
                    Log.Info("Starting Discord client...");
                    await _client.StartAsync();

                    Log.Info("Bot startup complete!");
                    await Task.Delay(-1, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error during bot initialization");
                    throw; // Re-throw to be caught by outer try/catch
                }
            }
            catch (TaskCanceledException)
            {
                Log.Info("Bot shutdown requested via cancellation token");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Critical error in MainAsync");
                Console.WriteLine($"CRITICAL ERROR: {ex}");
            }
            finally
            {
                Log.Info("Shutting down Discord client...");
                if (_client != null)
                {
                    try
                    {
                        await _client.StopAsync();
                        Log.Info("Discord client stopped successfully");
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error stopping Discord client");
                    }
                }
                Log.Info("Bot shutdown complete");
            }
        }

        public static ServiceProvider ConfigureServices()
        {
            try
            {
                Log.Info("Configuring Discord client...");
                var config = new DiscordSocketConfig
                {
                    LogLevel = LogSeverity.Info,
                    GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent,
                    AlwaysDownloadUsers = true
                };

                Log.Info("Configuring interaction service...");
                var interactionServiceConfig = new InteractionServiceConfig
                {
                    DefaultRunMode = Discord.Interactions.RunMode.Async,
                    LogLevel = LogSeverity.Info
                };

                Log.Info("Building service provider...");
                var serviceCollection = new ServiceCollection()
                    .AddSingleton(config)
                    .AddMemoryCache()
                    .AddSingleton<DiscordSocketClient>();
                    
                Log.Info("Adding interaction service...");
                serviceCollection.AddSingleton(x => 
                {
                    Log.Info("Creating interaction service instance...");
                    return new InteractionService(x.GetRequiredService<DiscordSocketClient>(), interactionServiceConfig);
                });
                    
                Log.Info("Adding remaining services...");
                serviceCollection
                    .AddSingleton<FirestoreDb>(FirestoreDb.Create("magicconchbot"))
                    .AddSingleton<HttpClient>()
                    .AddSingleton<YoutubeClient, YoutubeClient>()
                    .AddSingleton<YoutubeInfoService>()
                    .AddSingleton<IMp3ConverterService, Mp3ConverterService>()
                    .AddSingleton<ISongInfoService, YoutubeInfoService>()
                    .AddSingleton<ISongInfoService, SoundCloudInfoService>()
                    .AddSingleton<ISongInfoService, SpotifyResolveService>()
                    .AddSingleton<ISongInfoService, BandcampResolveService>()
                    .AddSingleton<ISongInfoService, DirectPlaySongResolver>()
                    //.AddSingleton<ISongInfoService, YoutubeDlResolver>()
                    .AddSingleton<ISongResolutionService, SongResolutionService>()
                    .AddSingleton<GuildServiceProvider>()
                    .AddSingleton<SoundCloudInfoService>()
                    .AddSingleton<GuildSettingsProvider>()
                    .AddSingleton<CommandHandler>()
                    .AddSingleton<CommandService>();
                    
                Log.Info("Building service provider...");
                return serviceCollection.BuildServiceProvider();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error configuring services");
                throw; // Re-throw to be caught by MainAsync
            }
        }
    }
}