using AdvancedBot.Core.Services;
using Discord.Interactions;
using System.Threading.Tasks;

namespace AdvancedBot.Core.Commands.Modules
{
    public class StagingComponentsModule : TopModule
    {
        public ModerationService ModService { get; set; }

        [ComponentInteraction("rules")]
        public async Task OnReloadRulesComponent()
        {
            if (!PowerUsers.Contains(Context.User.Id))
            {
                await RespondAsync($"Nice try bozo, what kind of loser calls themself {Context.User.Username} anyway", ephemeral: true);
                return;
            }

            var result = await ModService.ReloadRules(Context.User.Id);
            await SendResponseMessage(result.Message, true);
        }

        [ComponentInteraction("kicker")]
        public async Task OnKickerComponent()
        {
            if (!PowerUsers.Contains(Context.User.Id))
            {
                await RespondAsync($"Nice try bozo, what kind of loser calls themself {Context.User.Username} anyway", ephemeral: true);
                return;
            }

            var result = await ModService.RunStagingKickerAsync(Context.User.Id);
            await SendResponseMessage(result.Message, true);
        }

        [ComponentInteraction("helps")]
        public async Task OnResetHelpsComponent()
        {
            if (!PowerUsers.Contains(Context.User.Id))
            {
                await RespondAsync($"Nice try bozo, what kind of loser calls themself {Context.User.Username} anyway", ephemeral: true);
                return;
            }

            await Context.Interaction.RespondWithModalAsync<ResetHelpsModal>($"helps_menu");
        }

        [ComponentInteraction("startwar")]
        public async Task OnStartWarComponent()
        {
            if (!PowerUsers.Contains(Context.User.Id))
            {
                await RespondAsync($"Nice try bozo, what kind of loser calls themself {Context.User.Username} anyway", ephemeral: true);
                return;
            }

            await Context.Interaction.RespondWithModalAsync<WarModal>($"startwar_menu", null, x => x.Title = $"Starting A War");
        }

        [ComponentInteraction("endwar")]
        public async Task OnEndWarComponent()
        {
            if (!PowerUsers.Contains(Context.User.Id))
            {
                await RespondAsync($"Nice try bozo, what kind of loser calls themself {Context.User.Username} anyway", ephemeral: true);
                return;
            }

            await Context.Interaction.RespondWithModalAsync<WarModal>($"endwar_menu", null, x => x.Title = $"Ending A War");
        }

        [ModalInteraction("helps_menu")]
        public async Task ResetHelpsResponse(ResetHelpsModal modal)
        {
            if (!PowerUsers.Contains(Context.User.Id))
            {
                await RespondAsync($"Nice try bozo, what kind of loser calls themself {Context.User.Username} anyway", ephemeral: true);
                return;
            }

            await DeferAsync();

            var result = await ModService.ResetHelpsStagingAsync(Context.User.Id, uint.Parse(modal.UserId));
            await SendResponseMessage(result.Message, true);
        }

        [ModalInteraction("startwar_menu")]
        public async Task StartWarResponse(WarModal modal)
        {
            if (!PowerUsers.Contains(Context.User.Id))
            {
                await RespondAsync($"Nice try bozo, what kind of loser calls themself {Context.User.Username} anyway", ephemeral: true);
                return;
            }

            await DeferAsync();

            var result = await ModService.ForceWarStagingAsync(Context.User.Id, modal.AllianceA, modal.AllianceB);
            await SendResponseMessage(result.Message, true);
        }

        [ModalInteraction("endwar_menu")]
        public async Task EndWarResponse(WarModal modal)
        {
            if (!PowerUsers.Contains(Context.User.Id))
            {
                await RespondAsync($"Nice try bozo, what kind of loser calls themself {Context.User.Username} anyway", ephemeral: true);
                return;
            }

            await DeferAsync();

            var result = await ModService.ForceEndWarStagingAsync(Context.User.Id, modal.AllianceA, modal.AllianceB);
            await SendResponseMessage(result.Message, true);
        }
    }
}
