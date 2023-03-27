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

        public override Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
        {
            if (!_userIds.Contains(context.User.Id))
            {
                return Task.FromResult(PreconditionResult.FromError("You dont have permission to do this!"));
            }

            return Task.FromResult(PreconditionResult.FromSuccess());
        }
    }
}
