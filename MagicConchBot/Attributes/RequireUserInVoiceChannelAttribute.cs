using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace MagicConchBot.Attributes
{
    public class RequireUserInVoiceChannelAttribute : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IDependencyMap map)
        {
            await Task.Delay(0);

            var channel = (context.User as IGuildUser)?.VoiceChannel;
            return channel != null ? PreconditionResult.FromSuccess() : PreconditionResult.FromError("User must be in a voice channel.");
        }
    }
}
