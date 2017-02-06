using System.Reflection;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using MagicConchBot.Services;
using MagicConchBot.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace MagicConchBot.Handlers
{
    public class CommandHandler
    {
        private CmdSrv _commands;
        private DiscordSocketClient _client;
        private IDependencyMap _map;

        public void ConfigureServices(IDependencyMap map)
        {
            map.Add(new MusicServiceProvider());
            map.Add(new GoogleApiService());
            map.Add(new YouTubeInfoService(map));
            map.Add(new SoundCloudInfoService());
        }

        public async Task InstallAsync(IDependencyMap map)
        {
            // Create Command Service, inject it into Dependency Map
            _client = map.Get<DiscordSocketClient>();
            _commands = new CmdSrv();
            _map = map;

            _map.Add(_commands);

            await _commands.AddModulesAsync(Assembly.GetEntryAssembly());

            _client.MessageReceived += HandleCommandAsync;
            _client.GuildAvailable += HandleGuildAvailable;
            _client.JoinedGuild += HandleJoinedGuildAsync;
        }

        private async Task HandleJoinedGuildAsync(SocketGuild arg)
        {
            await arg.DefaultChannel.SendMessageAsync($"All hail the Magic Conch. In order to use the Music functions of this bot, please create a role named '{Constants.RequiredRoleName}' and add that role to the users whom you want to be able to control the Music functions of this bot. Type !help for help.");
        }

        private Task HandleGuildAvailable(SocketGuild guild)
        {
            _map.Get<MusicServiceProvider>().AddService(guild.Id, new FfmpegMusicService());
            return Task.CompletedTask;
        }

        public async Task HandleCommandAsync(SocketMessage parameterMessage)
        {
            // Don't handle the command if it is a system message
            var message = parameterMessage as SocketUserMessage;
            if (message == null) return;

            // Mark where the prefix ends and the command begins
            var argPos = 0;
            // Determine if the message has a valid prefix, adjust argPos 
            if (!(message.HasMentionPrefix(_client.CurrentUser, ref argPos) || message.HasCharPrefix('!', ref argPos))) return;

            // Create a Command Context
            var context = new CommandContext(_client, message);
            // Execute the Command, store the result
            var result = await _commands.ExecuteAsync(context, argPos, _map, MultiMatchHandling.Best);

            // If the command failed, notify the user
            if (!result.IsSuccess)
                await message.Channel.SendMessageAsync($"**Error:** {result.ErrorReason}");
        }
    }
    public class CmdSrv : CommandService
    {
        public new Task<IResult> ExecuteAsync(ICommandContext context, int argPos, IDependencyMap dependencyMap = null, MultiMatchHandling multiMatchHandling = MultiMatchHandling.Exception)
            => ExecuteAsync(context, context.Message.Content.Substring(argPos), dependencyMap, multiMatchHandling);
        public async new Task<IResult> ExecuteAsync(ICommandContext context, string input, IDependencyMap dependencyMap = null, MultiMatchHandling multiMatchHandling = MultiMatchHandling.Exception)
        {
            dependencyMap = dependencyMap ?? DependencyMap.Empty;

            var searchResult = Search(context, input);
            if (!searchResult.IsSuccess)
                return searchResult;

            var commands = searchResult.Commands;
            for (int i = commands.Count - 1; i >= 0; i--)
            {
                var preconditionResult = await commands[i].CheckPreconditionsAsync(context, dependencyMap).ConfigureAwait(false);
                if (!preconditionResult.IsSuccess)
                {
                    if (i == 1)
                        return preconditionResult;
                    else
                        continue;
                }

                var parseResult = await commands[i].ParseAsync(context, searchResult, preconditionResult).ConfigureAwait(false);
                if (!parseResult.IsSuccess)
                {
                    if (parseResult.Error == CommandError.MultipleMatches)
                    {
                        IReadOnlyList<TypeReaderValue> argList, paramList;
                        switch (multiMatchHandling)
                        {
                            case MultiMatchHandling.Best:
                                argList = parseResult.ArgValues.Select(x => x.Values.OrderByDescending(y => y.Score).First()).ToImmutableArray();
                                paramList = parseResult.ParamValues.Select(x => x.Values.OrderByDescending(y => y.Score).First()).ToImmutableArray();
                                parseResult = ParseResult.FromSuccess(argList, paramList);
                                break;
                        }
                    }

                    if (!parseResult.IsSuccess)
                    {
                        if (i == 1)
                            return parseResult;
                        else
                            continue;
                    }
                }

                return await commands[i].ExecuteAsync(context, parseResult, dependencyMap).ConfigureAwait(false);
            }

            return SearchResult.FromError(CommandError.UnknownCommand, "This input does not match any overload.");
        }
    }
}