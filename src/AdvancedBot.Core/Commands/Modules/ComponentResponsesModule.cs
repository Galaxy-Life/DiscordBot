using System.Threading.Tasks;
using AdvancedBot.Core.Services;
using Discord.Interactions;

namespace AdvancedBot.Core.Commands.Modules
{
    public class ComponentResponsesModule : TopModule
    {
        public ModerationService ModService { get; set; }
        public GLService GLService { get; set; }

        [ComponentInteraction("profile:*,*")]
        public async Task OnProfileComponent(string username, string userId)
        {
            await DeferAsync();
            var response = await GLService.GetUserProfileAsync(userId);

            await SendResponseMessage(response.Message, false);
        }

        [ComponentInteraction("stats:*,*")]
        public async Task OnStatsComponent(string username, string userId)
        {
            await DeferAsync();
            var response = await GLService.GetUserStatsAsync(userId);

            await SendResponseMessage(response.Message, false);
        }

        [ComponentInteraction("alliance:*")]
        public async Task OnAllianceComponent(string alliance)
        {
            await DeferAsync();

            var response = await GLService.GetAllianceAsync(alliance);

            await SendResponseMessage(response.Message, false);
        }

        [ComponentInteraction("members:*")]
        public async Task OnMembersComponent(string alliance)
        {
            await DeferAsync();

            var response = await GLService.GetAllianceMembersAsync(alliance);

            await SendResponseMessage(response.Message, false);
        }

        [ComponentInteraction("moderation:*,*,*,*")]
        public async Task OnModerationComponent(string username, string userId, string alliance, bool isBanned)
        {
            if (!PowerUsers.Contains(Context.User.Id))
            {
                await RespondAsync($"Nice try bozo, what kind of loser calls themself {Context.User.Username} anyway", ephemeral: true);
                return;
            }

            await DeferAsync();
            await ModifyOriginalResponseAsync(x => x.Components = CreateModerationComponents(username, userId, alliance, isBanned));
        }

        [ComponentInteraction("back:*,*,*,*")]
        public async Task OnBackComponent(string username, string userId, string alliance, bool isBanned)
        {
            if (!PowerUsers.Contains(Context.User.Id))
            {
                await RespondAsync($"Nice try bozo, what kind of loser calls themself {Context.User.Username} anyway", ephemeral: true);
                return;
            }

            await DeferAsync();
            await ModifyOriginalResponseAsync(x => x.Components = CreateDefaultComponents(username, userId, alliance, isBanned));
        }

        [ComponentInteraction("ban:*,*")]
        public async Task OnBanComponent(string username, string userId)
        {
            if (!PowerUsers.Contains(Context.User.Id))
            {
                await RespondAsync($"Nice try bozo, what kind of loser calls themself {Context.User.Username} anyway", ephemeral: true);
                return;
            }

            await Context.Interaction.RespondWithModalAsync<BanModal>($"ban_menu:{userId}", null, x => x.Title = $"Banning {username} ({userId})");
        }

        [ComponentInteraction("unban:*")]
        public async Task OnUnbanComponent(string userId)
        {
            if (!PowerUsers.Contains(Context.User.Id))
            {
                await RespondAsync($"Nice try bozo, what kind of loser calls themself {Context.User.Username} anyway", ephemeral: true);
                return;
            }

            await DeferAsync();
            var result = await ModService.UnbanUserAsync(Context.User.Id, uint.Parse(userId));
            await SendResponseMessage(result.Message, true);
        }

        [ComponentInteraction("addbeta:*")]
        public async Task OnAddBetaComponent(string userId)
        {
            if (!PowerUsers.Contains(Context.User.Id))
            {
                await RespondAsync($"Nice try bozo, what kind of loser calls themself {Context.User.Username} anyway", ephemeral: true);
                return;
            }

            await DeferAsync();
            var result = await ModService.AddBetaToUserAsync(Context.User.Id, uint.Parse(userId));
            await SendResponseMessage(result.Message, true);
        }

        [ComponentInteraction("removebeta:*")]
        public async Task OnRemoveBetaComponent(string userId)
        {
            if (!PowerUsers.Contains(Context.User.Id))
            {
                await RespondAsync($"Nice try bozo, what kind of loser calls themself {Context.User.Username} anyway", ephemeral: true);
                return;
            }

            await DeferAsync();
            var result = await ModService.RemoveBetaFromUserAsync(Context.User.Id, uint.Parse(userId));
            await SendResponseMessage(result.Message, true);
        }

