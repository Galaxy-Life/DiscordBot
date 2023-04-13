using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;

namespace AdvancedBot.Core.Commands.Preconditions
{
    public class RequirePrivateList : PreconditionAttribute
    {
        private List<ulong> _userIds = new List<ulong>() { 202095042372829184, 942849642931032164, 209801906237865984, 362271714702262273 };

        public override async Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
        {
            if (!_userIds.Contains(context.User.Id))
            {
                if (!context.Interaction.HasResponded)
                {
                    await context.Interaction.DeferAsync();
                }

                await context.Interaction.FollowupAsync($"Nice try bozo, what kind of loser calls themself {context.User.Username} anyway", ephemeral: true);
                return PreconditionResult.FromError($"{context.User.Username} has no permission to execute this command!");
            }

            return PreconditionResult.FromSuccess();
        }
    }
}
