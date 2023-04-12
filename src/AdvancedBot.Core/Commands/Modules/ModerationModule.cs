using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AdvancedBot.Core.Commands.Preconditions;
using AdvancedBot.Core.Entities.Enums;
using AdvancedBot.Core.Services;
using Discord;
using Discord.Interactions;
using GL.NET.Entities;
using Humanizer;

namespace AdvancedBot.Core.Commands.Modules
{
    [RequirePrivateList]
    [DontAutoRegister]
    [Group("game", "All commands handling in-game actions")]
    public class ModerationModule : TopModule
    {
        [Group("alliance", "All commands handling in-game alliance related actions")]
        public class AllianceModerationModule : TopModule
        {
            [SlashCommand("warlogs", "Get warlogs of an alliance")]
            public async Task GetAllianceWarlogs(string allianceName)
            {
                var alliance = await GLClient.GetAlliance(allianceName);

                if (alliance == null)
                {
                    await ModifyOriginalResponseAsync(x => x.Content = $"<:shrugR:945740284308893696> No alliance found for **{allianceName}**");
                    return;
                }

                var warlogs = await GLClient.GetAllianceWarlogs(alliance.Id);
                var texts = new List<string>();
                var wins = warlogs.Count(x => x.WinnerId == alliance.Id);

                for (int i = 0; i < warlogs.Count; i++)
                {
                    var log = warlogs[i];
                    var endDate = new DateTime(1970, 1, 1).AddMilliseconds(log.WarEndTime);
                    var duration = TimeSpan.FromMilliseconds(log.WarEndTime - log.WarStartTime);

                    var statusText = warlogs[i].WinnerId == alliance.Id ? "**Won** against" : "**Lost** against";
                    var dateText = $"ended **{endDate.ToString("dd/MM/yyyy (HH:mm)")}**";
                    var durationText = duration.Days == 3 ? $"" : $"(KO after {(TimeSpan.FromDays(3) - duration).Humanize(3)})";

                    texts.Add($"{statusText} **{log.EnemyAllianceName}** {dateText}\n"
                    + $"**{log.SelfAllianceWarScore}**wp versus **{log.EnemyAllianceWarScore}**wp {durationText}\n");
                }

                var winLossRatio = wins / (warlogs.Count - wins == 0 ? 1 : warlogs.Count - wins);

                await LogService.LogGameActionAsync(LogAction.GetWarlogs, Context.User.Id, 0, alliance.Id);

                var templateEmbed = new EmbedBuilder()
                {
                    Title = $"Warlogs for {alliance.Name}"
                }.WithFooter($"W/L ratio: {wins}/{warlogs.Count - wins} ({winLossRatio})");

                await SendPaginatedMessageAsync(null, texts, templateEmbed);
            }

            [SlashCommand("rename", "Renames an alliance")]
            public async Task RenameAllianceAsync(string allianceName, string newAllianceName)
            {
                var alliance = await GLClient.GetAlliance(allianceName);

                if (alliance == null)
                {
                    await ModifyOriginalResponseAsync(x => x.Content = $"<:shrugR:945740284308893696> No alliance found for **{allianceName}**");
                    return;
                }

                var checkAlliance = await GLClient.GetAlliance(newAllianceName);

                if (checkAlliance != null)
                {
                    await ModifyOriginalResponseAsync(x => x.Content = $"<:shrugR:945740284308893696> Alliance with name **{newAllianceName}** already exists!");
                    return;
                }

                if (!await GLClient.RenameAllianceAsync(alliance.Id, newAllianceName))
                {
                    await ModifyOriginalResponseAsync(x => x.Content = $"Failed to rename **{allianceName}** to **{newAllianceName}**");
                    return;
                }
                
                await LogService.LogGameActionAsync(LogAction.RenameAlliance, Context.User.Id, 0, $"{alliance.Name}:{newAllianceName}");

                var embed = new EmbedBuilder()
                {
                    Title = $"{alliance.Name} is now called {newAllianceName}",
                    Color = Color.Blue
                };

                await ModifyOriginalResponseAsync(x => x.Embed = embed.Build());
            }

