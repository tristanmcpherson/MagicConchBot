using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MagicConchBot.Handlers;
using MagicConchBot.Resources;
using MagicConchBot.Services.Music;
using NLog;
using NLog.Conditions;
using NLog.Config;
using NLog.Targets;

namespace MagicConchBot
{
    public class Program
    {
        // Release: https://discordapp.com/oauth2/authorize?client_id=267000484420780045&scope=bot&permissions=540048384
        // Debug: https://discordapp.com/oauth2/authorize?client_id=295020167732396032&scope=bot&permissions=540048384
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static DiscordSocketClient _client;
        private static CommandHandler _handler;
        private static CancellationTokenSource _cts;

        public static void Main()
        {
            ConfigureLogs();

            Log.Info($"Version: {Assembly.GetEntryAssembly().GetName().Version}");
            EnsureConfigExists();
            MusicServiceProvider.OnLoad();

            Console.WriteLine("Starting Magic Conch Bot. Press 'q' at any time to quit.");

            try
            {
                _cts = new CancellationTokenSource();
                Task.Factory.StartNew(async () => await MainAsync(_cts.Token), _cts.Token).Wait();

                while (!_cts.Token.IsCancellationRequested)
                {
                    HandleKeypress();
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

        private static void HandleKeypress()
        {
            if (!Console.KeyAvailable)
                return;

            var key = Console.ReadKey(true).Key;
            if (key == ConsoleKey.Q)
            {
                _cts.Cancel();
            }
            else if (key == ConsoleKey.S)
            {
                var config = Configuration.Load();
                var serverId = config.OwnerGuildId;
                Console.WriteLine("Skipping song.");
                var channel = (IMessageChannel)_client.GetGuild(serverId)?.Channels.FirstOrDefault(c => c.Name == config.BotControlChannel);
                if (channel == null)
                    return;
                if (MusicServiceProvider.GetService(serverId).Skip())
                    channel.SendMessageAsync("Skipping song at request of owner.");
                else
                    Console.WriteLine("No song to skip.");
            }
        }

        private static async Task MainAsync(CancellationToken cancellationToken)
        {
            var map = new DependencyMap();

            try
            {
                _handler = new CommandHandler();
                _handler.ConfigureServices(map);

                _client = new DiscordSocketClient(new DiscordSocketConfig
                {
                    LogLevel = LogSeverity.Info
                });

                _client.Log += WriteToLog;

                // Login and connect to Discord.
                map.Add(_client);

                await _handler.InstallAsync().ConfigureAwait(false);

                // Configuration.Load().Token
                await _client.LoginAsync(TokenType.Bot, Configuration.Load().Token).ConfigureAwait(false);
                await _client.StartAsync().ConfigureAwait(false);

                await Task.Delay(-1, cancellationToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception ex)
            {
                await WriteToLog(new LogMessage(LogSeverity.Critical, string.Empty, string.Empty, ex));
            }
            finally
            {
                MusicServiceProvider.StopAll();
                await _client.StopAsync();
            }
        }

        private static void ConfigureLogs()
        {
            // Step 1. Create configuration object 
            var config = new LoggingConfiguration();

            // Step 2. Create targets and add them to the configuration 
            var consoleTarget = new ColoredConsoleTarget();

            config.AddTarget("console", consoleTarget);

            var fileTarget = new FileTarget();
            config.AddTarget("file", fileTarget);

            consoleTarget.UseDefaultRowHighlightingRules = false;

            ConsoleRowHighlightingRule RowHighlight(LogLevel loglevel, ConsoleOutputColor foregroundColor,
                ConsoleOutputColor backgroundColor = ConsoleOutputColor.Black)
            {
                var condition = ConditionParser.ParseExpression($"level == {loglevel.GetType().Name}.{loglevel}");
                return new ConsoleRowHighlightingRule(condition, foregroundColor, backgroundColor);
            }

            consoleTarget.RowHighlightingRules.Add(RowHighlight(LogLevel.Info, ConsoleOutputColor.Green));
            consoleTarget.RowHighlightingRules.Add(RowHighlight(LogLevel.Debug, ConsoleOutputColor.Yellow));
            consoleTarget.RowHighlightingRules.Add(RowHighlight(LogLevel.Fatal, ConsoleOutputColor.Red));
            consoleTarget.RowHighlightingRules.Add(RowHighlight(LogLevel.Warn, ConsoleOutputColor.Blue));

            // Step 3. Set target properties 
            consoleTarget.Layout = @"[${date:format=HH\:mm\:ss}][${level:uppercase=true}] ${message} ${exception}";
            fileTarget.FileName = "log.txt";
            fileTarget.Layout = @"[${date:format=HH\:mm\:ss}][${level:uppercase=true}] ${message} ${exception}";

            // Step 4. Define rules
            var rule1 = new LoggingRule("*", LogLevel.Debug, consoleTarget);
            config.LoggingRules.Add(rule1);

            var rule2 = new LoggingRule("*", LogLevel.Debug, fileTarget);
            config.LoggingRules.Add(rule2);

            // Step 5. Activate the configuration
            LogManager.Configuration = config;
        }

        private static Task WriteToLog(LogMessage message)
        {
            if (message.Message != null && message.Message.Contains("Unknown OpCode"))
                return Task.CompletedTask;

            switch (message.Severity)
            {
                case LogSeverity.Debug:
                    Log.Debug(message.Exception, message.Message);
                    break;
                case LogSeverity.Verbose:
                case LogSeverity.Info:
                    Log.Info(message.Exception, message.Message);
                    break;
                case LogSeverity.Warning:
                    Log.Warn(message.Exception, message.Message);
                    break;
                case LogSeverity.Error:
                case LogSeverity.Critical:
                    Log.Fatal(message.Exception, message.Message);
                    break;
            }

            return Task.CompletedTask;
        }

        private static void EnsureConfigExists()
        {
            var loc = Path.Combine(AppContext.BaseDirectory, "Configuration.json");

            // Check if the configuration file exists.
            if (!File.Exists(loc))
            {
                var config = new Configuration(); // Create a new configuration object.
                config.Save(); // Save the new configuration object to file.

                Console.WriteLine("The configuration file has been created at 'Configuration.json', please enter your information and restart.");
                Console.Write("Token: ");

                config.Token = Console.ReadLine(); // Read the bot token from console.
            }

            Console.WriteLine("Configuration Loaded...");
        }
    }
}