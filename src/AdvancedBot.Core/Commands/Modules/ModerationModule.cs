using System.Threading.Tasks;
using AdvancedBot.Core.Commands.Preconditions;
using AdvancedBot.Core.Entities.Enums;
using AdvancedBot.Core.Services;
using Discord;
using Discord.Interactions;
using GL.NET;
using GL.NET.Entities;

namespace AdvancedBot.Core.Commands.Modules
{
    [RequirePrivateList]
    [DontAutoRegister]
    [Group("game", "All commands handling in-game actions")]
    public class ModerationModule : TopModule
    {
        [SlashCommand("getfull", "Get full information about a user")]
        public async Task GetFullUserAsync(string input)
        {
            var user = await GetPhoenixUserByInput(input, true) as FullPhoenixUser;

            if (user == null)
            {
                await ModifyOriginalResponseAsync(x => x.Content = $"<:shrugR:945740284308893696> No user found for **{input}**");
                return;
            }

            var steamId = user.SteamId ?? "No steam linked";
            var roleText = user.Role == PhoenixRole.Banned ? $"**This user has been banned!!**\nBan Reason: **{user.BanReason}**\n\n"
                : user.Role == PhoenixRole.Donator ? "This user is a Donator\n\n"
                : user.Role == PhoenixRole.Staff ? "This user is a Staff Member\n\n"
                : user.Role == PhoenixRole.Administrator ? "This user is an Admin\n\n"
                : "";

            var discordText = string.IsNullOrEmpty(user.DiscordId) ? "" : $"\nDiscord: <@{user.DiscordId}> **({user.DiscordId})**";

            var color = user.Role == PhoenixRole.Banned ? Color.Default
                : user.Role == PhoenixRole.Donator ? new Color(15710778)
                : user.Role == PhoenixRole.Staff ? new Color(2605694)
                : user.Role == PhoenixRole.Administrator ? Color.DarkRed
                : Color.LightGrey;

            var embed = new EmbedBuilder()
                .WithTitle($"Profile of {user.UserName}")
                .WithDescription($"{roleText}Id: **{user.UserId}**\nSteam Id: **{steamId.Replace("\"", "")}** "
                    + $"{discordText}\n\nEmail: **{user.Email}**")
                .WithColor(color)
                .WithFooter($"Account created on {user.Created.GetValueOrDefault().ToString("dd MMMM yyyy a\\t HH:mm")}");