            [SlashCommand("makeowner", "Makes a user owner of an alliance")]
            public async Task MakeUserOwnerOfAllianceAsync(string allianceName, uint userId)
            {
                var alliance = await GLClient.GetAlliance(allianceName);

                if (alliance == null)
                {
                    await ModifyOriginalResponseAsync(x => x.Content = $"<:shrugR:945740284308893696> No alliance found for **{allianceName}**");
                    return;
                }

                var user = await GLClient.GetUserById(userId.ToString());

                if (user == null || user.AllianceId != alliance.Id)
                {
                    await ModifyOriginalResponseAsync(x => x.Content = $"This user was not found in this alliance");
                    return;
                }

                if (!await GLClient.MakeUserOwnerInAllianceAsync(alliance.Id, userId.ToString()))
                {
                    await ModifyOriginalResponseAsync(x => x.Content = $"Failed to make {user.Name} owner of **{alliance.Name}**");
                    return;
                }

                await LogService.LogGameActionAsync(LogAction.MakeUserAllianceOwner, Context.User.Id, userId, allianceName);

                var embed = new EmbedBuilder()
                {
                    Title = $"{alliance.Name} is now owner of **{alliance.Name}**",
                    Color = Color.Green
                };

                await ModifyOriginalResponseAsync(x => x.Embed = embed.Build());
            }

            [SlashCommand("removeuser", "Removes a user from an alliance")]
            public async Task RemoveUserFromAllianceAsync(string allianceName, uint userId)
            {
                var alliance = await GLClient.GetAlliance(allianceName);

                if (alliance == null)
                {
                    await ModifyOriginalResponseAsync(x => x.Content = $"<:shrugR:945740284308893696> No alliance found for **{allianceName}**");
                    return;
                }

                var user = await GLClient.GetUserById(userId.ToString());

                if (user == null || user.AllianceId != alliance.Id)
                {
                    await ModifyOriginalResponseAsync(x => x.Content = $"This user was not found in this alliance");
                    return;
                }

                if (!await GLClient.KickUserFromAllianceAsync(alliance.Id, userId.ToString()))
                {
                    await ModifyOriginalResponseAsync(x => x.Content = $"Failed to kick {user.Name} from **{alliance.Name}**");
                    return;
                }

                await LogService.LogGameActionAsync(LogAction.RemoveUserFromAlliance, Context.User.Id, userId, allianceName);

                var embed = new EmbedBuilder()
                {
                    Title = $"{alliance.Name} is now owner of **{alliance.Name}**",
                    Color = Color.Red
                };

                await ModifyOriginalResponseAsync(x => x.Embed = embed.Build());
            }
        }

        [SlashCommand("getfull", "Get full information about a user")]
        public async Task GetFullUserAsync(string input)
        {
            var user = await GetPhoenixUserByInput(input, true) as FullPhoenixUser;

            if (user == null)
            {
                await ModifyOriginalResponseAsync(x => x.Content = $"<:shrugR:945740284308893696> No user found for **{input}**");
                return;
            }

            await LogService.LogGameActionAsync(LogAction.GetFull, Context.User.Id, user.UserId);

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
            var result = await ModService.BanUserAsync(Context.User.Id, userId, reason);

            switch (result.Type)
            {
                case ModResultType.Success:
                    var embed = new EmbedBuilder()
                    {
                        Title = $"{result.PhoenixUser.UserName} ({result.PhoenixUser.UserId}) is now banned in-game!",
                        Color = Color.Red
                    };

                    await ModifyOriginalResponseAsync(x => x.Embed = embed.Build());
                    break;
                case ModResultType.NotFound:
                    await ModifyOriginalResponseAsync(x => x.Content = $"No user found for **{userId}**");
                    break;
                case ModResultType.AlreadyDone:
                    await ModifyOriginalResponseAsync(x => x.Content = $"{result.PhoenixUser.UserName} ({result.PhoenixUser.UserId}) is already banned!");
                    break;
                case ModResultType.BackendError:
                    await ModifyOriginalResponseAsync(x => x.Content = $"Failed to ban {result.PhoenixUser.UserName} ({result.PhoenixUser.UserId}).");
                    break;
            }
        }

        [SlashCommand("unban", "Tries to unban a user")]
        public async Task TryUnbanUserAsync(uint userId)
        {
            var result = await ModService.UnbanUserAsync(Context.User.Id, userId);

            switch (result.Type)
            {
                case ModResultType.Success:
                    var embed = new EmbedBuilder()
                    {
                        Title = $"{result.PhoenixUser.UserName} ({result.PhoenixUser.UserId}) is no longer banned in-game!",
                        Color = Color.Green
                    };

                    await ModifyOriginalResponseAsync(x => x.Embed = embed.Build());
                    break;
                case ModResultType.NotFound:
                    await ModifyOriginalResponseAsync(x => x.Content = $"No user found for **{userId}**");
                    break;
                case ModResultType.AlreadyDone:
                    await ModifyOriginalResponseAsync(x => x.Content = $"{result.PhoenixUser.UserName} ({result.PhoenixUser.UserId}) is not banned!");
                    break;
                case ModResultType.BackendError:
                    await ModifyOriginalResponseAsync(x => x.Content = $"Failed to unban {result.PhoenixUser.UserName} ({result.PhoenixUser.UserId}).");
                    break;
            }
        }

