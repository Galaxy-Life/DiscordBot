using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;
using AdvancedBot.Core.Entities;
using AdvancedBot.Core.Entities.Enums;
using AdvancedBot.Core.Services.DataStorage;
using Discord;
using GL.NET;
using GL.NET.Entities;

namespace AdvancedBot.Core.Services
{
    public class ModerationService
    {
        private readonly GLClient gl;
        private readonly LogService logs;
        private readonly BotStorage storage;

        private readonly Timer banTimer = new(1000 * 60 * 30);

        public ModerationService(GLClient gl, LogService logs, BotStorage storage)
        {
            this.gl = gl;
            this.logs = logs;
            this.storage = storage;

            banTimer.Elapsed += onBanTimer;
            banTimer.Start();
            onBanTimer(null, null);
        }

        public async Task<ModResult> BanUserAsync(ulong discordId, uint userId, string reason, uint days = 0)
        {
            var user = await gl.Phoenix.GetPhoenixUserAsync(userId);

            if (user == null)
            {
                return new ModResult(ModResultType.NotFound, new ResponseMessage($"Could not find any user with id **{userId}**."));
            }

            if (user.Role == PhoenixRole.Banned)
            {
                return new ModResult(ModResultType.AlreadyDone, new ResponseMessage($"{user.UserName} ({user.UserId}) is already banned."), user);
            }

            if (!await gl.Phoenix.TryBanUser(userId, reason))
            {
                return new ModResult(ModResultType.BackendError, new ResponseMessage($"Failed to ban {user.UserName} ({user.UserId})"), user);
            }

            var embed = new EmbedBuilder()
                .WithTitle("Account successfully banned")
                .WithDescription($"**{user.UserName}** ({user.UserId}) has been banned!")
                .WithColor(Color.Red)
                .AddField("Player", $"{user.UserName} (`{user.UserId}`)", true)
                .AddField("Ban duration", days > 0 ? $"{days} days" : $"Permanent", true)
                .WithFooter(footer => footer
                    .WithText($"Ban requested by moderator with id {discordId}"))
                .WithCurrentTimestamp()
                .Build();

            DateTime? banDuration = null;

            if (days > 0)
            {
                banDuration = DateTime.UtcNow.AddDays(days);
                storage.AddTempBan(new Tempban(discordId, userId, banDuration.Value));
            }

            await gl.Production.TryKickUserOfflineAsync(userId.ToString());
            await logs.LogGameActionAsync(LogAction.Ban, discordId, userId, reason, banDuration);

            var message = new ResponseMessage(embeds: new Embed[] { embed });
            return new ModResult(ModResultType.Success, message, user);
        }

        public async Task<ModResult> UnbanUserAsync(ulong discordId, uint userId, bool auto = false)
        {
            var user = await gl.Phoenix.GetPhoenixUserAsync(userId);

            if (user == null)
            {
                return new ModResult(ModResultType.NotFound, new ResponseMessage($"Could not find any user with id **{userId}**."));
            }

            if (user.Role != PhoenixRole.Banned)
            {
                return new ModResult(ModResultType.AlreadyDone, new ResponseMessage($"{user.UserName} ({user.UserId}) is not banned."), user);
            }

            if (!await gl.Phoenix.TryUnbanUser(userId))
            {
                return new ModResult(ModResultType.BackendError, new ResponseMessage($"Failed to unban {user.UserName} ({user.UserId})"), user);
            }

            string extra = auto ? "Auto Unban" : "";
            await logs.LogGameActionAsync(LogAction.Unban, discordId, userId, extra);

            var embed = new EmbedBuilder()
                .WithTitle($"{user.UserName} ({user.UserId}) is no longer banned in-game!")
                .WithColor(Color.Green)
                .WithFooter(footer => footer
                    .WithText($"Unban requested by moderator with id {discordId}"))
                .WithCurrentTimestamp()
                .Build();

            var message = new ResponseMessage(embeds: new Embed[] { embed });
            return new ModResult(ModResultType.Success, message, user);
        }

