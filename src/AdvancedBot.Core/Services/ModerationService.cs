using System.Timers;
using AdvancedBot.Core.Entities;
using AdvancedBot.Core.Entities.Enums;
using AdvancedBot.Core.Services.DataStorage;
using Discord;
using GL.NET;
using GL.NET.Entities;
using Phoenix.Api.Models;

using Timer = System.Timers.Timer;

namespace AdvancedBot.Core.Services;

public class ModerationService
{
    private readonly GLClient _gl;
    private readonly PhoenixWrapperService _phoenixWrapper;
    private readonly LogService _logs;
    private readonly BotStorage _storage;

    private readonly Timer _banTimer = new(1000 * 60 * 30);

    public ModerationService(GLClient gl, PhoenixWrapperService phoenixWrapper, LogService logs, BotStorage storage)
    {
        _gl = gl;
        _phoenixWrapper = phoenixWrapper;
        _logs = logs;
        _storage = storage;

        _banTimer.Elapsed += onBanTimer;
        _banTimer.Start();
        onBanTimer(null, null);
    }

    public async Task<ModResult> BanUserAsync(ulong discordId, uint userId, BanReasonType type, string reason, uint days = 0)
    {
        var user = await _phoenixWrapper.GetClient(discordId).V1.Users[userId].GetAsync();
        if (user is null)
        {
            return new ModResult(
                ModResultType.NotFound,
                new ResponseMessage($"Could not find any user with id **{userId}**."));
        }

        if (user.IsBanned.GetValueOrDefault())
        {
            return new ModResult(
                ModResultType.AlreadyDone,
                new ResponseMessage($"{user.Username} ({user.Id}) is already banned."),
                user);
        }

        var banRequest = new BanUserRequest() { Type = (int)type, Duration = days, Reason = reason };
        await _phoenixWrapper.GetClient(discordId).V1.Users[userId].Ban.PostAsync(banRequest);

        var embed = new EmbedBuilder()
            .WithTitle("Account successfully banned")
            .WithDescription($"**{user.Username}** ({user.Id}) has been banned!")
            .WithColor(Color.Red)
            .AddField("Player", $"{user.Username} (`{user.Id}`)", true)
            .AddField("Ban duration", days > 0 ? $"{days} days" : $"Permanent", true)
            .WithFooter(footer => footer
                .WithText($"Ban requested by moderator with id {discordId}"))
            .WithCurrentTimestamp()
            .Build();

        DateTime? banDuration = null;

        if (days > 0)
        {
            banDuration = DateTime.UtcNow.AddDays(days);
            _storage.AddTempBan(new Tempban(discordId, userId, banDuration.Value));
        }

        await _gl.Production.TryKickUserOfflineAsync(userId.ToString());
        await _logs.LogGameActionAsync(LogAction.Ban, discordId, userId, reason, banDuration);

        var message = new ResponseMessage(embeds: [embed]);
        return new ModResult(ModResultType.Success, message, user);
    }

    public async Task<ModResult> UnbanUserAsync(ulong discordId, uint userId, bool auto = false)
    {
        var user = await _phoenixWrapper.GetClient(discordId).V1.Users[userId].GetAsync();
        if (user is null)
        {
            return new ModResult(
                ModResultType.NotFound,
                new ResponseMessage($"Could not find any user with id **{userId}**."));
        }

        if (!user.IsBanned.GetValueOrDefault())
        {
            return new ModResult(
                ModResultType.AlreadyDone,
                new ResponseMessage($"{user.Username} ({user.Id}) is not banned."),
                user
            );
        }

        await _phoenixWrapper.GetClient(discordId).V1.Users[userId].Unban.PostAsync(new UnbanUserRequest());

        var extra = auto ? "Auto Unban" : "";
        await _logs.LogGameActionAsync(LogAction.Unban, discordId, userId, extra);

        var embed = new EmbedBuilder()
            .WithTitle($"{user.Username} ({user.Id}) is no longer banned in-game!")
            .WithColor(Color.Green)
            .WithFooter(footer => footer
                .WithText($"Unban requested by moderator with id {discordId}"))
            .WithCurrentTimestamp()
            .Build();

        var message = new ResponseMessage(embeds: [embed]);
        return new ModResult(ModResultType.Success, message, user);
    }

