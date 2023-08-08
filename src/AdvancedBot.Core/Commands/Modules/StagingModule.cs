using System.Threading.Tasks;
using AdvancedBot.Core.Commands.Preconditions;
using AdvancedBot.Core.Entities;
using AdvancedBot.Core.Services;
using Discord;
using Discord.Interactions;

namespace AdvancedBot.Core.Commands.Modules
{
    [Group("staging", "Handles all commands with the staging server")]
    [RequirePrivateList]
    [DontAutoRegister]
    public class StagingModule : TopModule
    {
        public ModerationService ModService { get; set; }

        [SlashCommand("overview", "Shows an overview of the staging server")]
        public async Task StagingOverviewAsync()
        {
            var embed = new EmbedBuilder()
            {
                Title = "Staging Overview",
                Description = "Not really any info to give yet :/",
                Color = Color.Blue
            };

            await SendResponseMessage(new ResponseMessage(embeds: new Embed[] { embed.Build() }), false);

            // add components
            var components = new ComponentBuilder();

            components.WithButton("Reload Rules", $"rules", ButtonStyle.Primary, Emote.Parse("<:Emoji_LETSGO:1081691478490894366>"));
            components.WithButton("Run Kicker", $"kicker", ButtonStyle.Primary, Emote.Parse("<:Emoji_Bonkself:1109938953475338340>"));
            components.WithButton("Reset helps", $"helps", ButtonStyle.Primary, Emote.Parse("<:Item_Helmet:1084821573975945267>"));

            components.WithButton("Start War", $"startwar", ButtonStyle.Danger, new Emoji("⚔"), row: 1);
            components.WithButton("End War", $"endwar", ButtonStyle.Danger, Emote.Parse("<:jijijijamikaze:948038940819075092>"), row: 1);

            await ModifyOriginalResponseAsync(x => x.Components = components.Build());
        }

        [SlashCommand("reloadrules", "Reload staging backend rules")]
        public async Task ReloadRulesAsync()
        {
            var result = await ModService.ReloadRules(Context.User.Id, true);
            await SendResponseMessage(result.Message, false);
        }

        [SlashCommand("kicker", "Runs the kicker to kick everyone offline")]
        public async Task RunKickerAsync()
        {
            var result = await ModService.RunStagingKickerAsync(Context.User.Id);
            await SendResponseMessage(result.Message, false);
        }

        [SlashCommand("resethelps", "Reset visit helps for someone")]
        public async Task ResetHelpsAsync(uint userId)
        {
            var result = await ModService.ResetHelpsStagingAsync(Context.User.Id, userId);
            await SendResponseMessage(result.Message, false);
        }

        [SlashCommand("forcewar", "Force start a war between 2 alliances")]
        public async Task ForceWarAsync(string allianceA, string allianceB)
        {
            var result = await ModService.ForceWarStagingAsync(Context.User.Id, allianceA, allianceB);
            await SendResponseMessage(result.Message, false);
        }

        [SlashCommand("forcestopwar", "Force end a war between 2 alliances")]
        public async Task ForceStopWarAsync(string allianceA, string allianceB)
        {
            var result = await ModService.ForceEndWarStagingAsync(Context.User.Id, allianceA, allianceB);
            await SendResponseMessage(result.Message, false);
        }
    }
}