        public async Task<ModResult> AddBetaToUserAsync(ulong discordId, uint userId)
        {
            var user = await gl.Phoenix.GetPhoenixUserAsync(userId);

            if (user == null)
            {
                return new ModResult(ModResultType.NotFound, new ResponseMessage($"Could not find any user with id **{userId}**."));
            }

            if (!await gl.Phoenix.AddGlBeta(userId))
            {
                return new ModResult(ModResultType.BackendError, new ResponseMessage($"Failed to add beta access to {user.UserName} ({user.UserId})"));
            }

            await logs.LogGameActionAsync(LogAction.AddBeta, discordId, userId);

            var embed = new EmbedBuilder()
                .WithTitle($"Entitlement successfully added")
                .WithDescription($"**{user.UserName}** ({user.UserId}) has been given 'beta' access.")
                .WithColor(Color.Green)
                .WithFooter(footer => footer
                    .WithText($"Entitlement added by moderator with id {discordId}"))
                .WithCurrentTimestamp()
                .Build();

            var message = new ResponseMessage(embeds: new Embed[] { embed });
            return new ModResult(ModResultType.Success, message, user);
        }

        public async Task<ModResult> RemoveBetaFromUserAsync(ulong discordId, uint userId)
        {
            var user = await gl.Phoenix.GetPhoenixUserAsync(userId);

            if (user == null)
            {
                return new ModResult(ModResultType.NotFound, new ResponseMessage($"Could not find any user with id **{userId}**."));
            }

            if (!await gl.Phoenix.RemoveGlBeta(userId))
            {
                return new ModResult(ModResultType.BackendError, new ResponseMessage($"Failed to remove beta access from {user.UserName} ({user.UserId})"));
            }

            await logs.LogGameActionAsync(LogAction.RemoveBeta, discordId, userId);

            var embed = new EmbedBuilder()
                .WithTitle($"Entitlement successfully removed")
                .WithDescription($"**{user.UserName}** ({user.UserId}) has been revoked 'beta' access.")
                .WithColor(Color.Red)
                .WithFooter(footer => footer
                    .WithText($"Entitlement removed by moderator with id {discordId}"))
                .WithCurrentTimestamp()
                .Build();

            var message = new ResponseMessage(embeds: new Embed[] { embed });
            return new ModResult(ModResultType.Success, message, user);
        }

        public async Task<ModResult> AddEmulateToUserAsync(ulong discordId, uint userId)
        {
            var user = await gl.Phoenix.GetPhoenixUserAsync(userId);

            if (user == null)
            {
                return new ModResult(ModResultType.NotFound, new ResponseMessage($"Could not find any user with id **{userId}**."));
            }

            if (!await gl.Phoenix.AddEmulate(userId))
            {
                return new ModResult(ModResultType.BackendError, new ResponseMessage($"Failed to add emulate access to {user.UserName} ({user.UserId})"));
            }

            await logs.LogGameActionAsync(LogAction.AddEmulate, discordId, userId);

            var embed = new EmbedBuilder()
                .WithTitle($"Entitlement successfully added")
                .WithDescription($"**{user.UserName}** ({user.UserId}) has been given 'emulate' access.")
                .WithColor(Color.Green)
                .WithFooter(footer => footer
                    .WithText($"Entitlement added by moderator with id {discordId}"))
                .WithCurrentTimestamp()
                .Build();

            var message = new ResponseMessage(embeds: new Embed[] { embed });
            return new ModResult(ModResultType.Success, message, user);
        }

        public async Task<ModResult> RemoveEmulateFromUserAsync(ulong discordId, uint userId)
        {
            var user = await gl.Phoenix.GetPhoenixUserAsync(userId);

            if (user == null)
            {
                return new ModResult(ModResultType.NotFound, new ResponseMessage($"Could not find any user with id **{userId}**."));
            }

            if (!await gl.Phoenix.RemoveGlBeta(userId))
            {
                return new ModResult(ModResultType.BackendError, new ResponseMessage($"Failed to remove emulate access from {user.UserName} ({user.UserId})"));
            }

            await logs.LogGameActionAsync(LogAction.RemoveEmulate, discordId, userId);

            var embed = new EmbedBuilder()
                .WithTitle($"Entitlement successfully removed")
                .WithDescription($"**{user.UserName}** ({user.UserId}) has been revoked 'emulate' access.")
                .WithColor(Color.Green)
                .WithFooter(footer => footer
                    .WithText($"Entitlement removed by moderator with id {discordId}"))
                .WithCurrentTimestamp()
                .Build();

            var message = new ResponseMessage(embeds: new Embed[] { embed });
            return new ModResult(ModResultType.Success, message, user);
        }