    public async Task<ModResult> AddBetaToUserAsync(ulong discordId, uint userId)
    {
        var user = await _phoenixWrapper.GetClient(discordId).V1.Users[userId].GetAsync();
        if (user is null)
        {
            return new ModResult(
                ModResultType.NotFound,
                new ResponseMessage($"Could not find any user with id **{userId}**."));
        }

        await _phoenixWrapper.GetClient(discordId).V1.Users[userId].Entitlements.PostAsync(new AddEntitlementRequest() { Type = "feature", Value = "galaxylife-beta" });
        await _logs.LogGameActionAsync(LogAction.AddBeta, discordId, userId);

        var embed = new EmbedBuilder()
            .WithTitle($"Entitlement successfully added")
            .WithDescription($"**{user.Username}** ({user.Id}) has been given 'beta' access.")
            .WithColor(Color.Green)
            .WithFooter(footer => footer
                .WithText($"Entitlement added by moderator with id {discordId}"))
            .WithCurrentTimestamp()
            .Build();

        var message = new ResponseMessage(embeds: [embed]);
        return new ModResult(ModResultType.Success, message, user);
    }

    public async Task<ModResult> RemoveBetaFromUserAsync(ulong discordId, uint userId)
    {
        var user = await _phoenixWrapper.GetClient(discordId).V1.Users[userId].GetAsync();
        if (user is null)
        {
            return new ModResult(
                ModResultType.NotFound,
                new ResponseMessage($"Could not find any user with id **{userId}**."));
        }

        await _phoenixWrapper.GetClient(discordId).V1.Users[userId].Entitlements.DeleteAsync(new RemoveEntitlementRequest() { Entitlement = "galaxylife-beta" });

        await _logs.LogGameActionAsync(LogAction.RemoveBeta, discordId, userId);

        var embed = new EmbedBuilder()
            .WithTitle($"Entitlement successfully removed")
            .WithDescription($"**{user.Username}** ({user.Id}) has been revoked 'beta' access.")
            .WithColor(Color.Red)
            .WithFooter(footer => footer
                .WithText($"Entitlement removed by moderator with id {discordId}"))
            .WithCurrentTimestamp()
            .Build();

        var message = new ResponseMessage(embeds: [embed]);
        return new ModResult(ModResultType.Success, message, user);
    }

    public async Task<ModResult> AddEmulateToUserAsync(ulong discordId, uint userId)
    {
        var user = await _phoenixWrapper.GetClient(discordId).V1.Users[userId].GetAsync();
        if (user is null)
        {
            return new ModResult(
                ModResultType.NotFound,
                new ResponseMessage($"Could not find any user with id **{userId}**."));
        }

        await _phoenixWrapper.GetClient(discordId).V1.Users[userId].Entitlements.PostAsync(new AddEntitlementRequest() { Type = "feature", Value = "galaxylife-emulate" });

        await _logs.LogGameActionAsync(LogAction.AddEmulate, discordId, userId);

        var embed = new EmbedBuilder()
            .WithTitle($"Entitlement successfully added")
            .WithDescription($"**{user.Username}** ({user.Id}) has been given 'emulate' access.")
            .WithColor(Color.Green)
            .WithFooter(footer => footer
                .WithText($"Entitlement added by moderator with id {discordId}"))
            .WithCurrentTimestamp()
            .Build();

        var message = new ResponseMessage(embeds: [embed]);
        return new ModResult(ModResultType.Success, message, user);
    }

    public async Task<ModResult> RemoveEmulateFromUserAsync(ulong discordId, uint userId)
    {
        var user = await _phoenixWrapper.GetClient(discordId).V1.Users[userId].GetAsync();
        if (user is null)
        {
            return new ModResult(
                ModResultType.NotFound,
                new ResponseMessage($"Could not find any user with id **{userId}**."));
        }

        await _phoenixWrapper.GetClient(discordId).V1.Users[userId].Entitlements.DeleteAsync(new RemoveEntitlementRequest() { Entitlement = "galaxylife-beta" });

        await _logs.LogGameActionAsync(LogAction.RemoveEmulate, discordId, userId);

        var embed = new EmbedBuilder()
            .WithTitle($"Entitlement successfully removed")
            .WithDescription($"**{user.Username}** ({user.Id}) has been revoked 'emulate' access.")
            .WithColor(Color.Green)
            .WithFooter(footer => footer
                .WithText($"Entitlement removed by moderator with id {discordId}"))
            .WithCurrentTimestamp()
            .Build();

        var message = new ResponseMessage(embeds: [embed]);
        return new ModResult(ModResultType.Success, message, user);
    }

