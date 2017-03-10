namespace MagicConchBot
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Discord;
    using Discord.Audio;
    using Discord.Commands;
    using Discord.WebSocket;

    using log4net;

    using MagicConchBot.Handlers;
    using MagicConchBot.Resources;
    using MagicConchBot.Services;

    public class Program
    {
        // https://discordapp.com/oauth2/authorize?client_id=267000484420780045&scope=bot&permissions=540048384
        private static readonly ILog Log = LogManager.GetLogger(typeof(Program));

        private static DiscordSocketClient client;
        private static CommandHandler handler;

        public static void Main()
        {
            Console.WriteLine("Starting Magic Conch Bot. Press 'q' at any time to quit.");
            var cts = new CancellationTokenSource();

            try
            {
                var task = Task.Factory.StartNew(async () => await MainAsync(cts.Token), cts.Token);

                while (true)
                {
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(true).Key;
                        if (key == ConsoleKey.Q)
                        {
                            cts.Cancel();
                            break;
                        }

                        // Skip song
                        if (key == ConsoleKey.S)
                        {
                            var config = Configuration.Load();
                            var serverId = config.OwnerGuildId;
                            Console.WriteLine("Skipping song.");
                            var channel = (IMessageChannel)client.GetGuild(serverId).Channels.First(c => c.Name == config.BotControlChannel);
                            if (MusicServiceProvider.GetService(serverId).Skip())
                            {
                                channel.SendMessageAsync("Skipping song at request of owner.");
                            }
                            else
                            {
                                Console.WriteLine("No song to skip.");
                            }
                        }
                    }

                    Thread.Sleep(250);
                }

                Task.WaitAll(task);
                Thread.Sleep(500);
            }
            finally
            {
                Console.WriteLine("Press enter to continue . . .");
                Console.ReadLine();
            }
        }

        private static async Task MainAsync(CancellationToken cancellationToken)
        {
            EnsureConfigExists();

            var map = new DependencyMap();

            try
            {
                handler = new CommandHandler();
                handler.ConfigureServices(map);

                client = new DiscordSocketClient(new DiscordSocketConfig
                {
                    LogLevel = LogSeverity.Info,
                    AudioMode = AudioMode.Outgoing
                });

                client.Log += WriteToLog;

                // Login and connect to Discord.
                map.Add(client);

                await handler.InstallAsync().ConfigureAwait(false);

                //Configuration.Load().Token
                await client.LoginAsync(TokenType.Bot, Configuration.Load().Token);
                await client.ConnectAsync().ConfigureAwait(false);

                await Task.Delay(-1, cancellationToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception ex)
            {
                await WriteToLog(new LogMessage(LogSeverity.Critical, "", "", ex));
            }
            finally
            {
                MusicServiceProvider.StopAll();
                client.DisconnectAsync();
            }
        }

        private static Task WriteToLog(LogMessage message)
        {
            if (message.Message.Contains("Unknown OpCode"))
            {
                return Task.CompletedTask;
            }

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
            
            // Check if the configuration file exists.
            if (!File.Exists(loc))                              
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