        public async Task<ModResult> GiveRoleAsync(ulong discordId, uint userId, PhoenixRole newRole)
        {
            var user = await gl.Phoenix.GetPhoenixUserAsync(userId);

            if (user == null)
            {
                return new ModResult(ModResultType.NotFound, new ResponseMessage($"Could not find any user with id **{userId}**."));
            }

            string roleText = newRole == PhoenixRole.Donator ? "a Donator"
                : newRole == PhoenixRole.Staff ? "a Staff Member"
                : newRole == PhoenixRole.Administrator ? "an Admin"
                : newRole.ToString();

            if (user.Role == newRole)
            {
                return new ModResult(ModResultType.AlreadyDone, new ResponseMessage($"User is already {roleText}!"));
            }

            if (!await gl.Phoenix.GiveRoleAsync(userId, newRole))
            {
                return new ModResult(ModResultType.BackendError, new ResponseMessage($"Failed to give {newRole} to {user.UserName} ({user.UserId})"), user);
            }

            await logs.LogGameActionAsync(LogAction.GiveRole, discordId, userId, newRole.ToString());

            var embed = new EmbedBuilder()
                .WithTitle($"User role successfully updated")
                .WithDescription($"**{user.UserName}** ({user.UserId}) role has been updated.")
                .AddField("Previous", $"{user.Role}", true)
                .AddField("Updated", $"{newRole}", true)
                .WithColor(Color.Green)
                .WithFooter(footer => footer
                    .WithText($"Role given by moderator with id {discordId}"))
                .WithCurrentTimestamp()
                .Build();

            var message = new ResponseMessage(embeds: new Embed[] { embed });
            return new ModResult(ModResultType.Success, message, user);
        }

        public async Task<ModResult> GetChipsBoughtAsync(ulong discordId, uint userId)
        {
            var user = await gl.Api.GetUserById(userId.ToString());

            if (user == null)
            {
                return new ModResult(ModResultType.NotFound, new ResponseMessage($"Could not find any user with id **{userId}**."));
            }

            int chipsBought = await gl.Api.GetChipsBoughtAsync(userId.ToString());
            await logs.LogGameActionAsync(LogAction.GetChipsBought, discordId, userId);

            string description = chipsBought == 0 ?
                  $"This player hasn't bought any chips."
                : $"This player has bought **{chipsBought}** chips.";

            var embed = new EmbedBuilder()
                .WithTitle($"{user.Name} ({user.Id})")
                .WithDescription(description)
                .WithColor(Color.DarkBlue)
                .WithFooter(footer => footer
                    .WithText($"Requested by moderator with id {discordId}"))
                .WithCurrentTimestamp()
                .Build();

            var message = new ResponseMessage(embeds: new Embed[] { embed });
            return new ModResult(ModResultType.Success, message, null, user) { IntValue = chipsBought };
        }

        public async Task<ModResult> GetChipsSpentAsync(ulong discordId, uint userId)
        {
            var user = await gl.Api.GetUserById(userId.ToString());

            if (user == null)
            {
                return new ModResult(ModResultType.NotFound, new ResponseMessage($"Could not find any user with id **{userId}**."));
            }

            int chipsSpent = await gl.Api.GetChipsSpentAsync(userId.ToString());
            await logs.LogGameActionAsync(LogAction.GetChipsSpent, discordId, userId);

            string description = chipsSpent == 0 ?
                  $"This player hasn't spent any chips."
                : $"This player has spent **{chipsSpent}** chips.";

            var embed = new EmbedBuilder()
                .WithTitle($"{user.Name} ({user.Id})")
                .WithDescription(description)
                .WithColor(Color.DarkBlue)
                .WithFooter(footer => footer
                    .WithText($"Requested by moderator with id {discordId}"))
                .WithCurrentTimestamp()
                .Build();

            var message = new ResponseMessage(embeds: new Embed[] { embed });
            return new ModResult(ModResultType.Success, message, null, user) { IntValue = chipsSpent };
        }

