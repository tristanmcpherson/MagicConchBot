using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;

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
            // Get the ID of the bot's owner
            // If this command was executed by that user, return a success
            var requiredRole = context.Guild.Roles.FirstOrDefault(r => r.Name == _requiredRole);

            return requiredRole != null && (await context.Guild.GetUserAsync(context.User.Id)).RoleIds.Contains(requiredRole.Id) 
                ? PreconditionResult.FromSuccess() 
                : PreconditionResult.FromError($"You must have the role {_requiredRole} to run this command.");
        }
    }
}
