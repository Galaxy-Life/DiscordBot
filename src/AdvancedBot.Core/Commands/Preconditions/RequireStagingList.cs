using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;

namespace AdvancedBot.Core.Commands.Preconditions
{
    public class RequireStagingList : PreconditionAttribute
    {
        private List<ulong> _userIds = new List<ulong>() { 202095042372829184, 942849642931032164, 209801906237865984, 180676108088246272, 356060824223350784, 275698828974489612, 240402743443980288 };

        public override async Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
        {
            if (!_userIds.Contains(context.User.Id))
            {
                if (!context.Interaction.HasResponded)
                {
                    await context.Interaction.DeferAsync();
                }

                return PreconditionResult.FromError($"{context.User.Username} has no permission to execute this command!");
            }

            return PreconditionResult.FromSuccess();
        }
    }
}
