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
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command,
            IServiceProvider map)
        {

            if (Configuration.Load().Owners.Contains(context.User.Id))
                return Task.FromResult(PreconditionResult.FromSuccess());

            var channel = (context.User as IGuildUser)?.VoiceChannel;
            return channel != null
                ? Task.FromResult(PreconditionResult.FromSuccess())
                : Task.FromResult(PreconditionResult.FromError("User must be in a voice channel."));
        }
    }
}