        public async Task<ModResult> AddChipsAsync(ulong discordId, uint userId, int amount, bool staging = false)
        {
            var user = await gl.Api.GetUserById(userId.ToString());

            if (user == null)
            {
                return new ModResult(ModResultType.NotFound, new ResponseMessage($"Could not find any user with id **{userId}**."));
            }

            bool success = staging ? await gl.Staging.TryAddChipsToUserAsync(userId.ToString(), amount) : await gl.Production.TryAddChipsToUserAsync(userId.ToString(), amount);

            if (!success)
            {
                return new ModResult(ModResultType.BackendError, new ResponseMessage($"Failed to add chips to {user.Name} ({user.Id})"), null, user);
            }

            await logs.LogGameActionAsync(LogAction.AddChips, discordId, userId, amount.ToString());

            string action = Math.Sign(amount) < 0 ? "removed" : "added";

            var embed = new EmbedBuilder()
                .WithTitle($"Chips {action} successfully")
                .AddField($"Player", $"{user.Name} (`{user.Id}`)", true)
                .AddField($"Chips {action}", $"{amount}", true)
                .AddField($"Server", staging ? "Staging" : "Production")
                .WithColor(Math.Sign(amount) < 0 ? Color.Red : Color.Green)
                .WithFooter(footer => footer
                    .WithText($"Chips {action} by moderator with id {discordId}"))
                .WithCurrentTimestamp()
                .Build();

            var message = new ResponseMessage(embeds: new Embed[] { embed });
            return new ModResult(ModResultType.Success, message, null, user);
        }

        public async Task<ModResult> AddItemsAsync(ulong discordId, uint userId, string sku, int amount, bool staging = false)
        {
            var user = await gl.Api.GetUserById(userId.ToString());

            if (user == null)
            {
                return new ModResult(ModResultType.NotFound, new ResponseMessage($"Could not find any user with id **{userId}**."));
            }

            bool success = staging ? await gl.Staging.TryAddItemToUserAsync(userId.ToString(), sku, amount) : await gl.Production.TryAddItemToUserAsync(userId.ToString(), sku, amount);

            if (!success)
            {
                return new ModResult(ModResultType.BackendError, new ResponseMessage($"Failed to add item with sku {sku} to {user.Name} ({user.Id})"), null, user);
            }

            await logs.LogGameActionAsync(LogAction.AddItem, discordId, userId, $"{sku}:{amount}");

            string action = Math.Sign(amount) < 0 ? "removed" : "added";

            var embed = new EmbedBuilder()
                .WithTitle($"Item {action} successfully")
                .AddField("Player", $"{user.Name} (`{user.Id}`)", true)
                .AddField("Item SKU", $"{sku}", true)
                .AddField($"Quantity {action}", $"{amount}", true)
                .AddField($"Server", staging ? "Staging" : "Production")
                .WithColor(Math.Sign(amount) < 0 ? Color.Red : Color.Green)
                .WithFooter(footer => footer
                    .WithText($"Item(s) {action} by moderator with id {discordId}"))
                .WithCurrentTimestamp()
                .Build();

            var message = new ResponseMessage(embeds: new Embed[] { embed });
            return new ModResult(ModResultType.Success, message, null, user);
        }

        public async Task<ModResult> AddXpAsync(ulong discordId, uint userId, int amount, bool staging = false)
        {
            var user = await gl.Api.GetUserById(userId.ToString());

            if (user == null)
            {
                return new ModResult(ModResultType.NotFound, new ResponseMessage($"Could not find any user with id **{userId}**."));
            }

            bool success = staging ? await gl.Staging.TryAddXpToUserAsync(userId.ToString(), amount) : await gl.Production.TryAddXpToUserAsync(userId.ToString(), amount);
            if (!success)
            {
                return new ModResult(ModResultType.BackendError, new ResponseMessage($"Failed to add xp to {user.Name} ({user.Id})"), null, user);
            }

            await logs.LogGameActionAsync(LogAction.AddXp, discordId, userId, $"{amount}");

            string action = Math.Sign(amount) < 0 ? "removed" : "added";

            var embed = new EmbedBuilder()
                .WithTitle($"Experience {action} successfully")
                .AddField("Player", $"{user.Name} (`{user.Id}`)", true)
                .AddField($"Experience {action}", $"{amount}", true)
                .AddField($"Server", staging ? "Staging" : "Production")
                .WithColor(Math.Sign(amount) < 0 ? Color.Red : Color.Green)
                .WithFooter(footer => footer
                    .WithText($"Experience {action} by moderator with id {discordId}"))
                .WithCurrentTimestamp()
                .Build();

            var message = new ResponseMessage(embeds: new Embed[] { embed });
            return new ModResult(ModResultType.Success, message, null, user);
        }

