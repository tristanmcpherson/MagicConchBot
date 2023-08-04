using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using MagicConchBot.Resources;

namespace MagicConchBot.Attributes
{
    public class RequireBotOwnerAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
        {
            return Task.FromResult(
                Configuration.Owners.Contains(context.User.Id)
                ? PreconditionResult.FromSuccess()
                : PreconditionResult.FromError("This feature is only usable by admins. ")
            );
        }
    }
}
