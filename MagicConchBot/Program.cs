using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.WebSocket;
using log4net;
using MagicConchBot.Handlers;
using MagicConchBot.Resources;
using Nito.AsyncEx;

namespace MagicConchBot
{
    public class Program
    {
        public const string RequiredRoleName = "Conch Control";

        private static readonly ILog Log =
            LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

            private static DiscordSocketClient _client;
            private static CommandHandler _handler;

        public static void Main()
        {
            Console.WriteLine("Starting Magic Conch Bot. Press 'q' at any time to quit.");
            var cts = new CancellationTokenSource();

            try
            {
                Task.Factory.StartNew(async () => await new Program().MainAsync(cts.Token));

                while (true)
                {
                    if (Console.KeyAvailable)
                    {
                        if (Console.ReadKey(true).Key == ConsoleKey.Q)
                        {
                            cts.Cancel();
                            break;
                        }
                    }
                }
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("Bot exited successfully.");
            }
            finally
            {
                Console.WriteLine("Press enter to continue . . .");
                Console.ReadLine();
            }
        }

        private async Task MainAsync(CancellationToken cancellationToken)
        {
            EnsureConfigExists();

            var map = new DependencyMap();

            try
            {
                _client = new DiscordSocketClient(new DiscordSocketConfig
                {
                    LogLevel = LogSeverity.Info,
                    AudioMode = AudioMode.Outgoing
                });

                _client.Log += WriteToLog;

                // Login and connect to Discord.
                await _client.LoginAsync(TokenType.Bot, Configuration.Load().Token).ConfigureAwait(false);
                await _client.ConnectAsync().ConfigureAwait(false);

                map.Add(_client);

                _handler = new CommandHandler();
                await _handler.InstallAsync(map).ConfigureAwait(false);
                _handler.ConfigureServices(map);

                await Task.Delay(-1, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                map.Get<Services.FfmpegMusicService>().Stop();
                await Task.Delay(3000);
                await _client.DisconnectAsync();
            }
        }

        private static Task WriteToLog(LogMessage message)
        {
            if (message.Message.Contains("Unknown OpCode"))
                return Task.CompletedTask;

            switch (message.Severity)
            {
                case LogSeverity.Debug:
                    Log.Debug(message.Message, message.Exception);
                    break;
                case LogSeverity.Verbose:
                case LogSeverity.Info:
                    Log.Info(message.Message, message.Exception);
                    break;
                case LogSeverity.Warning:
                    Log.Warn(message.Message, message.Exception);
                    break;
                case LogSeverity.Error:
                case LogSeverity.Critical:
                    Log.Fatal(message.Message, message.Exception);
                    break;
            }

            return Task.CompletedTask;
        }

        private static void EnsureConfigExists()
        {
            var loc = Path.Combine(AppContext.BaseDirectory, "Configuration.json");

            if (!File.Exists(loc))                              // Check if the configuration file exists.
            {
                var config = new Configuration();               // Create a new configuration object.

                Console.WriteLine("The configuration file has been created at 'Configuration.json', " +
                                  "please enter your information and restart.");
                Console.Write("Token: ");

                config.Token = Console.ReadLine();              // Read the bot token from console.
                config.Save();                                  // Save the new configuration object to file.
            }
            Console.WriteLine("Configuration Loaded...");
        }
    }
}