    public async Task<ModResult> DeleteAvatarAsync(ulong discordId, uint userId)
    {
        var user = await _phoenixWrapper.GetClient(discordId).V1.Users[userId].GetAsync();
        if (user is null)
        {
            return new ModResult(
                ModResultType.NotFound,
                new ResponseMessage($"Could not find any user with id **{userId}**."));
        }

        await _phoenixWrapper.GetClient(discordId).V1.Users[userId].Avatar.DeleteAsync(new DeleteAvatarRequest());

        await _logs.LogGameActionAsync(LogAction.AvatarDeleted, discordId, userId);

        var embed = new EmbedBuilder()
            .WithTitle($"Avatar successfully removed")
            .WithDescription($"**{user.Username}** ({user.Id})'s avatar has been removed.")
            .WithColor(Color.Green)
            .WithFooter(footer => footer
                .WithText($"Avatar removed by moderator with id {discordId}"))
            .WithCurrentTimestamp()
            .Build();

        var message = new ResponseMessage(embeds: [embed]);
        return new ModResult(ModResultType.Success, message);
    }

    public async Task<ModResult> GetChipsBoughtAsync(ulong discordId, uint userId)
    {
        var user = await _gl.Api.GetUserById(userId.ToString());
        if (user is null)
        {
            return new ModResult(
                ModResultType.NotFound,
                new ResponseMessage($"Could not find any user with id **{userId}**."));
        }

        var chipsBought = await _gl.Api.GetChipsBoughtAsync(userId.ToString());
        await _logs.LogGameActionAsync(LogAction.GetChipsBought, discordId, userId);

        var description = chipsBought == 0 ?
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

        var message = new ResponseMessage(embeds: [embed]);
        return new ModResult(ModResultType.Success, message, null, user) { IntValue = chipsBought };
    }

    public async Task<ModResult> GetChipsSpentAsync(ulong discordId, uint userId)
    {
        var user = await _gl.Api.GetUserById(userId.ToString());
        if (user is null)
        {
            return new ModResult(ModResultType.NotFound, new ResponseMessage($"Could not find any user with id **{userId}**."));
        }

        var chipsSpent = await _gl.Api.GetChipsSpentAsync(userId.ToString());
        await _logs.LogGameActionAsync(LogAction.GetChipsSpent, discordId, userId);

        var description = chipsSpent == 0 ?
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

        var message = new ResponseMessage(embeds: [embed]);
        return new ModResult(ModResultType.Success, message, null, user) { IntValue = chipsSpent };
    }

    public async Task<ModResult> AddChipsAsync(ulong discordId, uint userId, int amount, bool staging = false)
    {
        var user = await _gl.Api.GetUserById(userId.ToString());
        if (user is null)
        {
            return new ModResult(
                ModResultType.NotFound,
                new ResponseMessage($"Could not find any user with id **{userId}**."));
        }

        var hasSucceeded = staging
            ? await _gl.Staging.TryAddChipsToUserAsync(userId.ToString(), amount)
            : await _gl.Production.TryAddChipsToUserAsync(userId.ToString(), amount);

        if (!hasSucceeded)
        {
            return new ModResult(
                ModResultType.BackendError,
                new ResponseMessage($"Failed to add chips to {user.Name} ({user.Id})"),
                null,
                user);
        }

        await _logs.LogGameActionAsync(LogAction.AddChips, discordId, userId, amount.ToString());

        var action = Math.Sign(amount) < 0 ? "removed" : "added";

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

        var message = new ResponseMessage(embeds: [embed]);
        return new ModResult(ModResultType.Success, message, null, user);
    }

    public async Task<ModResult> AddItemsAsync(ulong discordId, uint userId, string sku, int amount, bool staging = false)
    {
        var user = await _gl.Api.GetUserById(userId.ToString());
        if (user is null)
        {
            return new ModResult(
                ModResultType.NotFound,
                new ResponseMessage($"Could not find any user with id **{userId}**."));
        }

        var hasSucceeded = staging
            ? await _gl.Staging.TryAddItemToUserAsync(userId.ToString(), sku, amount)
            : await _gl.Production.TryAddItemToUserAsync(userId.ToString(), sku, amount);

        if (!hasSucceeded)
        {
            return new ModResult(
                ModResultType.BackendError,
                new ResponseMessage($"Failed to add item with sku {sku} to {user.Name} ({user.Id})"),
                null, user);
        }

        await _logs.LogGameActionAsync(LogAction.AddItem, discordId, userId, $"{sku}:{amount}");

        var action = Math.Sign(amount) < 0 ? "removed" : "added";

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

        var message = new ResponseMessage(embeds: [embed]);
        return new ModResult(ModResultType.Success, message, null, user);
    }