            if (user.SteamId != null)
            {
                embed.WithUrl($"https://steamcommunity.com/profiles/{steamId.Replace("\"", "")}");
            }

            await ModifyOriginalResponseAsync(x => x.Embed = embed.Build());
        }

        [SlashCommand("ban", "Tries to ban a user")]
        public async Task TryBanUserAsync(uint userId, string reason)
        {
            var user = await GLClient.GetPhoenixUserAsync(userId);

            if (user.Role == PhoenixRole.Banned)
            {
                await ModifyOriginalResponseAsync(x => x.Content = $"{user.UserName} ({user.UserId}) is already banned!");
                return;
            }

            if (!await GLClient.TryBanUser(userId, reason))
            {
                await ModifyOriginalResponseAsync(x => x.Content = $"Failed to ban {user.UserName} ({user.UserId}).");
                return;
            }

            LogService.LogGameAction(LogAction.Ban, Context.User.Id, userId, reason);

            var embed = new EmbedBuilder()
            {
                Title = $"{user.UserName} ({user.UserId}) is now banned in-game!",
                Color = Color.Red
            };

            await ModifyOriginalResponseAsync(x => x.Embed = embed.Build() );
        }

        [SlashCommand("unban", "Tries to unban a user")]
        public async Task TryUnbanUserAsync(uint userId)
        {
            var user = await GLClient.GetPhoenixUserAsync(userId);

            if (user.Role != PhoenixRole.Banned)
            {
                await ModifyOriginalResponseAsync(x => x.Content = $"{user.UserName} ({user.UserId}) is not banned!");
                return;
            }

            if (!await GLClient.TryUnbanUser(userId))
            {
                await ModifyOriginalResponseAsync(x => x.Content = $"Failed to unban {user.UserName} ({user.UserId}).");
                return;
            }

            LogService.LogGameAction(LogAction.Ban, Context.User.Id, userId);

            var embed = new EmbedBuilder()
            {
                Title = $"{user.UserName} ({user.UserId}) is no longer banned in-game!",
                Color = Color.Green
            };

            await ModifyOriginalResponseAsync(x => x.Embed = embed.Build() );
        }

        [SlashCommand("updateemail", "Update a user's email")]
        public async Task TryUpdateEmailAsync(uint userId, string email)
        {
            var user = await GLClient.GetFullPhoenixUserAsync(userId);

            if (user.Email == email)
            {
                await ModifyOriginalResponseAsync(x => x.Content = $"{user.UserName} ({user.UserId}) already has `{user.Email}` as their email!");
                return;
            }

            if (!await GLClient.TryUpdateEmail(userId, email))
            {
                await ModifyOriginalResponseAsync(x => x.Content = $"Could not update email for {user.UserName} ({user.UserId})");
                return;
            }

            LogService.LogGameAction(LogAction.UpdateEmail, Context.User.Id, userId, $"{user.Email}:{email}");

            var embed = new EmbedBuilder()
            {
                Title = $"{user.UserName} ({user.UserId})'s email was updated!",
                Description = $"Old email: **{user.Email}**\nNew email: **{email}**",
                Color = Color.Blue
            };

            await ModifyOriginalResponseAsync(x => x.Embed = embed.Build() );
        }

        [SlashCommand("updatename", "Update a user's username")]
        public async Task TryUpdateNameAsync(uint userId, string username)
        {
            var user = await GLClient.GetFullPhoenixUserAsync(userId);

            if (user.UserName == username)
            {
                await ModifyOriginalResponseAsync(x => x.Content = $"{user.UserId} already has `{user.UserName}` as their email!");
                return;
            }

            if (!await GLClient.TryUpdateUsername(userId, username))
            {
                await ModifyOriginalResponseAsync(x => x.Content = $"Could not update username for {user.UserName} ({user.UserId})");
                return;
            }

            LogService.LogGameAction(LogAction.UpdateEmail, Context.User.Id, userId, $"{user.UserName}:{username}");

            var embed = new EmbedBuilder()
            {
                Title = $"{user.UserId}'s username was updated!",
                Description = $"Old name: **{user.UserName}**\nNew name: **{username}**",
                Color = Color.Blue
            };

            await ModifyOriginalResponseAsync(x => x.Embed = embed.Build() );
        }

        [SlashCommand("addbeta", "Adds GL Beta to a user")]
        public async Task AddBetaToUserAsync(uint userId)
        {
            var user = await GLClient.GetPhoenixUserAsync(userId);

            if (!await GLClient.AddGlBeta(userId))
            {
                await ModifyOriginalResponseAsync(x => x.Content = $"Failed to add beta access to {user.UserName} ({user.UserId})");
                return;
            }

            LogService.LogGameAction(LogAction.AddBeta, Context.User.Id, userId);

            var embed = new EmbedBuilder()
            {
                Title = $"{user.UserName} ({user.UserId}) now has access to beta",
                Color = Color.Green
            };

            await ModifyOriginalResponseAsync(x => x.Embed = embed.Build() );
        }

        [SlashCommand("removebeta", "Adds GL Beta to a user")]
        public async Task RemoveBetaFromUserAsync(uint userId)
        {
            var user = await GLClient.GetPhoenixUserAsync(userId);

            if (!await GLClient.RemoveGlBeta(userId))
            {
                await ModifyOriginalResponseAsync(x => x.Content = $"Failed to remove beta access to {user.UserName} ({user.UserId})");
                return;
            }

            LogService.LogGameAction(LogAction.RemoveBeta, Context.User.Id, userId);

            var embed = new EmbedBuilder()
            {
                Title = $"{user.UserName} ({user.UserId}) no longer has beta access",
                Color = Color.Red
            };

            await ModifyOriginalResponseAsync(x => x.Embed = embed.Build() );
        }
    }
}
