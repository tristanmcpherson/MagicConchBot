using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using MagicConchBot.Resources;

namespace MagicConchBot.Attributes
{
    public class RequireUserInVoiceChannelAttribute : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command,
            IServiceProvider map)
        {
            await Task.Delay(0);

            if (Configuration.Load().Owners.Contains(context.User.Id))
                return PreconditionResult.FromSuccess();

            var channel = (context.User as IGuildUser)?.VoiceChannel;
            return channel != null
                ? PreconditionResult.FromSuccess()
                : PreconditionResult.FromError("User must be in a voice channel.");
        }
    }
}