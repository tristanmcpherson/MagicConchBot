using Discord;
using Discord.Commands;
using System.Threading.Tasks;
using System;

namespace MagicConchBot.Attributes
{
    public class RequireUserInVoiceChannelAttribute : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IDependencyMap map)
        {
            await Task.Delay(0);

            var channel = (context.User as IGuildUser)?.VoiceChannel;
            if (channel != null)
                return PreconditionResult.FromSuccess();
            return PreconditionResult.FromError("User must be in a voice channel.");
        }
    }
}
