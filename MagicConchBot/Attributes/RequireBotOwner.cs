using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using MagicConchBot.Resources;

namespace MagicConchBot.Attributes
{
    public class RequireBotOwnerAttribute : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IDependencyMap map)
        {
            return Configuration.Load().Owners.Contains(context.User.Id) ? PreconditionResult.FromSuccess() : PreconditionResult.FromError("");
        }
    }
}
