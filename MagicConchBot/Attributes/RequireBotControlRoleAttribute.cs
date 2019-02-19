using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using MagicConchBot.Resources;

namespace MagicConchBot.Attributes
{
    public class RequireBotControlRoleAttribute : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command,
            IServiceProvider map)
        {
            // return PreconditionResult.FromSuccess();
            // Get the ID of the bot's owner
            // If this command was executed by that user, return a success
            var config = Configuration.Load();
            var requiredRole = context.Guild.Roles.FirstOrDefault(r => r.Name == config.RequiredRole);
            var isBlacklist = config.Blacklist.Contains(context.User.Id);
            var isOwner = config.Owners.Contains(context.User.Id);

            if (isOwner)
                return PreconditionResult.FromSuccess();

            if (isBlacklist)
                return PreconditionResult.FromError("You are not allowed to use the bot.");

            if (context.Channel.Name != config.BotControlChannel)
                if (context.Guild.Id == config.OwnerGuildId)
                    return PreconditionResult.FromError(config.WrongChannelError);

            if (requiredRole == null)
                return PreconditionResult.FromSuccess();

            if (((IGuildUser) context.User).RoleIds.Contains(requiredRole.Id))
                return PreconditionResult.FromSuccess();

            return PreconditionResult.FromError($"You must have the role {config.RequiredRole} to run this command.");
        }
    }
}