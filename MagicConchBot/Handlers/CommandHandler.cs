namespace MagicConchBot.Handlers
{
    using System.Reflection;
    using System.Threading.Tasks;

    using Discord.Commands;
    using Discord.WebSocket;

    using log4net;

    using MagicConchBot.Modules;
    using MagicConchBot.Resources;
    using MagicConchBot.Services;

    public class CommandHandler
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(CommandHandler));

        private CmdSrv commands;
        private DiscordSocketClient client;
        private IDependencyMap map;

        public void ConfigureServices(IDependencyMap depMap)
        {
            map = depMap;
            map.Add(new GoogleApiService());
            map.Add(new YouTubeInfoService(map));
            map.Add(new SoundCloudInfoService());
            map.Add(new ChanService());
        }

        public async Task InstallAsync()
        {
            // Create Command Service, inject it into Dependency Map
            client = map.Get<DiscordSocketClient>();
            commands = new CmdSrv();

            map.Add(commands);

            await commands.AddModulesAsync(Assembly.GetEntryAssembly());

            client.MessageReceived += HandleCommandAsync;
            client.GuildAvailable += HandleGuildAvailable;
            client.JoinedGuild += HandleJoinedGuildAsync;
            client.MessageReceived += HandleMessageReceivedAsync;
        }

        public async Task HandleCommandAsync(SocketMessage parameterMessage)
        {
            // Don't handle the command if it is a system message
            var message = parameterMessage as SocketUserMessage;
            if (message == null)
            {
                return;
            }

            // Mark where the prefix ends and the command begins
            var argPos = 0;

            // Determine if the message has a valid prefix, adjust argPos 
            if (!(message.HasMentionPrefix(client.CurrentUser, ref argPos) || message.HasCharPrefix('!', ref argPos)))
            {
                return;
            }

            // Create a Command Context
            var context = new MusicCommandContext(client, message);

            // Execute the Command, store the result
            var result = await commands.ExecuteAsync(context, argPos, map, MultiMatchHandling.Best);

            // If the command failed, notify the user
            if (!result.IsSuccess)
            {
                if (result.ErrorReason == Configuration.Load().WrongChannelError)
                {
                    await message.Channel.SendMessageAsync($"{result.ErrorReason}", true);
                }
                else
                {
                    await message.Channel.SendMessageAsync($"**Error:** {result.ErrorReason}");
                }
            }
        }

        private async Task HandleMessageReceivedAsync(SocketMessage arg)
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
        }

        private Task HandleGuildAvailable(SocketGuild guild)
        {
            MusicServiceProvider.AddService(guild.Id, new FfmpegMusicService());
            return Task.CompletedTask;
        }
    }
}