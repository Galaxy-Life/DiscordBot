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
            [SlashCommand("warlogs", "Retrieves an alliance's warlog")]
            public async Task GetAllianceWarlogs(string allianceName)
            {
                var alliance = await GLClient.Api.GetAlliance(allianceName);

                if (alliance == null)
                {
                    await ModifyOriginalResponseAsync(msg => msg.Content = $"<:shrugR:945740284308893696> Could not find any alliance named **{allianceName}**");
                    return;
                }

                var warlogs = await GLClient.Api.GetAllianceWarlogs(alliance.Id);
                var texts = new List<string>();
                var wins = warlogs.Count(war => war.WinnerId == alliance.Id);

                for (int i = 0; i < warlogs.Count; i++)
                {
                    var log = warlogs[i];
                    var endDate = new DateTime(1970, 1, 1).AddMilliseconds(log.WarEndTime);
                    var duration = TimeSpan.FromMilliseconds(log.WarEndTime - log.WarStartTime);

                    var statusText = warlogs[i].WinnerId == alliance.Id ? "**Won** against" : "**Lost** against";
                    var dateText = $"ended **{endDate:dd/MM/yyyy (HH:mm)}**";
                    var durationText = duration.Days == 3 ? $"" : $"(KO after {(TimeSpan.FromDays(3) - duration).Humanize(3)})";

                    texts.Add($"{statusText} **{log.EnemyAllianceName}** {dateText}\n"
                    + $"**{log.SelfAllianceWarScore}**wp versus **{log.EnemyAllianceWarScore}**wp {durationText}\n");
                }

                var winLossRatio = wins / (warlogs.Count - wins == 0 ? 1 : warlogs.Count - wins);

                await LogService.LogGameActionAsync(LogAction.GetWarlogs, Context.User.Id, 0, alliance.Id);

                var templateEmbed = new EmbedBuilder()
                    .WithTitle($"Warlogs for {alliance.Name}")
                    .AddField("Wins", $"{wins}W", true)
                    .AddField("Loss", $"{warlogs.Count - wins}L", true)
                    .AddField("W/L", $"{winLossRatio}%", true)
                    .WithFooter(footer => footer
                        .WithText($"Warlogs requested by {Context.User.Username}#{Context.User.Discriminator}")
                        .WithIconUrl(Context.User.GetAvatarUrl()))
                    .WithCurrentTimestamp();

                await SendPaginatedMessageAsync(null, texts, templateEmbed);
            }

            [SlashCommand("rename", "Renames an alliance")]
            public async Task RenameAllianceAsync(string allianceName, string newAllianceName)
            {
                var alliance = await GLClient.Api.GetAlliance(allianceName);

                if (alliance == null)
                {
                    await ModifyOriginalResponseAsync(msg => msg.Content = $"<:shrugR:945740284308893696> Could not find any alliance named **{allianceName}**.");
                    return;
                }

                var checkAlliance = await GLClient.Api.GetAlliance(newAllianceName);

                if (checkAlliance != null)
                {
                    await ModifyOriginalResponseAsync(msg => msg.Content = $"<:shrugR:945740284308893696> This alliance name is not available!");
                    return;
                }

                if (!await GLClient.Production.RenameAllianceAsync(alliance.Id, newAllianceName))
                {
                    await ModifyOriginalResponseAsync(msg => msg.Content = $"Could not rename **{allianceName}** to **{newAllianceName}**.");
                    return;
                }
                
                await LogService.LogGameActionAsync(LogAction.RenameAlliance, Context.User.Id, 0, $"{alliance.Name}:{newAllianceName}");

                var embed = new EmbedBuilder()
                    .WithTitle("Alliance successfully renamed")
                    .AddField("Previous", $"**{alliance.Name}**", true)
                    .AddField("Updated", $"**{newAllianceName}**", true)
                    .WithColor(Color.Green)
                    .WithFooter(footer => footer
                        .WithText($"Alliance rename requested by {Context.User.Username}#{Context.User.Discriminator}")
                        .WithIconUrl(Context.User.GetAvatarUrl()))
                    .WithCurrentTimestamp()
                    .Build();

                await ModifyOriginalResponseAsync(msg => msg.Embeds = new Embed[] { embed });
            }

            [SlashCommand("makeowner", "Transfers alliance ownership to a player.")]
            public async Task MakeUserOwnerOfAllianceAsync(string allianceName, uint userId)
            {
                var alliance = await GLClient.Api.GetAlliance(allianceName);

                if (alliance == null)
                {
                    await ModifyOriginalResponseAsync(msg => msg.Content = $"<:shrugR:945740284308893696> Could not find any alliance named **{allianceName}**.");
                    return;
                }

                var user = await GLClient.Api.GetUserById(userId.ToString());

                if (user == null || user.AllianceId != alliance.Id)
                {
                    await ModifyOriginalResponseAsync(msg => msg.Content = $"<:shrugR:945740284308893696> This user is not a member of this alliance.");                    return;
                }

                if (!await GLClient.Api.MakeUserOwnerInAllianceAsync(alliance.Id, userId.ToString()))
                {
                    await ModifyOriginalResponseAsync(msg => msg.Content = $"Could not make **{user.Name}** the owner of **{alliance.Name}**.");
                    return;
                }
                await LogService.LogGameActionAsync(LogAction.MakeUserAllianceOwner, Context.User.Id, userId, allianceName);

                var embed = new EmbedBuilder()
                    .WithTitle("Ownership successfully given")
                    .WithDescription($"Player **{user.Name}** is now the owner of **${allianceName}**.")
                    .WithColor(Color.Green)
                    .WithFooter(footer => footer
                        .WithText($"Ownership transfer requested by {Context.User.Username}#{Context.User.Discriminator}")
                        .WithIconUrl(Context.User.GetAvatarUrl()))
                    .WithCurrentTimestamp()
                    .Build();

                await ModifyOriginalResponseAsync(msg => msg.Embeds = new Embed[] { embed });
            }

            [SlashCommand("removeuser", "Kicks a user from an alliance.")]
            public async Task RemoveUserFromAllianceAsync(string allianceName, uint userId)
            {
                var alliance = await GLClient.Api.GetAlliance(allianceName);

                if (alliance == null)
                {
                    await ModifyOriginalResponseAsync(msg => msg.Content = $"<:shrugR:945740284308893696> Could not find any alliance named **{allianceName}**.");
                    return;
                }

                var user = await GLClient.Api.GetUserById(userId.ToString());
                if (user == null || user.AllianceId != alliance.Id)
                {
                    await ModifyOriginalResponseAsync(msg => msg.Content = $"<:shrugR:945740284308893696> This user is not a member of this alliance.");
                    return;
                }

                if (!await GLClient.Api.KickUserFromAllianceAsync(alliance.Id, userId.ToString()))
                {
                    await ModifyOriginalResponseAsync(msg => msg.Content = $"Could not kick **{user.Name}** from **{alliance.Name}**.");
                    return;
                }

                await LogService.LogGameActionAsync(LogAction.RemoveUserFromAlliance, Context.User.Id, userId, allianceName);

                var embed = new EmbedBuilder()
                    .WithTitle("Member successfully kicked")
                    .WithDescription($"Player **{user.Name}** has been kicked of **{alliance.Name}**.")
                    .WithColor(Color.Green)
                    .WithFooter(footer => footer
                        .WithText($"Kick requested by {Context.User.Username}#{Context.User.Discriminator}")
                        .WithIconUrl(Context.User.GetAvatarUrl()))
                    .WithCurrentTimestamp()
                    .Build();

                await ModifyOriginalResponseAsync(msg => msg.Embeds = new Embed[] { embed });
            }
        }

        [SlashCommand("getfull", "Retrieves the complete profile of a user.")]
        public async Task GetFullUserAsync(string input)
        {
            if (await GetPhoenixUserByInput(input, true) is not FullPhoenixUser user)
            {
                await ModifyOriginalResponseAsync(msg => msg.Content = $"<:shrugR:945740284308893696> Could not find any user for **{input}**.");
                return;
            }

            await LogService.LogGameActionAsync(LogAction.GetFull, Context.User.Id, user.UserId);

            var steamId = user.SteamId ?? "No account linked";
            var discordTag = string.IsNullOrEmpty(user.DiscordId) ? "No account linked" : $"<@{user.DiscordId}>";
           
            var description = 
                  user.Role == PhoenixRole.Banned ? $"**This user has been banned!!**\nBan Reason: **{user.BanReason}**\n\n"
                : user.Role == PhoenixRole.Donator ? "This user is a Donator\n\n"
                : user.Role == PhoenixRole.Staff ? "This user is a Staff Member\n\n"
                : user.Role == PhoenixRole.Administrator ? "This user is an Admin\n\n"
                : "";

            var color = 
                  user.Role == PhoenixRole.Banned ? Color.Default
                : user.Role == PhoenixRole.Donator ? new Color(15710778)
                : user.Role == PhoenixRole.Staff ? new Color(2605694)
                : user.Role == PhoenixRole.Administrator ? Color.DarkRed
                : Color.LightGrey;

            var embed = new EmbedBuilder()
                .WithTitle($"Profile of {user.UserName}")
                .WithDescription(description)
                .WithColor(color)
                .AddField("ID", $"`{user.UserId}`", true)
                .AddField("Steam ID", $"`{steamId.Replace("\"", "")}`", true)
                .AddField("Discord", $"{discordTag}", true)
                .AddField("Email", $"{user.Email}")
                .AddField("Account creation date", $"{user.Created.GetValueOrDefault():dd MMMM yyyy a\\t HH:mm}", true)
                .WithFooter(footer => footer
                    .WithText($"Full profile requested by {Context.User.Username}#{Context.User.Discriminator}")
                    .WithIconUrl(Context.User.GetAvatarUrl()))
                .WithCurrentTimestamp();

            if (user.SteamId != null)
            {
                embed.WithUrl($"https://steamcommunity.com/profiles/{steamId.Replace("\"", "")}");
            }

            await ModifyOriginalResponseAsync(msg => msg.Embeds = new Embed[] { embed.Build() });
        }

        [SlashCommand("ban", "Bans a given user")]
        public async Task TryBanUserAsync(uint userId, string reason, uint days = 0)
        {
            var result = await ModService.BanUserAsync(Context.User.Id, userId, reason, days);
            await SendResponseMessage(result.Message, false);
        }

        [SlashCommand("unban", "Unbans a given user")]
        public async Task TryUnbanUserAsync(uint userId)
        {
            var result = await ModService.UnbanUserAsync(Context.User.Id, userId);
            await SendResponseMessage(result.Message, false);
        }

        [SlashCommand("updateemail", "Updates a given user's email address")]
        public async Task TryUpdateEmailAsync(uint userId, string newEmail)
        {
            var user = await GLClient.Phoenix.GetFullPhoenixUserAsync(userId);

            if (user == null)
            {
                await ModifyOriginalResponseAsync(msg => msg.Content = $"Could not find any user with id **{userId}**.");
                return;
            }

            if (user.Email == newEmail)
            {
                await ModifyOriginalResponseAsync(msg => msg.Content = $"{user.UserName} ({user.UserId}) already has `{user.Email}` as their email!");
                return;
            }

            if (!await GLClient.Phoenix.TryUpdateEmail(userId, newEmail))
            {
                await ModifyOriginalResponseAsync(msg => msg.Content = $"Could not update email for {user.UserName} ({user.UserId})");
                return;
            }

            await LogService.LogGameActionAsync(LogAction.UpdateEmail, Context.User.Id, userId, $"{user.Email}:{newEmail}");

            var embed = new EmbedBuilder()
                .WithTitle("Email successfully updated")
                .WithDescription($"**{user.UserName}** email has been updated.")
                .AddField("Previous", $"**{user.Email}**", true)
                .AddField("Updated", $"**{newEmail}**", true)
                .WithColor(Color.Green)
                .WithFooter(footer => footer
                    .WithText($"Email change requested by {Context.User.Username}#{Context.User.Discriminator}")
                    .WithIconUrl(Context.User.GetAvatarUrl()))
                .WithCurrentTimestamp()
                .Build();

            await ModifyOriginalResponseAsync(msg => msg.Embeds = new Embed[] { embed });
        }

        [SlashCommand("updatename", "Updates a given user's username")]
        public async Task TryUpdateNameAsync(uint userId, string newName)
        {
            var user = await GLClient.Phoenix.GetFullPhoenixUserAsync(userId);

            if (user == null)
            {
                await ModifyOriginalResponseAsync(msg => msg.Content = $"Could not find any user with id **{userId}**.");
                return;
            }

            if (user.UserName == newName)
            {
                await ModifyOriginalResponseAsync(msg => msg.Content = $"{user.UserName} ({userId}) is already named `{newName}`.");
                return;
            }

            if (!await GLClient.Phoenix.TryUpdateUsername(userId, newName))
            {
                await ModifyOriginalResponseAsync(msg => msg.Content = $"Could not update username for {user.UserName} ({user.UserId})");
                return;
            }

            var backendSuccess = await GLClient.Api.UpdateNameFromPhoenixAsync(userId.ToString());

            await LogService.LogGameActionAsync(LogAction.UpdateName, Context.User.Id, userId, $"{user.UserName}:{newName}");

            var embed = new EmbedBuilder()
                .WithTitle("Username successfully updated")
                .WithDescription($"User with id **{user.UserId}** username has been updated.")
                .AddField("Previous", $"**{user.UserName}**", true)
                .AddField("Updated", $"**{newName}**", true)
                .AddField("Backend update status", backendSuccess ? "Success" : "Failed")
                .WithColor(backendSuccess ? Color.Green : Color.Orange)
                .WithFooter(footer => footer
                    .WithText($"Username change requested by {Context.User.Username}#{Context.User.Discriminator}")
                    .WithIconUrl(Context.User.GetAvatarUrl()))
                .WithCurrentTimestamp()
                .Build();

            await ModifyOriginalResponseAsync(msg => msg.Embeds = new Embed[] { embed });           
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

        [SlashCommand("kick", "Forces kick a user offline")]
        public async Task KickUserOfflineAsync(uint userId)
        {
            var user = await GLClient.Phoenix.GetPhoenixUserAsync(userId);

            if (user == null)
            {
                await ModifyOriginalResponseAsync(msg => msg.Content = $"Could not find any user with id **{userId}**.");
                return;
            }
            
            if (!await GLClient.Production.TryKickUserOfflineAsync(userId.ToString()))
            {
                await ModifyOriginalResponseAsync(msg => msg.Content = $"Failed to force {user.UserName} ({user.UserId}) offline.");
                return;
            }

            await LogService.LogGameActionAsync(LogAction.KickOffline, Context.User.Id, userId);

            var embed = new EmbedBuilder()
                .WithTitle("User was successfully kicked")
                .WithDescription($"User with id **{user.UserId}** is now offline.")
                .WithColor(Color.Green)
                .WithFooter(footer => footer
                    .WithText($"Offline kick requested by {Context.User.Username}#{Context.User.Discriminator}")
                    .WithIconUrl(Context.User.GetAvatarUrl()))
                .WithCurrentTimestamp()
                .Build();

            await ModifyOriginalResponseAsync(msg => msg.Embeds = new Embed[] { embed });  
        }

        [SlashCommand("reset", "Resets a user's progress")]
        public async Task ResetUserAsync(uint userId)
        {
            var user = await GLClient.Phoenix.GetPhoenixUserAsync(userId);

            if (user == null)
            {
                await ModifyOriginalResponseAsync(msg => msg.Content = $"Could not find any user with id **{userId}**.");
                return;
            }
            
            if (!await GLClient.Production.TryResetUserAsync(userId.ToString()))
            {
                await ModifyOriginalResponseAsync(msg => msg.Content = $"Failed to reset {user.UserName} ({user.UserId}) progress.");
                return;
            }

            await LogService.LogGameActionAsync(LogAction.Reset, Context.User.Id, userId, "Production");

            var embed = new EmbedBuilder()
                .WithTitle("Account progress successfully reset")
                .WithDescription($"Account with **{user.UserId}**'s progress has been reset.")
                .WithColor(Color.Green)
                .WithFooter(footer => footer
                    .WithText($"Progress reset requested by {Context.User.Username}#{Context.User.Discriminator}")
                    .WithIconUrl(Context.User.GetAvatarUrl()))
                .WithCurrentTimestamp()
                .Build();

            await ModifyOriginalResponseAsync(msg => msg.Embeds = new Embed[] { embed });  
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
            await GLClient.Production.CompensateChips(amount);

            await LogService.LogGameActionAsync(LogAction.Compensate, Context.User.Id, 0, $"Chips:{amount}");

            await ModifyOriginalResponseAsync(msg => msg.Content = $"Sending out compensation of {amount} chips to everyone!");
        }

        [SlashCommand("compensateitems", "Compensate items to all users")]
        public async Task CompensateItems(string sku, uint amount)
        {
            await GLClient.Production.CompensateItems(sku, amount);

            await LogService.LogGameActionAsync(LogAction.Compensate, Context.User.Id, 0, $"Items:{sku}:{amount}");

            await ModifyOriginalResponseAsync(msg => msg.Content = $"Sending out compensation of {amount}x item {sku} to everyone!");
        }

        [SlashCommand("lb", "Shows all the possible leaderboards")]
        public async Task GetLeaderboardAsync([
            Choice("Experience", "xp"), 
            Choice("Experience from attacks", "attackXp"), 
            Choice("Rivals", "rivalsWon"), 
            Choice("Chips", "chips"), 
            Choice("Chips spent", "chipsSpent"), 
            Choice("Friends helped", "friendsHelped"), 
            Choice("Gifts received", "giftsReceived"), 
            Choice("Gifts sent", "giftsSent"), 
            Choice("Stars visited", "starsVisited"), 
            Choice("Obstacles recycled", "obstaclesRecycled"), 
            Choice("Utility Used", "utilityUsed"), 
            Choice("Item", "item"),
            Choice("Warpoints", "warpoints"), 
            Choice("Alliances", "alliancewarpoints"), 
            Choice("Chips (advanced)", "advchips")] string type, string sku = "7000")
        {
            List<string> displayTexts = new() { "Failed to get information" };
            var title = "Galaxy Life Leaderboard";

            switch (type)
            {
                case "attackXp":
                    title = "Experience from attacks | Leaderboard";
                    displayTexts = (await GLClient.Api.GetXpFromAttackLeaderboard())
                        .Select(player => $"<:RedExp:1082428998182768701>{player.Level} **{player.Name}**")
                        .ToList();
                    break;
                case "rivalsWon":
                    title = "Rivals | Leaderboard";
                    displayTexts = (await GLClient.Api.GetRivalsWonLeaderboard())
                        .Select(player => $"<:pistol:1082429024963395674>{player.RivalsWon} **{player.Name}**")
                        .ToList();
                    break;
                case "chips":
                    title = "Chips | Leaderboard";
                    displayTexts = (await GLClient.Api.GetChipsLeaderboard())
                        .Select(player => $"<:Resource_Chip:943313446940868678>{player.Chips} **{player.Name}**")
                        .ToList();
                    break;
                case "chipsSpent":
                    title = "Chips Spent | Leaderboard";
                    displayTexts = (await GLClient.Api.GetChipsSpentLeaderboard())
                        .Select(player => $"<:Resource_PileOfChips:943313554742865951>{player.ChipsSpent} **{player.Name}**")
                        .ToList();
                    break;
                case "friendsHelped":
                    title = "Friends Helped | Leaderboard";
                    displayTexts = (await GLClient.Api.GetFriendsHelpedLeaderboard())
                        .Select(player => $"<:Starling_Sorry:943311734196809821>{player.FriendsHelped} **{player.Name}**")
                        .ToList();
                    break;
                case "giftsReceived":
                    title = "Gifts Received | Leaderboard";
                    displayTexts = (await GLClient.Api.GetGiftsReceivedLeaderboard())
                        .Select(player => $"<:Story_Chubi_Happy:943325609113833492>{player.GiftsReceived} **{player.Name}**")
                        .ToList();
                    break;
                case "giftsSent":
                    title = "Gifts Sent | Leaderboard";
                    displayTexts = (await GLClient.Api.GetGiftsSentLeaderboard())
                        .Select(player => $"<:Starling_Gentleman:945539138311061554>{player.GiftsSent} **{player.Name}**")
                        .ToList();
                    break;
                case "starsVisited":
                    title = "Stars Visited | Leaderboard";
                    displayTexts = (await GLClient.Api.GetStarsVisitedLeaderboard())
                        .Select(player => $"⭐{player.StarsVisited} **{player.Name}**")
                        .ToList();
                    break;
                case "obstaclesRecycled":
                    title = "Obstacles Recycled | Leaderboard";
                    displayTexts = (await GLClient.Api.GetObstaclesRecycledLeaderboard())
                        .Select(player => $"<:TouchGrass:1085581198690099281>{player.ObstaclesRecycled} **{player.Name}**")
                        .ToList();
                    break;
                case "utilityUsed":
                    title = "Utility Used | Leaderboard";
                    displayTexts = (await GLClient.Api.GetUtilityUsedLeaderboard())
                        .Select(player => $"<:Nuke:1034465682835898408>{player.UtilityUsed} **{player.Name}**")
                        .ToList();
                    break;
                case "item":
                    title = $"Item {sku} | Leaderboard";
                    displayTexts = (await GLClient.Api.GetItemLeaderboard(sku))
                        .Select(player => $"<:Item_Helmet:1084821573975945267>{player.Quantity} **{player.Name}**")
                        .ToList();
                    break;
                case "warpoints":
                    title = $"Warpoints | Leaderboard";
                    displayTexts = (await GLClient.Api.GetWarpointLeaderboard())
                        .Select(player => $"<:Starling_Frenchling:1080133173352091708>{player.Warpoints} **{player.Name}** ({player.AllianceName})")
                        .ToList();
                    break;
                case "alliancewarpoints":
                    title = $"Alliance | Leaderboard";
                    displayTexts = (await GLClient.Api.GetAllianceWarpointLeaderboard())
                        .Select(alliance => $"<:TopNotch:945458565538279515>{alliance.Warpoints} **{alliance.Name}** ({alliance.MemberCount} members)")
                        .ToList();
                    break;
                case "advchips":
                    title = $"Advanced Chips | Leaderboard";
                    displayTexts = (await GLClient.Api.GetAdvancedChipsLb())
                        .Select(player => $"<:Resource_PileOfChips:943313554742865951>{player.Chips + player.ChipsSpent - player.ChipsPurchased} **{player.Name}**")
                        .ToList();
                    break;
                default:
                case "xp":
                    title = "Experience | Leaderboard";
                    displayTexts = (await GLClient.Api.GetXpLeaderboard())
                        .Select(player => $"<:eplayerperience:920289172428849182> {player.Level} **{player.Name}**")
                        .ToList();
                    break;
            }

            if (displayTexts.Count == 0)
            {
                await ModifyOriginalResponseAsync(msg => msg.Content = $"<:Starling_WorkerHappy:943308706555260928> Servers are still loading the leaderboard, please be patient!");
                return;
            }

            for (int i = 0; i < displayTexts.Count; i++)
            {
                displayTexts[i] = $"**#{i + 1}** | {displayTexts[i]}";
            }

            var embed = new EmbedBuilder()
                .WithTitle(title)
                .WithColor(Color.Purple);

            await SendPaginatedMessageAsync(null, displayTexts, embed);

        }
    }
}