        public async Task<ModResult> EnableMaintenance(ulong discordId, uint minutes)
        {
            bool success = await gl.Production.EnableMaintenance(minutes);

            if (!success)
            {
                return new ModResult(ModResultType.BackendError, new ResponseMessage($"Failed to enable maintenance for {minutes} minutes"));
            }

            await logs.LogGameActionAsync(LogAction.EnableMaintenance, discordId, 0, minutes.ToString());

            var embed = new EmbedBuilder()
                .WithTitle($"Maintenance enabled successfully")
                .AddField($"Duration", $"{minutes} minutes", true)
                .WithColor(Color.Red)
                .WithFooter(footer => footer
                    .WithText($"Enabled by moderator with id {discordId}"))
                .WithCurrentTimestamp()
                .Build();

            var message = new ResponseMessage(embeds: new Embed[] { embed });
            return new ModResult(ModResultType.Success, message);
        }

        public async Task<ModResult> ReloadRules(ulong discordId, bool staging = false)
        {
            bool success = staging ? await gl.Staging.ReloadRules() : await gl.Production.ReloadRules();
            string serverText = staging ? "Staging" : "Production";

            if (!success)
            {
                return new ModResult(ModResultType.BackendError, new ResponseMessage($"Could not reload rules on the {serverText.ToLower()} backend!"));
            }

            await logs.LogGameActionAsync(LogAction.ReloadRules, discordId, 0, serverText);

            var embed = new EmbedBuilder()
                .WithTitle($"Rules reloaded successfully")
                .AddField($"Server", serverText)
                .WithColor(Color.Green)
                .WithFooter(footer => footer
                    .WithText($"Rules reloaded by moderator with id {discordId}"))
                .WithCurrentTimestamp()
                .Build();

            var message = new ResponseMessage(embeds: new Embed[] { embed });
            return new ModResult(ModResultType.Success, message);
        }

        public async Task<ModResult> RunStagingKickerAsync(ulong discordId)
        {
            bool success = await gl.Staging.RunKicker();

            if (!success)
            {
                return new ModResult(ModResultType.BackendError, new ResponseMessage($"Failed to run kicker on staging!"));
            }

            await logs.LogGameActionAsync(LogAction.RunKicker, discordId, 0, "Staging");

            var embed = new EmbedBuilder()
                .WithTitle($"Kicker ran successfully")
                .AddField("Server", "Staging", true)
                .WithColor(Color.Green)
                .WithFooter(footer => footer
                    .WithText($"Kicker requested by moderator with id {discordId}"))
                .WithCurrentTimestamp()
                .Build();

            var message = new ResponseMessage(embeds: new Embed[] { embed });
            return new ModResult(ModResultType.Success, message);
        }

        public async Task<ModResult> ResetHelpsStagingAsync(ulong discordId, uint userId)
        {
            bool success = await gl.Staging.ResetPlanetHelps(userId.ToString());

            if (!success)
            {
                return new ModResult(ModResultType.BackendError, new ResponseMessage($"Failed to reset helps for {userId}!"));
            }

            await logs.LogGameActionAsync(LogAction.ResetHelps, discordId, 0, "Staging");

            var embed = new EmbedBuilder()
                .WithTitle($"Helps reset successfully")
                .AddField("Player ID", $"{userId}", true)
                .AddField("Server", "Staging", true)
                .WithColor(Color.Green)
                .WithFooter(footer => footer
                    .WithText($"Kicker requested by moderator with id {discordId}"))
                .WithCurrentTimestamp()
                .Build();

            var message = new ResponseMessage(embeds: new Embed[] { embed });
            return new ModResult(ModResultType.Success, message);
        }

