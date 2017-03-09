namespace MagicConchBot.Attributes
{
    using System.Linq;
    using System.Threading.Tasks;

    using Discord;
    using Discord.Commands;

    using MagicConchBot.Resources;

    public class RequireBotControlRoleAttribute : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IDependencyMap map)
        {
            //return PreconditionResult.FromSuccess();
            // Get the ID of the bot's owner
            // If this command was executed by that user, return a success
            var config = Configuration.Load();
            var requiredRole = context.Guild.Roles.FirstOrDefault(r => r.Name == config.RequiredRole);
            var isOwner = config.Owners.Contains(context.User.Id);

            if (context.Channel.Name != config.BotControlChannel)
            {
                if (context.Guild.Id == config.OwnerGuildId)
                {
                    return PreconditionResult.FromError(config.WrongChannelError);
                }
            }

            if (isOwner)
            {
                return PreconditionResult.FromSuccess();
            }

            if (requiredRole == null)
            {
                return PreconditionResult.FromError($"No role named 'Conch Control' exists.");
            }

            if (((IGuildUser)context.User).RoleIds.Contains(requiredRole.Id))
            {
                return PreconditionResult.FromSuccess();
            }

            return PreconditionResult.FromError($"You must have the role {config.RequiredRole} to run this command.");
        }
    }
}