    public async Task<ModResult> AddXpAsync(ulong discordId, uint userId, int amount, bool staging = false)
    {
        var user = await _gl.Api.GetUserById(userId.ToString());
        if (user is null)
        {
            return new ModResult(
                ModResultType.NotFound,
                new ResponseMessage($"Could not find any user with id **{userId}**."));
        }

        var hasSucceeded = staging
            ? await _gl.Staging.TryAddXpToUserAsync(userId.ToString(), amount)
            : await _gl.Production.TryAddXpToUserAsync(userId.ToString(), amount);

        if (!hasSucceeded)
        {
            return new ModResult(
                ModResultType.BackendError,
                new ResponseMessage($"Failed to add xp to {user.Name} ({user.Id})"), null, user);
        }

        await _logs.LogGameActionAsync(LogAction.AddXp, discordId, userId, $"{amount}");

        var action = Math.Sign(amount) < 0 ? "removed" : "added";

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

        var message = new ResponseMessage(embeds: [embed]);
        return new ModResult(ModResultType.Success, message, null, user);
    }

    public async Task<ModResult> EnableMaintenance(ulong discordId, uint minutes)
    {
        var hasSucceeded = await _gl.Production.EnableMaintenance(minutes);
        if (!hasSucceeded)
        {
            return new ModResult(
                ModResultType.BackendError,
                new ResponseMessage($"Failed to enable maintenance for {minutes} minutes"));
        }

        await _logs.LogGameActionAsync(LogAction.EnableMaintenance, discordId, 0, minutes.ToString());

        var embed = new EmbedBuilder()
            .WithTitle($"Maintenance enabled successfully")
            .AddField($"Duration", $"{minutes} minutes", true)
            .WithColor(Color.Red)
            .WithFooter(footer => footer
                .WithText($"Enabled by moderator with id {discordId}"))
            .WithCurrentTimestamp()
            .Build();

        var message = new ResponseMessage(embeds: [embed]);
        return new ModResult(ModResultType.Success, message);
    }

    public async Task<ModResult> ReloadRules(ulong discordId, bool staging = false)
    {
        var serverText = staging ? "Staging" : "Production";
        var hasSucceeded = staging
            ? await _gl.Staging.ReloadRules()
            : await _gl.Production.ReloadRules();

        if (!hasSucceeded)
        {
            return new ModResult(
                ModResultType.BackendError,
                new ResponseMessage($"Could not reload rules on the {serverText.ToLower()} backend!"));
        }

        await _logs.LogGameActionAsync(LogAction.ReloadRules, discordId, 0, serverText);

        var embed = new EmbedBuilder()
            .WithTitle($"Rules reloaded successfully")
            .AddField($"Server", serverText)
            .WithColor(Color.Green)
            .WithFooter(footer => footer
                .WithText($"Rules reloaded by moderator with id {discordId}"))
            .WithCurrentTimestamp()
            .Build();

        var message = new ResponseMessage(embeds: [embed]);
        return new ModResult(ModResultType.Success, message);
    }

    public async Task<ModResult> RunStagingKickerAsync(ulong discordId)
    {
        var hasSucceeded = await _gl.Staging.RunKicker();
        if (!hasSucceeded)
        {
            return new ModResult(
                ModResultType.BackendError,
                new ResponseMessage($"Failed to run kicker on staging!"));
        }

        await _logs.LogGameActionAsync(LogAction.RunKicker, discordId, 0, "Staging");

        var embed = new EmbedBuilder()
            .WithTitle($"Kicker ran successfully")
            .AddField("Server", "Staging", true)
            .WithColor(Color.Green)
            .WithFooter(footer => footer
                .WithText($"Kicker requested by moderator with id {discordId}"))
            .WithCurrentTimestamp()
            .Build();

        var message = new ResponseMessage(embeds: [embed]);
        return new ModResult(ModResultType.Success, message);
    }