        [SlashCommand("updateemail", "Update a user's email")]
        public async Task TryUpdateEmailAsync(uint userId, string email)
        {
            var user = await GLClient.GetFullPhoenixUserAsync(userId);

            if (user == null)
            {
                await ModifyOriginalResponseAsync(x => x.Content = $"No user found for **{userId}**");
                return;
            }

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

            await LogService.LogGameActionAsync(LogAction.UpdateEmail, Context.User.Id, userId, $"{user.Email}:{email}");

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

            if (user == null)
            {
                await ModifyOriginalResponseAsync(x => x.Content = $"No user found for **{userId}**");
                return;
            }

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

            await LogService.LogGameActionAsync(LogAction.UpdateName, Context.User.Id, userId, $"{user.UserName}:{username}");

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
            var result = await ModService.AddBetaToUserAsync(Context.User.Id, userId);

            switch (result.Type)
            {
                case ModResultType.Success:
                    var embed = new EmbedBuilder()
                    {
                        Title = $"{result.PhoenixUser.UserName} ({result.PhoenixUser.UserId}) now has access to beta",
                        Color = Color.Green
                    };

                    await ModifyOriginalResponseAsync(x => x.Embed = embed.Build());
                    break;
                case ModResultType.NotFound:
                    await ModifyOriginalResponseAsync(x => x.Content = $"No user found for **{userId}**");
                    break;
                case ModResultType.BackendError:
                    await ModifyOriginalResponseAsync(x => x.Content = $"Failed to add beta access to {result.PhoenixUser.UserName} ({result.PhoenixUser.UserId})");
                    break;
            }
        }

        [SlashCommand("removebeta", "Removes GL Beta to a user")]
        public async Task RemoveBetaFromUserAsync(uint userId)
        {
            var result = await ModService.RemoveBetaFromUserAsync(Context.User.Id, userId);

            switch (result.Type)
            {
                case ModResultType.Success:
                    var embed = new EmbedBuilder()
                    {
                        Title = $"{result.PhoenixUser.UserName} ({result.PhoenixUser.UserId}) no longer has beta access",
                        Color = Color.Green
                    };

                    await ModifyOriginalResponseAsync(x => x.Embed = embed.Build());
                    break;
                case ModResultType.NotFound:
                    await ModifyOriginalResponseAsync(x => x.Content = $"No user found for **{userId}**");
                    break;
                case ModResultType.BackendError:
                    await ModifyOriginalResponseAsync(x => x.Content = $"Failed to remove beta access to {result.PhoenixUser.UserName} ({result.PhoenixUser.UserId})");
                    break;
            }
        }

        [SlashCommand("giverole", "Gives a certain user a role")]
        public async Task GiveRoleAsync(uint userId, PhoenixRole role)
        {
            var result = await ModService.GiveRoleAsync(Context.User.Id, userId, role);

            var roleText = role == PhoenixRole.Donator ? "a Donator"
                : role == PhoenixRole.Staff ? "a Staff Member"
                : role == PhoenixRole.Administrator ? "an Admin"
                : role.ToString();

            switch (result.Type)
            {
                case ModResultType.Success:
                    var embed = new EmbedBuilder()
                    {
                        Title = $"{result.PhoenixUser.UserName} ({result.PhoenixUser.UserId}) is now {roleText}",
                        Color = Color.Blue
                    };

                    await ModifyOriginalResponseAsync(x => x.Embed = embed.Build());
                    break;
                case ModResultType.NotFound:
                    await ModifyOriginalResponseAsync(x => x.Content = $"No user found for **{userId}**");
                    break;
                case ModResultType.AlreadyDone:
                    await ModifyOriginalResponseAsync(x => x.Content = $"User is already {roleText}!");
                    break;
                case ModResultType.BackendError:
                    await ModifyOriginalResponseAsync(x => x.Content = $"Failed to give {role} to {result.PhoenixUser.UserName} ({result.PhoenixUser.UserId})");
                    break;
            }
        }

