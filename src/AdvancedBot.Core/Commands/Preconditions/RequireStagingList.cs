using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;

namespace AdvancedBot.Core.Commands.Preconditions
{
    public class RequireStagingList : PreconditionAttribute
    {
        private readonly List<ulong> userIds = new()
        {
            202095042372829184, // svr333
            942849642931032164, // lifecoder
            180676108088246272, // lodethebig
            356060824223350784, // andyvv.
            275698828974489612, // magniolya 
            240402743443980288, // supercrocman
            424689465450037278  // bryan
        };

        public override async Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
        {
            if (!userIds.Contains(context.User.Id))
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