        public async Task<ModResult> ForceWarStagingAsync(ulong discordId, string allianceA, string allianceB)
        {
            bool success = await gl.Staging.ForceWar(allianceA, allianceB);

            if (!success)
            {
                return new ModResult(ModResultType.BackendError, new ResponseMessage($"Failed to force war between **{allianceA}** and **{allianceB}**!"));
            }

            await logs.LogGameActionAsync(LogAction.ForceWar, discordId, 0, $"Staging:{allianceA}:{allianceB}");

            var embed = new EmbedBuilder()
                .WithTitle($"War declared successfully")
                .WithDescription("Both alliances are now at war against each other on staging!")
                .AddField("Alliance", $"{allianceA}", true)
                .AddField("vs", $"\u200B", true)
                .AddField("Alliance", $"{allianceB}", true)
                .WithColor(Color.Red)
                .WithFooter(footer => footer
                    .WithText($"War declared by moderator with id {discordId}"))
                .WithCurrentTimestamp()
                .Build();

            var message = new ResponseMessage(embeds: new Embed[] { embed });
            return new ModResult(ModResultType.Success, message);
        }

        public async Task<ModResult> ForceEndWarStagingAsync(ulong discordId, string allianceA, string allianceB)
        {
            bool success = await gl.Staging.ForceStopWar(allianceA, allianceB);

            if (!success)
            {
                return new ModResult(ModResultType.BackendError, new ResponseMessage($"Failed to end war between **{allianceA}** and **{allianceB}**!"));
            }

            await logs.LogGameActionAsync(LogAction.ForceStopWar, discordId, 0, $"Staging:{allianceA}:{allianceB}");

            var embed = new EmbedBuilder()
                .WithTitle($"War ended successfully")
                .WithDescription("Both alliances are no longer at war against each other on staging!")
                .AddField("Alliance", $"{allianceA}", true)
                .AddField("vs", $"\u200B", true)
                .AddField("Alliance", $"{allianceB}", true)
                .WithColor(Color.Red)
                .WithFooter(footer => footer
                    .WithText($"War ended by moderator with id {discordId}"))
                .WithCurrentTimestamp()
                .Build();

            var message = new ResponseMessage(embeds: new Embed[] { embed });
            return new ModResult(ModResultType.Success, message);
        }

        public async Task<ModResult<List<BattleLog>>> GetBattleLogTelemetry(ulong discordId, uint userId)
        {
            var result = await gl.Api.GetBattlelogTelemetry(userId.ToString());
            await logs.LogGameActionAsync(LogAction.GetTelemetry, discordId, userId, "BattleLogs");

            return new ModResult<List<BattleLog>>(result, ModResultType.Success);
        }

        public async Task<ModResult<List<Gift>>> GetGiftsTelemetry(ulong discordId, uint userId)
        {
            var result = await gl.Api.GetGiftsTelemetry(userId.ToString());
            await logs.LogGameActionAsync(LogAction.GetTelemetry, discordId, userId, "Gifts");

            return new ModResult<List<Gift>>(result, ModResultType.Success);
        }

        public async Task<ModResult<List<Login>>> GetLoginsTelemetry(ulong discordId, uint userId)
        {
            var result = await gl.Api.GetLoginsTelemetry(userId.ToString());
            await logs.LogGameActionAsync(LogAction.GetTelemetry, discordId, userId, "Gifts");

            return new ModResult<List<Login>>(result, ModResultType.Success);
        }

        public async Task<ModResult<Dictionary<string, Dictionary<string, Fingerprint>>>> GetPossibleAlts(ulong discordId, uint userId)
        {
            var result = await gl.Api.GetFingerprint(userId.ToString());
            await logs.LogGameActionAsync(LogAction.GetAccounts, discordId, userId);

            return new ModResult<Dictionary<string, Dictionary<string, Fingerprint>>>(result, ModResultType.Success);
        }

        private void onBanTimer(object sender, ElapsedEventArgs e)
        {
            var bans = storage.GetTempbans();
            var bansToRemove = new List<Tempban>();

            for (int i = 0; i < bans.Count; i++)
            {
                double time = (bans[i].BanEnd - DateTime.UtcNow).TotalMinutes;

                if (time <= 0)
                {
                    // no need to await
                    UnbanUserAsync(bans[i].ModeratorId, bans[i].UserId, true).ConfigureAwait(false);
                    bansToRemove.Add(bans[i]);
                }
            }

            // remove bans that were handled
            for (int i = 0; i < bansToRemove.Count; i++)
            {
                storage.RemoveTempban(bansToRemove[i]);
            }
        }
    }
}
