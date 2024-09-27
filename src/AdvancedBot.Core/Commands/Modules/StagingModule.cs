using System.Threading.Tasks;
using AdvancedBot.Core.Commands.Preconditions;
using AdvancedBot.Core.Entities;
using AdvancedBot.Core.Entities.Enums;
using AdvancedBot.Core.Services;
using Discord;
using Discord.Interactions;

namespace AdvancedBot.Core.Commands.Modules;

[Group("staging", "Handles all commands with the staging server")]
[RequireStagingList]
[DontAutoRegister]
public class StagingModule : TopModule
{
    public ModerationService ModService { get; set; }

    [SlashCommand("overview", "Shows an overview of the staging server")]
    public async Task StagingOverviewAsync()
    {
        var embed = new EmbedBuilder()
            .WithTitle("Staging Overview")
            .WithDescription("Our testing server, allowing us to test changes before releasing to production.")
            .WithColor(Color.Blue)
            .Build();

        await SendResponseMessage(new ResponseMessage(embeds: [embed]), false);

        var components = new ComponentBuilder()
          .WithButton("Reload Rules", $"rules", ButtonStyle.Primary, Emote.Parse("<:letsgo:1225454893741899886>"))
          .WithButton("Run Kicker", $"kicker", ButtonStyle.Primary, Emote.Parse("<:bonkself:1224793972271091732>"))
          .WithButton("Reset helps", $"helps", ButtonStyle.Primary, Emote.Parse("<:Item_Helmet:1084821573975945267>"))
          .WithButton("Start War", $"startwar", ButtonStyle.Danger, new Emoji("⚔"), row: 1)
          .WithButton("End War", $"endwar", ButtonStyle.Danger, Emote.Parse("<:jijijijamikaze:948038940819075092>"), row: 1);

        await ModifyOriginalResponseAsync(msg => msg.Components = components.Build());
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

    [SlashCommand("reset", "Resets a user on staging")]
    public async Task ResetStagingUser(uint userId)
    {
        var user = await GLClient.Phoenix.GetPhoenixUserAsync(userId);

        if (user == null)
        {
            await ModifyOriginalResponseAsync(msg => msg.Content = $"Could not find any user with id **{userId}**.");
            return;
        }

        if (!await GLClient.Staging.TryResetUserAsync(userId.ToString()))
        {
            await ModifyOriginalResponseAsync(msg => msg.Content = $"Could not reset {user.UserName} ({user.UserId}) progress on staging.");
            return;
        }

        await LogService.LogGameActionAsync(LogAction.Reset, Context.User.Id, userId, "Staging");

        var embed = new EmbedBuilder()
            .WithTitle("Account staging progress successfully reset")
            .WithDescription($"Account with **{user.UserId}**'s progress has been reset on staging")
            .WithColor(Color.Green)
            .WithFooter(footer => footer
                .WithText($"Staging progress reset requested by {Context.User.Username}#{Context.User.Discriminator}")
                .WithIconUrl(Context.User.GetDisplayAvatarUrl()))
            .WithCurrentTimestamp()
            .Build();

        await ModifyOriginalResponseAsync(msg => msg.Embeds = new Embed[] { embed });
    }

    [SlashCommand("addchips", "Adds chips to a user")]
    public async Task AddChipsToUserAsync(uint userId, int amount)
    {
        var result = await ModService.AddChipsAsync(Context.User.Id, userId, amount, true);
        await SendResponseMessage(result.Message, false);
    }

    [SlashCommand("additem", "Adds an item a user")]
    public async Task AddItemsToUserAsync(uint userId, string sku, int amount)
    {
        var result = await ModService.AddItemsAsync(Context.User.Id, userId, sku, amount, true);
        await SendResponseMessage(result.Message, false);
    }

    [SlashCommand("addxp", "Adds xp to a user")]
    public async Task AddXpToUserAsync(uint userId, int amount)
    {
        var result = await ModService.AddXpAsync(Context.User.Id, userId, amount, true);
        await SendResponseMessage(result.Message, false);
    }

    [SlashCommand("restart", "Restarts staging server")]
    public async Task RestartStagingAsync()
    {
        bool result = await GLClient.Staging.RestartServer();

        if (!result)
        {
            await ModifyOriginalResponseAsync(msg => msg.Content = "Failed lol");
        }

        var embed = new EmbedBuilder()
            .WithTitle($"Restarted Staging")
            .WithColor(Color.Green)
            .Build();

        await ModifyOriginalResponseAsync(msg => msg.Embeds = new Embed[] { embed });
    }
}