        [ComponentInteraction("giverole:*")]
        public async Task OnGiveRoleComponent(string userId)
        {
            if (!PowerUsers.Contains(Context.User.Id))
            {
                await RespondAsync($"Nice try bozo, what kind of loser calls themself {Context.User.Username} anyway", ephemeral: true);
                return;
            }

            await DeferAsync(true);
            await FollowupAsync("implement", ephemeral: true);
        }

        [ComponentInteraction("chipsbought:*")]
        public async Task OnChipsBoughtComponent(string userId)
        {
            if (!PowerUsers.Contains(Context.User.Id))
            {
                await RespondAsync($"Nice try bozo, what kind of loser calls themself {Context.User.Username} anyway", ephemeral: true);
                return;
            }

            await DeferAsync(true);
            var result = await ModService.GetChipsBoughtAsync(Context.User.Id, uint.Parse(userId));

            result.Message.Ephemeral = true;
            await SendResponseMessage(result.Message, true);
        }

        [ComponentInteraction("addchips:*,*")]
        public async Task OnAddChipsComponent(string username, string userId)
        {
            if (!PowerUsers.Contains(Context.User.Id))
            {
                await RespondAsync($"Nice try bozo, what kind of loser calls themself {Context.User.Username} anyway", ephemeral: true);
                return;
            }

            await Context.Interaction.RespondWithModalAsync<AddChipsModal>($"addchips_menu:{userId}", null, x => x.Title = $"Adding chips to {username} ({userId})");
        }

        [ComponentInteraction("additem:*,*")]
        public async Task OnAddItemComponent(string username, string userId)
        {
            if (!PowerUsers.Contains(Context.User.Id))
            {
                await RespondAsync($"Nice try bozo, what kind of loser calls themself {Context.User.Username} anyway", ephemeral: true);
                return;
            }

            await Context.Interaction.RespondWithModalAsync<AddItemModal>($"additem_menu:{userId}", null, x => x.Title = $"Adding item(s) to {username} ({userId})");
        }

        [ModalInteraction("ban_menu:*")]
        public async Task BanModalResponse(string userId, BanModal modal)
        {
            if (!PowerUsers.Contains(Context.User.Id))
            {
                await RespondAsync($"Nice try bozo, what kind of loser calls themself {Context.User.Username} anyway", ephemeral: true);
                return;
            }

            if (string.IsNullOrEmpty(modal.BanReason))
            {
                await RespondAsync("Cannot ban without a valid ban reason", ephemeral: true);
                return;
            }

            await DeferAsync();

            var result = await ModService.BanUserAsync(Context.User.Id, uint.Parse(userId), modal.BanReason);
            await SendResponseMessage(result.Message, true);
        }

        [ModalInteraction("addchips_menu:*")]
        public async Task AddChipsModalResponse(string userId, AddChipsModal modal)
        {
            if (!PowerUsers.Contains(Context.User.Id))
            {
                await RespondAsync($"Nice try bozo, what kind of loser calls themself {Context.User.Username} anyway", ephemeral: true);
                return;
            }

            if (string.IsNullOrEmpty(modal.Amount))
            {
                await RespondAsync("Invalid amount", ephemeral: true);
                return;
            }

            await DeferAsync(true);
            var result = await ModService.AddChipsAsync(Context.User.Id, uint.Parse(userId), modal.ActualAmount);

            result.Message.Ephemeral = true;
            await SendResponseMessage(result.Message, true);
        }

        [ModalInteraction("additem_menu:*")]
        public async Task AddItemModalResponse(string userId, AddItemModal modal)
        {
            if (!PowerUsers.Contains(Context.User.Id))
            {
                await RespondAsync($"Nice try bozo, what kind of loser calls themself {Context.User.Username} anyway", ephemeral: true);
                return;
            }

            if (string.IsNullOrEmpty(modal.Sku))
            {
                await RespondAsync("Invalid sku", ephemeral: true);
                return;
            }

            if (string.IsNullOrEmpty(modal.Amount))
            {
                await RespondAsync("Invalid amount", ephemeral: true);
                return;
            }

            await DeferAsync(true);
            var result = await ModService.AddItemsAsync(Context.User.Id, uint.Parse(userId), modal.Sku, modal.ActualAmount);

            result.Message.Ephemeral = true;
            await SendResponseMessage(result.Message, true);
        }
    }
}
