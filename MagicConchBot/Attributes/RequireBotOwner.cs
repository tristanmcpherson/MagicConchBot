using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using MagicConchBotApp.Resources;

namespace MagicConchBotApp.Attributes
{
    public class RequireBotOwnerAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider map)
        {
			return Task.FromResult(Configuration.Load().Owners.Contains(context.User.Id) ? PreconditionResult.FromSuccess() : PreconditionResult.FromError("This feature is only usable by admins. "));
        }
	}
}
