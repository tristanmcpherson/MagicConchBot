using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using MagicConchBot.Resources;
using Discord;

namespace MagicConchBot.Attributes
{
    public class RequireUserRoleAttribute : PreconditionAttribute
    {
        private readonly string _requiredRole;

        public RequireUserRoleAttribute(string requiredRole)
        {
            _requiredRole = requiredRole;
        }

        public override async Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IDependencyMap map)
        {
            //return PreconditionResult.FromSuccess();
            // Get the ID of the bot's owner
            // If this command was executed by that user, return a success
            var requiredRole = context.Guild.Roles.FirstOrDefault(r => r.Name == _requiredRole);
            var isOwner = Configuration.Load().Owners.Contains(context.User.Id);

            if (isOwner)
            {
                return PreconditionResult.FromSuccess();
            }

            if (requiredRole == null)
            {
                return PreconditionResult.FromError($"No role named 'Conch Control' exists.");
            }

            return (context.User as IGuildUser).RoleIds.Contains(requiredRole.Id) 
                ? PreconditionResult.FromSuccess() 
                : PreconditionResult.FromError($"You must have the role {_requiredRole} to run this command.");
        }
    }
}
