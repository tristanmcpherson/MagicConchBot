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
using System.Text.Json;

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
            using var services = ConfigureServices();
            _client = services.GetService<DiscordSocketClient>();
            var _interactionService = services.GetService<InteractionService>();

            try
            {
                _client.Log += a => { Log.WriteToLog(a); return Task.CompletedTask; };
                var commandHandler = services.GetService<CommandHandler>();

                commandHandler.SetupEvents();
                await commandHandler.InstallAsync();




                // Configuration.Load().Token
                await _client.LoginAsync(TokenType.Bot, Configuration.Token);
                await _client.StartAsync();




                await Task.Delay(-1, cancellationToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception ex)
            {
                Log.WriteToLog(new LogMessage(LogSeverity.Critical, string.Empty, ex.ToString(), ex));
            }
            finally
            {
                //services.GetService<GuildServiceProvider>().StopAll();
                await _client.StopAsync();
            }
        }

        private static readonly List<Cookie> cookies = ParseCookiesFromJson(Environment.GetEnvironmentVariable("YOUTUBE_COOKIE"));
        public class CookieData
        {
            public string Name { get; set; }
            public string Value { get; set; }
            public string Domain { get; set; }
            public string Path { get; set; }
            public bool Secure { get; set; }
            public bool HttpOnly { get; set; }
            public double? ExpirationDate { get; set; } // Nullable for session cookies
        }
        public static List<Cookie> ParseCookiesFromJson(string json)
        {
            var cookieData = JsonSerializer.Deserialize<List<CookieData>>(json);
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



        public static ServiceProvider ConfigureServices()
        {
            var config = new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info

            };

            return new ServiceCollection()
                .AddSingleton(config)
                .AddMemoryCache()
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<InteractionService>()
                .AddSingleton<FirestoreDb>(FirestoreDb.Create("magicconchbot"))
                .AddSingleton<HttpClient>()
                .AddSingleton<CommandHandler>()
                .AddSingleton<CommandService>()
                .AddSingleton(new YoutubeClient(cookies))
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
                .BuildServiceProvider();
        }
    }
}