        [SlashCommand("chipsbought", "Gets chips bought from a user")]
        public async Task GetChipsBoughtAsync(uint userId)
        {
            var result = await ModService.GetChipsBoughtAsync(Context.User.Id, userId);

            switch (result.Type)
            {
                case ModResultType.Success:
                    var embed = new EmbedBuilder()
                    {
                        Title = $"{result.User.Name} ({result.User.Id})",
                        Description = $"**{result.IntValue}** chips bought",
                        Color = Color.Blue
                    };

                    await ModifyOriginalResponseAsync(x => x.Embed = embed.Build());
                    break;
                case ModResultType.NotFound:
                    await ModifyOriginalResponseAsync(x => x.Content = $"No user found for **{userId}**");
                    break;
            }
        }

        [SlashCommand("addchips", "Adds chips to a user")]
        public async Task AddChipsToUserAsync(uint userId, int amount)
        {
            var result = await ModService.AddChipsAsync(Context.User.Id, userId, amount);

            switch (result.Type)
            {
                case ModResultType.Success:
                    var embed = new EmbedBuilder()
                    {
                        Title = $"Added {amount} chips to {result.User.Name} ({result.User.Id})",
                        Color = Color.Blue
                    };

                    await ModifyOriginalResponseAsync(x => x.Embed = embed.Build());
                    break;
                case ModResultType.NotFound:
                    await ModifyOriginalResponseAsync(x => x.Content = $"No user found for **{userId}**");
                    break;
                case ModResultType.BackendError:
                    await ModifyOriginalResponseAsync(x => x.Content = $"Failed to add chips to {result.User.Name} ({result.User.Id})");
                    break;
            }
        }

        [SlashCommand("additem", "Adds an item a user")]
        public async Task AddItemsToUserAsync(uint userId, string sku, int amount)
        {
            var result = await ModService.AddItemsAsync(Context.User.Id, userId, sku, amount);

            switch (result.Type)
            {
                case ModResultType.Success:
                    var embed = new EmbedBuilder()
                    {
                        Title = $"Added item {sku} {amount}x to {result.User.Name} ({result.User.Id})",
                        Color = Color.Blue
                    };

                    await ModifyOriginalResponseAsync(x => x.Embed = embed.Build());
                    break;
                case ModResultType.NotFound:
                    await ModifyOriginalResponseAsync(x => x.Content = $"No user found for **{userId}**");
                    break;
                case ModResultType.BackendError:
                    await ModifyOriginalResponseAsync(x => x.Content = $"Failed to add item with id {sku} to {result.User.Name} ({result.User.Id})");
                    break;
            }
        }

        [SlashCommand("kick", "Force kicks a user offline")]
        public async Task KickUserOfflineAsync(uint userId)
        {
            var user = await GLClient.GetPhoenixUserAsync(userId);

            if (user == null)
            {
                await ModifyOriginalResponseAsync(x => x.Content = $"No user found for **{userId}**");
                return;
            }
            
            if (!await GLClient.TryKickUserOfflineAsync(userId.ToString()))
            {
                await ModifyOriginalResponseAsync(x => x.Content = $"Failed to kick {user.UserName} ({user.UserId}) offline");
                return;
            }

            await LogService.LogGameActionAsync(LogAction.KickOffline, Context.User.Id, userId);

            var embed = new EmbedBuilder()
            {
                Title = $"Forced {user.UserName} ({user.UserId}) offline",
                Color = Color.Blue
            };

            await ModifyOriginalResponseAsync(x => x.Embed = embed.Build() );
        }

        [SlashCommand("reset", "Force kicks a user offline")]
        public async Task ResetUserAsync(uint userId)
        {
            var user = await GLClient.GetPhoenixUserAsync(userId);

            if (user == null)
            {
                await ModifyOriginalResponseAsync(x => x.Content = $"No user found for **{userId}**");
                return;
            }
            
            if (!await GLClient.TryResetUserAsync(userId.ToString()))
            {
                await ModifyOriginalResponseAsync(x => x.Content = $"Failed to reset {user.UserName} ({user.UserId})");
                return;
            }

            await LogService.LogGameActionAsync(LogAction.Reset, Context.User.Id, userId);

            var embed = new EmbedBuilder()
            {
                Title = $"Reset {user.UserName} ({user.UserId})",
                Color = Color.Red
            };

            await ModifyOriginalResponseAsync(x => x.Embed = embed.Build() );
        }
    }
}