    public async Task<ModResult> ResetHelpsStagingAsync(ulong discordId, uint userId)
    {
        var hasSucceeded = await _gl.Staging.ResetPlanetHelps(userId.ToString());
        if (!hasSucceeded)
        {
            return new ModResult(
                ModResultType.BackendError,
                new ResponseMessage($"Failed to reset helps for user with id {userId}!"));
        }

        await _logs.LogGameActionAsync(LogAction.ResetHelps, discordId, 0, "Staging");

        var embed = new EmbedBuilder()
            .WithTitle($"Helps reset successfully")
            .AddField("Player ID", $"{userId}", true)
            .AddField("Server", "Staging", true)
            .WithColor(Color.Green)
            .WithFooter(footer => footer
                .WithText($"Kicker requested by moderator with id {discordId}"))
            .WithCurrentTimestamp()
            .Build();

        var message = new ResponseMessage(embeds: [embed]);
        return new ModResult(ModResultType.Success, message);
    }

    public async Task<ModResult> ForceWarStagingAsync(ulong discordId, string allianceA, string allianceB)
    {
        var hasSucceeded = await _gl.Staging.ForceWar(allianceA, allianceB);
        if (!hasSucceeded)
        {
            return new ModResult(
                ModResultType.BackendError,
                new ResponseMessage($"Failed to force war between **{allianceA}** and **{allianceB}**!"));
        }

        await _logs.LogGameActionAsync(LogAction.ForceWar, discordId, 0, $"Staging:{allianceA}:{allianceB}");

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

        var message = new ResponseMessage(embeds: [embed]);
        return new ModResult(ModResultType.Success, message);
    }

    public async Task<ModResult> ForceEndWarStagingAsync(ulong discordId, string allianceA, string allianceB)
    {
        var hasSucceeded = await _gl.Staging.ForceStopWar(allianceA, allianceB);
        if (!hasSucceeded)
        {
            return new ModResult(
                ModResultType.BackendError,
                new ResponseMessage($"Failed to end war between **{allianceA}** and **{allianceB}**!"));
        }

        await _logs.LogGameActionAsync(LogAction.ForceStopWar, discordId, 0, $"Staging:{allianceA}:{allianceB}");

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

        var message = new ResponseMessage(embeds: [embed]);
        return new ModResult(ModResultType.Success, message);
    }

    public async Task<ModResult<List<BattleLog>>> GetBattleLogTelemetry(ulong discordId, uint userId)
    {
        var result = await _gl.Api.GetBattlelogTelemetry(userId.ToString());
        await _logs.LogGameActionAsync(LogAction.GetTelemetry, discordId, userId, "BattleLogs");

        return new ModResult<List<BattleLog>>(result, ModResultType.Success);
    }

    public async Task<ModResult<List<Gift>>> GetGiftsTelemetry(ulong discordId, uint userId)
    {
        var result = await _gl.Api.GetGiftsTelemetry(userId.ToString());
        await _logs.LogGameActionAsync(LogAction.GetTelemetry, discordId, userId, "Gifts");

        return new ModResult<List<Gift>>(result, ModResultType.Success);
    }

    public async Task<ModResult<List<Login>>> GetLoginsTelemetry(ulong discordId, uint userId)
    {
        var result = await _gl.Api.GetLoginsTelemetry(userId.ToString());
        await _logs.LogGameActionAsync(LogAction.GetTelemetry, discordId, userId, "Gifts");

        return new ModResult<List<Login>>(result, ModResultType.Success);
    }

    public async Task<ModResult<Dictionary<string, Dictionary<string, Fingerprint>>>> GetPossibleAlts(ulong discordId, uint userId)
    {
        var result = await _gl.Api.GetFingerprint(userId.ToString());
        await _logs.LogGameActionAsync(LogAction.GetAccounts, discordId, userId);

        return new ModResult<Dictionary<string, Dictionary<string, Fingerprint>>>(result, ModResultType.Success);
    }

    private void onBanTimer(object sender, ElapsedEventArgs e)
    {
        var bans = _storage.GetTempbans();
        var bansToRemove = new List<Tempban>();

        for (var i = 0; i < bans.Count; i++)
        {
            var time = (bans[i].BanEnd - DateTime.UtcNow).TotalMinutes;

            if (time <= 0)
            {
                // no need to await
                UnbanUserAsync(bans[i].ModeratorId, bans[i].UserId, true).ConfigureAwait(false);
                bansToRemove.Add(bans[i]);
            }
        }

        // remove bans that were handled
        for (var i = 0; i < bansToRemove.Count; i++)
        {
            _storage.RemoveTempban(bansToRemove[i]);
        }
    }
}
