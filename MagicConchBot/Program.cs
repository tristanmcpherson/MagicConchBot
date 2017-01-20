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
            var cancellationTokenSource = new CancellationTokenSource();
            Console.WriteLine("Starting Magic Conch Bot. Press 'q' at any time to quit.");

            var program = new Program();
            Task.Factory.StartNew(async () => await program.StartAsync(cancellationTokenSource.Token), cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            while (true)
            {
                if (Console.ReadKey(true).Key == ConsoleKey.Q)
                {
                    Console.WriteLine("Stopping bot.");
                    cancellationTokenSource.Cancel();
                    _client.DisconnectAsync();
                    break;
                }
            }

            Thread.Sleep(1000);

            Console.WriteLine("Press enter to continue . . .");
            Console.ReadLine();
        }

        private async Task StartAsync(CancellationToken cancellationToken)
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
                await _client.LoginAsync(TokenType.Bot, Configuration.Load().Token);
                await _client.ConnectAsync();

                map.Add(_client);

                _handler = new CommandHandler();
                await _handler.InstallAsync(map);
                _handler.ConfigureServices(map);

                await Task.Delay(-1, cancellationToken);
            }
            finally
            {
                map.Get<Services.FfmpegMusicService>().Stop();
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