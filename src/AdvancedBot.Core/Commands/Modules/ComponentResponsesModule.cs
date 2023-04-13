using System.Threading.Tasks;
using AdvancedBot.Core.Entities.Enums;
using AdvancedBot.Core.Services;
using Discord;
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
            await DeferAsync();

            if (!PowerUsers.Contains(Context.User.Id))
            {
                await Context.Interaction.FollowupAsync($"Nice try bozo, what kind of loser calls themself {Context.User.Username} anyway", ephemeral: true);
                return;
            }

            await ModifyOriginalResponseAsync(x => x.Components = CreateModerationComponents(username, userId, alliance, isBanned));
        }

        [ComponentInteraction("back:*,*,*,*")]
        public async Task OnBackComponent(string username, string userId, string alliance, bool isBanned)
        {
            await DeferAsync();

            if (!PowerUsers.Contains(Context.User.Id))
            {
                await Context.Interaction.FollowupAsync($"Nice try bozo, what kind of loser calls themself {Context.User.Username} anyway", ephemeral: true);
                return;
            }

            await ModifyOriginalResponseAsync(x => x.Components = CreateDefaultComponents(username, userId, alliance, isBanned));
        }

        [ComponentInteraction("ban:*,*")]
        public async Task OnBanComponent(string username, string userId)
        {
            if (!PowerUsers.Contains(Context.User.Id))
            {
                await DeferAsync();
                await Context.Interaction.FollowupAsync($"Nice try bozo, what kind of loser calls themself {Context.User.Username} anyway", ephemeral: true);
                return;
            }

            await Context.Interaction.RespondWithModalAsync<BanModal>($"ban_menu:{username},{userId}", null, x => x.Title = $"Banning {username} ({userId})");
        }

        [ComponentInteraction("unban:*,*")]
        public async Task OnUnbanComponent(string username, string userId)
        {
            await DeferAsync();

            if (!PowerUsers.Contains(Context.User.Id))
            {
                await Context.Interaction.FollowupAsync($"Nice try bozo, what kind of loser calls themself {Context.User.Username} anyway", ephemeral: true);
                return;
            }

            var result = await ModService.UnbanUserAsync(Context.User.Id, uint.Parse(userId));

            switch (result.Type)
            {
                case ModResultType.Success:
                    var embed = new EmbedBuilder()
                    {
                        Title = $"{result.PhoenixUser.UserName} ({result.PhoenixUser.UserId}) is no longer banned in-game!",
                        Color = Color.Green
                    };

                    await FollowupAsync(embed: embed.Build());
                    var components = CreateDefaultComponents(username, userId, result.User?.AllianceId, false);
                    await ModifyOriginalResponseAsync(x => x.Components = components);
                    break;
                case ModResultType.AlreadyDone:
                    await FollowupAsync($"{result.PhoenixUser.UserName} ({result.PhoenixUser.UserId}) is not banned!");
                    break;
                case ModResultType.BackendError:
                    await FollowupAsync($"Failed to unban {username} ({userId}).", ephemeral: true);
                    break;
            }
        }

        [ModalInteraction("ban_menu:*,*")]
        public async Task BanModalResponse(string username, string userId, BanModal modal)
        {
            await DeferAsync();

            if (!PowerUsers.Contains(Context.User.Id))
            {
                await Context.Interaction.FollowupAsync($"Nice try bozo, what kind of loser calls themself {Context.User.Username} anyway", ephemeral: true);
            }

            if (string.IsNullOrEmpty(modal.BanReason))
            {
                await RespondAsync("Cannot ban without a valid ban reason", ephemeral: true);
                return;
            }

            var result = await ModService.BanUserAsync(Context.User.Id, uint.Parse(userId), modal.BanReason);

            switch (result.Type)
            {
                case ModResultType.Success:
                    var embed = new EmbedBuilder()
                    {
                        Title = $"{result.PhoenixUser.UserName} ({result.PhoenixUser.UserId}) is now banned in-game!",
                        Color = Color.Red
                    };

                    await FollowupAsync(embed: embed.Build());

                    var components = CreateDefaultComponents(username, userId, result.User?.AllianceId, true);
                    await ModifyOriginalResponseAsync(x => x.Components = components);
                    break;
                case ModResultType.AlreadyDone:
                    await FollowupAsync($"{result.PhoenixUser.UserName} ({result.PhoenixUser.UserId}) is already banned!");
                    break;
                case ModResultType.BackendError:
                    await FollowupAsync($"Failed to ban {username} ({userId}).", ephemeral: true);
                    break;
            }
        }
    }
}
