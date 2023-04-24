using System.Threading.Tasks;
using AdvancedBot.Core.Commands.Preconditions;
using AdvancedBot.Core.Services;
using Discord.Interactions;

namespace AdvancedBot.Core.Commands.Modules
{
    [Group("staging", "Handles all commands with the staging server")]
    [RequirePrivateList]
    [DontAutoRegister]
    public class StagingModule : TopModule
    {
        public ModerationService ModService { get; set; }

        [SlashCommand("reloadrules", "Reload staging backend rules")]
        public async Task ReloadRulesAsync()
        {
            var result = await ModService.ReloadRules(Context.User.Id, true);
            await SendResponseMessage(result.Message, false);
        }
    }
}
