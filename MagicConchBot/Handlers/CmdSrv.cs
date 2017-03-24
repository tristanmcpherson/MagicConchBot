using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;

namespace MagicConchBot.Handlers
{
    public class CmdSrv : CommandService
    {
        public new Task<IResult> ExecuteAsync(ICommandContext context, int argPos, IDependencyMap dependencyMap = null,
            MultiMatchHandling multiMatchHandling = MultiMatchHandling.Exception)
            => ExecuteAsync(context, context.Message.Content.Substring(argPos), dependencyMap, multiMatchHandling);

        public new async Task<IResult> ExecuteAsync(ICommandContext context, string input,
            IDependencyMap dependencyMap = null, MultiMatchHandling multiMatchHandling = MultiMatchHandling.Exception)
        {
            dependencyMap = dependencyMap ?? DependencyMap.Empty;

            var searchResult = Search(context, input);
            if (!searchResult.IsSuccess)
                return searchResult;

            var commands = searchResult.Commands;
            for (var i = commands.Count - 1; i >= 0; i--)
            {
                var preconditionResult =
                    await commands[i].CheckPreconditionsAsync(context, dependencyMap).ConfigureAwait(false);
                if (!preconditionResult.IsSuccess)
                {
                    if (i == 0)
                        return preconditionResult;

                    continue;
                }

                var parseResult =
                    await commands[i].ParseAsync(context, searchResult, preconditionResult).ConfigureAwait(false);
                if (!parseResult.IsSuccess)
                {
                    if (parseResult.Error == CommandError.MultipleMatches)
                        switch (multiMatchHandling)
                        {
                            case MultiMatchHandling.Best:
                                IReadOnlyList<TypeReaderValue> argList =
                                    parseResult.ArgValues.Select(x => x.Values.OrderByDescending(y => y.Score).First())
                                        .ToImmutableArray();
                                IReadOnlyList<TypeReaderValue> paramList =
                                    parseResult.ParamValues.Select(x => x.Values.OrderByDescending(y => y.Score).First())
                                        .ToImmutableArray();
                                parseResult = ParseResult.FromSuccess(argList, paramList);
                                break;
                        }

                    if (!parseResult.IsSuccess)
                    {
                        if (i == 0)
                            return parseResult;

                        continue;
                    }
                }

                return await commands[i].ExecuteAsync(context, parseResult, dependencyMap).ConfigureAwait(false);
            }

            return SearchResult.FromError(CommandError.UnknownCommand, "This input does not match any overload.");
        }
    }
}