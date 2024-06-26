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
    [DontAutoRegister]
    [RequirePrivateList]
    [Group("game", "All commands handling in-game actions")]
    public class ModerationModule : TopModule
    {
        public ModerationService ModService { get; set; }

        [Group("alliance", "All commands handling in-game alliance related actions")]
        public class AllianceModerationModule : TopModule
        {
            [SlashCommand("warlogs", "Get warlogs of an alliance")]
            public async Task GetAllianceWarlogs(string allianceName)
            {
                var alliance = await GLClient.Api.GetAlliance(allianceName);

                if (alliance == null)
                {
                    await ModifyOriginalResponseAsync(x => x.Content = $"<:shrugR:945740284308893696> No alliance found for **{allianceName}**");
                    return;
                }

                var warlogs = await GLClient.Api.GetAllianceWarlogs(alliance.Id);
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
                var alliance = await GLClient.Api.GetAlliance(allianceName);

                if (alliance == null)
                {
                    await ModifyOriginalResponseAsync(x => x.Content = $"<:shrugR:945740284308893696> No alliance found for **{allianceName}**");
                    return;
                }

                var checkAlliance = await GLClient.Api.GetAlliance(newAllianceName);

                if (checkAlliance != null)
                {
                    await ModifyOriginalResponseAsync(x => x.Content = $"<:shrugR:945740284308893696> Alliance with name **{newAllianceName}** already exists!");
                    return;
                }

                if (!await GLClient.Production.RenameAllianceAsync(alliance.Id, newAllianceName))
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
                var alliance = await GLClient.Api.GetAlliance(allianceName);

                if (alliance == null)
                {
                    await ModifyOriginalResponseAsync(x => x.Content = $"<:shrugR:945740284308893696> No alliance found for **{allianceName}**");
                    return;
                }

                var user = await GLClient.Api.GetUserById(userId.ToString());

                if (user == null || user.AllianceId != alliance.Id)
                {
                    await ModifyOriginalResponseAsync(x => x.Content = $"This user was not found in this alliance");
                    return;
                }

                if (!await GLClient.Api.MakeUserOwnerInAllianceAsync(alliance.Id, userId.ToString()))
                {
                    await ModifyOriginalResponseAsync(x => x.Content = $"Failed to make {user.Name} owner of **{alliance.Name}**");
                    return;
                }

                await LogService.LogGameActionAsync(LogAction.MakeUserAllianceOwner, Context.User.Id, userId, allianceName);

                var embed = new EmbedBuilder()
                {
                    Title = $"{user.Name} is now owner of **{alliance.Name}**",
                    Color = Color.Green
                };

                await ModifyOriginalResponseAsync(x => x.Embed = embed.Build());
            }

            [SlashCommand("removeuser", "Removes a user from an alliance")]
            public async Task RemoveUserFromAllianceAsync(string allianceName, uint userId)
            {
                var alliance = await GLClient.Api.GetAlliance(allianceName);

                if (alliance == null)
                {
                    await ModifyOriginalResponseAsync(x => x.Content = $"<:shrugR:945740284308893696> No alliance found for **{allianceName}**");
                    return;
                }

                var user = await GLClient.Api.GetUserById(userId.ToString());

                if (user == null || user.AllianceId != alliance.Id)
                {
                    await ModifyOriginalResponseAsync(x => x.Content = $"This user was not found in this alliance");
                    return;
                }

                if (!await GLClient.Api.KickUserFromAllianceAsync(alliance.Id, userId.ToString()))
                {
                    await ModifyOriginalResponseAsync(x => x.Content = $"Failed to kick {user.Name} from **{alliance.Name}**");
                    return;
                }

                await LogService.LogGameActionAsync(LogAction.RemoveUserFromAlliance, Context.User.Id, userId, allianceName);

                var embed = new EmbedBuilder()
                {
                    Title = $"{alliance.Name} is now kicked from **{alliance.Name}**",
                    Color = Color.Red
                };

                await ModifyOriginalResponseAsync(x => x.Embed = embed.Build());
            }
        }

        [SlashCommand("getfull", "Get full information about a user")]
        public async Task GetFullUserAsync(string input)
        {
            if (await GetPhoenixUserByInput(input, true) is not FullPhoenixUser user)
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
        public async Task TryBanUserAsync(uint userId, string reason, uint days = 0)
        {
            var result = await ModService.BanUserAsync(Context.User.Id, userId, reason, days);
            await SendResponseMessage(result.Message, false);
        }

        [SlashCommand("unban", "Tries to unban a user")]
        public async Task TryUnbanUserAsync(uint userId)
        {
            var result = await ModService.UnbanUserAsync(Context.User.Id, userId);
            await SendResponseMessage(result.Message, false);
        }

        [SlashCommand("updateemail", "Update a user's email")]
        public async Task TryUpdateEmailAsync(uint userId, string email)
        {
            var user = await GLClient.Phoenix.GetFullPhoenixUserAsync(userId);

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

            if (!await GLClient.Phoenix.TryUpdateEmail(userId, email))
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
            var user = await GLClient.Phoenix.GetFullPhoenixUserAsync(userId);

            if (user == null)
            {
                await ModifyOriginalResponseAsync(x => x.Content = $"No user found for **{userId}**");
                return;
            }

            if (user.UserName == username)
            {
                await ModifyOriginalResponseAsync(x => x.Content = $"{user.UserId} already has `{user.UserName}` as their name!");
                return;
            }

            if (!await GLClient.Phoenix.TryUpdateUsername(userId, username))
            {
                await ModifyOriginalResponseAsync(x => x.Content = $"Could not update username for {user.UserName} ({user.UserId})");
                return;
            }

            var backendSuccess = await GLClient.Api.UpdateNameFromPhoenixAsync(userId.ToString());

            await LogService.LogGameActionAsync(LogAction.UpdateName, Context.User.Id, userId, $"{user.UserName}:{username}");

            var embed = new EmbedBuilder()
            {
                Title = $"{user.UserId}'s username was updated!",
                Description = $"Old name: **{user.UserName}**\nNew name: **{username}**\n Updated in backend: **{backendSuccess}**",
                Color = Color.Blue
            };

            await ModifyOriginalResponseAsync(x => x.Embed = embed.Build() );
        }

        [SlashCommand("addbeta", "Adds GL Beta to a user")]
        public async Task AddBetaToUserAsync(uint userId)
        {
            var result = await ModService.AddBetaToUserAsync(Context.User.Id, userId);
            await SendResponseMessage(result.Message, false);
        }

        [SlashCommand("removebeta", "Removes GL Beta to a user")]
        public async Task RemoveBetaFromUserAsync(uint userId)
        {
            var result = await ModService.RemoveBetaFromUserAsync(Context.User.Id, userId);
            await SendResponseMessage(result.Message, false);
        }

        [SlashCommand("addemulate", "Adds Emulate Access to a user")]
        public async Task AddEmulateToUserAsync(uint userId)
        {
            var result = await ModService.AddEmulateToUserAsync(Context.User.Id, userId);
            await SendResponseMessage(result.Message, false);
        }

        [SlashCommand("removeemulate", "Removes Emulate Access to a user")]
        public async Task RemoveEmulateFromUserAsync(uint userId)
        {
            var result = await ModService.RemoveEmulateFromUserAsync(Context.User.Id, userId);
            await SendResponseMessage(result.Message, false);
        }

        [SlashCommand("giverole", "Gives a certain user a role")]
        public async Task GiveRoleAsync(uint userId, PhoenixRole role)
        {
            var result = await ModService.GiveRoleAsync(Context.User.Id, userId, role);
            await SendResponseMessage(result.Message, false);
        }

        [SlashCommand("chipsbought", "Gets chips bought from a user")]
        public async Task GetChipsBoughtAsync(uint userId)
        {
            var result = await ModService.GetChipsBoughtAsync(Context.User.Id, userId);
            await SendResponseMessage(result.Message, false);
        }

        [SlashCommand("chipsspent", "Gets chips bought from a user")]
        public async Task GetChipsSpentAsync(uint userId)
        {
            var result = await ModService.GetChipsSpentAsync(Context.User.Id, userId);
            await SendResponseMessage(result.Message, false);
        }

        [SlashCommand("addchips", "Adds chips to a user")]
        public async Task AddChipsToUserAsync(uint userId, int amount)
        {
            var result = await ModService.AddChipsAsync(Context.User.Id, userId, amount);
            await SendResponseMessage(result.Message, false);
        }

        [SlashCommand("additem", "Adds an item a user")]
        public async Task AddItemsToUserAsync(uint userId, string sku, int amount)
        {
            var result = await ModService.AddItemsAsync(Context.User.Id, userId, sku, amount);
            await SendResponseMessage(result.Message, false);
        }

        [SlashCommand("addxp", "Adds xp to a user")]
        public async Task AddXpToUserAsync(uint userId, int amount)
        {
            var result = await ModService.AddXpAsync(Context.User.Id, userId, amount);
            await SendResponseMessage(result.Message, false);
        }

        [SlashCommand("kick", "Force kicks a user offline")]
        public async Task KickUserOfflineAsync(uint userId)
        {
            var user = await GLClient.Phoenix.GetPhoenixUserAsync(userId);

            if (user == null)
            {
                await ModifyOriginalResponseAsync(x => x.Content = $"No user found for **{userId}**");
                return;
            }
            
            if (!await GLClient.Production.TryKickUserOfflineAsync(userId.ToString()))
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

        [SlashCommand("reset", "Resets a users progress")]
        public async Task ResetUserAsync(uint userId)
        {
            var user = await GLClient.Phoenix.GetPhoenixUserAsync(userId);

            if (user == null)
            {
                await ModifyOriginalResponseAsync(x => x.Content = $"No user found for **{userId}**");
                return;
            }
            
            if (!await GLClient.Production.TryResetUserAsync(userId.ToString()))
            {
                await ModifyOriginalResponseAsync(x => x.Content = $"Failed to reset {user.UserName} ({user.UserId})");
                return;
            }

            await LogService.LogGameActionAsync(LogAction.Reset, Context.User.Id, userId, "Production");

            var embed = new EmbedBuilder()
            {
                Title = $"Reset {user.UserName} ({user.UserId})",
                Color = Color.Red
            };

            await ModifyOriginalResponseAsync(x => x.Embed = embed.Build() );
        }

        [SlashCommand("reloadrules", "Reloads the server rules")]
        public async Task ReloadRulesAsync()
        {
            var result = await ModService.ReloadRules(Context.User.Id);
            await SendResponseMessage(result.Message, false);
        }

        [SlashCommand("maintenance", "Enables maintenance on the server")]
        public async Task EnableMaintenanceAsync(uint minutes = 60)
        {
            var result = await ModService.EnableMaintenance(Context.User.Id, minutes);
            await SendResponseMessage(result.Message, false);
        }

        [SlashCommand("compensatechips", "Compensate chips to all users")]
        public async Task CompensateChips(uint amount)
        {
            GLClient.Production.CompensateChips(amount);

            await LogService.LogGameActionAsync(LogAction.Compensate, Context.User.Id, 0, $"Chips:{amount}");

            await ModifyOriginalResponseAsync(x => x.Content = $"Send out compensation of {amount} chips to everyone!");
        }

        [SlashCommand("compensateitems", "Compensate items to all users")]
        public async Task CompensateItems(string sku, uint amount)
        {
            GLClient.Production.CompensateItems(sku, amount);

            await LogService.LogGameActionAsync(LogAction.Compensate, Context.User.Id, 0, $"Items:{sku}:{amount}");

            await ModifyOriginalResponseAsync(x => x.Content = $"Send out compensation of {amount}x item {sku} to everyone!");
        }

        [SlashCommand("lb", "Shows all the possible leaderboards")]
        public async Task GetLeaderboardAsync([Choice("Xp", "xp"), Choice("Xp From Attack", "attackXp"), Choice("Rivals Won", "rivalsWon"), Choice("Chips", "chips"), Choice("Chips Spent", "chipsSpent"), Choice("Friends Helped", "friendsHelped"), Choice("Gifts Received", "giftsReceived"), Choice("Gifts Sent", "giftsSent"), Choice("Stars Visited", "starsVisited"), Choice("Obstacles Recycled", "obstaclesRecycled"), Choice("Utility Used", "utilityUsed"), Choice("Item", "item"), Choice("Warpoints", "warpoints"), Choice("Alliance Warpoints", "alliancewarpoints"), Choice("Advanced Chips", "advchips")]string type, string sku = "7000")
        {
            List<string> displayTexts = new List<string>() { "Failed to get information" };
            var title = "Galaxy Life Leaderboard";

            switch (type)
            {
                case "attackXp":
                    title = "Xp From Attack Leaderboard";
                    displayTexts = (await GLClient.Api.GetXpFromAttackLeaderboard()).Select(x => $"<:RedExp:1082428998182768701>{x.Level} **{x.Name}**").ToList();
                    break;
                case "rivalsWon":
                    title = "Rivals Won Leaderboard";
                    displayTexts = (await GLClient.Api.GetRivalsWonLeaderboard()).Select(x => $"<:pistol:1082429024963395674>{x.RivalsWon} **{x.Name}**").ToList();
                    break;
                case "chips":
                    title = "Chips Leaderboard";
                    displayTexts = (await GLClient.Api.GetChipsLeaderboard()).Select(x => $"<:Resource_Chip:943313446940868678>{x.Chips} **{x.Name}**").ToList();
                    break;
                case "chipsSpent":
                    title = "Chips Spent Leaderboard";
                    displayTexts = (await GLClient.Api.GetChipsSpentLeaderboard()).Select(x => $"<:Resource_PileOfChips:943313554742865951>{x.ChipsSpent} **{x.Name}**").ToList();
                    break;
                case "friendsHelped":
                    title = "Friends Helped Leaderboard";
                    displayTexts = (await GLClient.Api.GetFriendsHelpedLeaderboard()).Select(x => $"<:Starling_Sorry:943311734196809821>{x.FriendsHelped} **{x.Name}**").ToList();
                    break;
                case "giftsReceived":
                    title = "Gifts Received Leaderboard";
                    displayTexts = (await GLClient.Api.GetGiftsReceivedLeaderboard()).Select(x => $"<:Story_Chubi_Happy:943325609113833492>{x.GiftsReceived} **{x.Name}**").ToList();
                    break;
                case "giftsSent":
                    title = "Gifts Sent Leaderboard";
                    displayTexts = (await GLClient.Api.GetGiftsSentLeaderboard()).Select(x => $"<:Starling_Gentleman:945539138311061554>{x.GiftsSent} **{x.Name}**").ToList();
                    break;
                case "starsVisited":
                    title = "Stars Visited Leaderboard";
                    displayTexts = (await GLClient.Api.GetStarsVisitedLeaderboard()).Select(x => $"⭐{x.StarsVisited} **{x.Name}**").ToList();
                    break;
                case "obstaclesRecycled":
                    title = "Obstacles Recycled Leaderboard";
                    displayTexts = (await GLClient.Api.GetObstaclesRecycledLeaderboard()).Select(x => $"<:TouchGrass:1085581198690099281>{x.ObstaclesRecycled} **{x.Name}**").ToList();
                    break;
                case "utilityUsed":
                    title = "Utility Used Leaderboard";
                    displayTexts = (await GLClient.Api.GetUtilityUsedLeaderboard()).Select(x => $"<:Nuke:1034465682835898408>{x.UtilityUsed} **{x.Name}**").ToList();
                    break;
                case "item":
                    title = $"Item {sku} Leaderboard";
                    displayTexts = (await GLClient.Api.GetItemLeaderboard(sku)).Select(x => $"<:Item_Helmet:1084821573975945267>{x.Quantity} **{x.Name}**").ToList();
                    break;
                case "warpoints":
                    title = $"Warpoints Leaderboard";
                    displayTexts = (await GLClient.Api.GetWarpointLeaderboard()).Select(x => $"<:Starling_Frenchling:1080133173352091708>{x.Warpoints} **{x.Name}** ({x.AllianceName})").ToList();
                    break;
                case "alliancewarpoints":
                    title = $"Alliance Warpoints Leaderboard";
                    displayTexts = (await GLClient.Api.GetAllianceWarpointLeaderboard()).Select(x => $"<:TopNotch:945458565538279515>{x.Warpoints} **{x.Name}** ({x.MemberCount} members)").ToList();
                    break;
                case "advchips":
                    title = $"Advanced Chips";
                    displayTexts = (await GLClient.Api.GetAdvancedChipsLb()).Select(x => $"<:Resource_PileOfChips:943313554742865951>{x.Chips + x.ChipsSpent - x.ChipsPurchased} **{x.Name}**").ToList();
                    break;
                default:
                case "xp":
                    title = "Xp Leaderboard";
                    displayTexts = (await GLClient.Api.GetXpLeaderboard()).Select(x => $"<:experience:920289172428849182> {x.Level} **{x.Name}**").ToList();
                    break;
            }

            if (displayTexts.Count == 0)
            {
                await ModifyOriginalResponseAsync(x => x.Content = $"<:Starling_WorkerHappy:943308706555260928> Servers are still loading the leaderboard, please be patient!");
                return;
            }

            for (int i = 0; i < displayTexts.Count(); i++)
            {
                displayTexts[i] = $"**#{i + 1}** | {displayTexts[i]}";
            }

            await SendPaginatedMessageAsync(null, displayTexts, new EmbedBuilder()
            {
                Title = title,
                Color = Color.Purple
            });
        }
    }
}
