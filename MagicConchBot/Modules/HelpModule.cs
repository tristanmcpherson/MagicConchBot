using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using MagicConchBot.Resources;

namespace MagicConchBot.Modules
{
    [Group("Help")]
    [Name("Help Commands")]
    public class HelpModule : ModuleBase
    {
        private readonly CommandService _service;

        public HelpModule(CommandService service)
        {
            // Create a constructor for the commandservice dependency
            _service = service;
        }

        [NoHelp]
        [Command]
        public async Task HelpAsync()
        {
            var prefix = Configuration.Prefix;
            var builder = new EmbedBuilder
            {
                Color = Constants.MaterialBlue,
                Description = "These are the commands you can use"
            };

            foreach (var module in _service.Modules)
            {
                if (module.Commands.Any(n => n.Name == nameof(HelpAsync)))
                    continue;
                

                string description = null;
                string last = null;
                foreach (var cmd in module.Commands)
                {
                    // var result = await cmd.CheckPreconditionsAsync(Context);
                    // if (result.IsSuccess)
                    //if (cmd.Name)

                    var alias = cmd.Aliases.First();
                    if (last == alias)
                        continue;

                    last = alias;
                    description += $"{prefix}{alias}" + (cmd.Parameters.Count > 0 ? " ..." : string.Empty) + "\n";
                }

                if (!string.IsNullOrWhiteSpace(description))
                    builder.AddField(x =>
                    {
                        x.Name = module.Name;
                        x.Value = description;
                        x.IsInline = false;
                    });
            }

            await ReplyAsync(string.Empty, false, builder.Build());
        }

        [NoHelp]
        [Command]
        public async Task HelpAsync([Summary("The command")] string command)
        {
            var result = _service.Search(Context, command);

            if (!result.IsSuccess)
            {
                await ReplyAsync($"Sorry, I couldn't find a command like **{command}**.");
                return;
            }

            var builder = new EmbedBuilder
            {
                Color = Constants.MaterialBlue,
                Description = $"**{command}**:"
            };

            foreach (var match in result.Commands)
            {
                var cmd = match.Command;

                builder.AddField(x =>
                {
                    x.Name = string.Join(", ", cmd.Aliases) + (cmd.Parameters.Count == 0 ? string.Empty : " ...");
                    x.Value = (cmd.Parameters.Count == 0
                                  ? string.Empty
                                  : $"**Parameters:** {string.Join(", ", cmd.Parameters.Select(p => p.Name + " - " + p.Summary))}\n") +
                              $"**Summary:** {cmd.Summary}";
                    x.IsInline = false;
                });
            }

            await ReplyAsync(string.Empty, false, builder.Build());
        }
    }

    public class NoHelpAttribute : Attribute
    {
    }
}