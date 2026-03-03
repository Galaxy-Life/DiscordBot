using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AdvancedBot.Core.Entities;
using AdvancedBot.Core.Entities.Enums;
using AdvancedBot.Core.Services;
using AdvancedBot.Core.Services.DataStorage;
using Discord;
using Discord.Interactions;
using GL.NET;
using GL.NET.Entities;
using Phoenix.Api.Models;

namespace AdvancedBot.Core.Commands;

public class TopModule : InteractionModuleBase<SocketInteractionContext>
{
    public AccountService Accounts { get; set; }

    public GLClient GLClient { get; set; }

    public PhoenixWrapperService PhoenixWrapper { get; set; }

    public PaginatorService Paginator { get; set; }

    public LogService LogService { get; set; }

    public PhoenixCredentials Credentials { get; set; }


    public readonly List<ulong> PowerUsers =
    [
        202095042372829184, // svr333
        942849642931032164, // lifecoder
        180676108088246272, // lodethebig
        356060824223350784, // andyvv.
        275698828974489612, // magniolya
        424689465450037278  // bryan
    ];

    public readonly List<ulong> SemiPowerUsers = [];

    public override async Task BeforeExecuteAsync(ICommandInfo command)
    {
        if (command.SupportsWildCards || command.Name.Contains(':') || command.Name.Contains(',')) return;

        await DeferAsync();
    }

    public override Task AfterExecuteAsync(ICommandInfo command)
        => Task.CompletedTask;


    protected async Task SendPaginatedMessageAsync(IEnumerable<EmbedField> displayFields, IEnumerable<string> displayTexts, EmbedBuilder templateEmbed)
    {
        var displayItems = 0;

        if (displayTexts != null)
        {
            templateEmbed.WithDescription(string.Join("\n", displayTexts.Take(10)));
            displayItems = displayTexts.Count();
        }
        else if (displayFields != null)
        {
            displayItems = displayFields.Count();
            var fields = displayFields.Take(10).ToArray();

            for (var i = 0; i < fields.Length; i++)
            {
                templateEmbed.AddField(fields[i].Name, fields[i].Value, fields[i].Inline);
            }
        }

        if (displayItems == 0)
        {
            await Context.Interaction.ModifyOriginalResponseAsync(x => x.Content = "Nothing to display here");
            return;
        }
        else if (displayItems > 10)
        {
            templateEmbed.WithTitle($"{templateEmbed.Title} (Page 1)");
        }

        await Paginator.HandleNewPaginatedMessageAsync(Context, displayFields, displayTexts, templateEmbed.Build());
        await Task.Delay(200);
    }

    protected async Task SendResponseMessage(ResponseMessage response, bool followup)
    {
        if (followup)
        {
            await FollowupAsync(response.Content, response.Embeds, ephemeral: response.Ephemeral);
            return;
        }

        if (!string.IsNullOrEmpty(response.Content))
        {
            await ModifyOriginalResponseAsync(msg => msg.Content = response.Content);
        }
        if (response.Embeds != null)
        {
            await ModifyOriginalResponseAsync(msg => msg.Embeds = response.Embeds);
        }
    }

    protected MessageComponent CreateDefaultComponents(string username, string userId, string alliance, bool isBanned)
    {
        var components = new ComponentBuilder();
        // component magic, cannot handle empty values
        alliance = alliance == " " ? null : alliance;

        components.WithButton("Profile", $"profile:{username},{userId}", ButtonStyle.Primary, Emote.Parse("<:AFCElderby:943325489009934368>"));
        components.WithButton("Stats", $"stats:{username},{userId}", ButtonStyle.Primary, Emote.Parse("<:AACLooter:943311525320482877>"));

        if (!string.IsNullOrEmpty(alliance))
        {
            components.WithButton("Alliance", $"alliance:{alliance}", ButtonStyle.Primary, Emote.Parse("<:AFECounselor_Mobius:1082315024829272154>"));
            components.WithButton("Members", $"members:{alliance}", ButtonStyle.Primary, Emote.Parse("<:Major_Wor:944250193279324171>"));
        }

        if (PowerUsers.Contains(Context.User.Id))
        {
            components.WithButton("Moderation", $"moderation:{username},{userId},{alliance ?? " "},{isBanned}", ButtonStyle.Secondary, new Emoji("➕"));
        }
        else if (SemiPowerUsers.Contains(Context.User.Id))
        {
            components.WithButton("Moderation", $"semimoderation:{username},{userId},{alliance ?? " "}", ButtonStyle.Secondary, new Emoji("➕"));
        }

        return components.Build();
    }

    protected static MessageComponent CreateModerationComponents(string username, string userId, string alliance, bool isBanned)
    {
        var components = new ComponentBuilder();

        components.WithButton("Add Beta", $"addbeta:{userId}", ButtonStyle.Success, Emote.Parse("<:Starling_Nukeling:1079151310538022963>"), row: 0);
        components.WithButton("Remove Beta", $"removebeta:{userId}", ButtonStyle.Danger, Emote.Parse("<:magmalot:1229102605108777002>"), row: 0);
        components.WithButton("Give Role", $"giverole:{userId}", ButtonStyle.Primary, Emote.Parse("<:Starling_Swag:943310403658715196>"), disabled: true, row: 0);
        components.WithButton("Chips bought", $"chipsbought:{userId}", ButtonStyle.Primary, Emote.Parse("<:Resource_PileOfChips:943313554742865951>"), row: 0);

        components.WithButton("Back", $"back:{username},{userId},{alliance},{isBanned}", ButtonStyle.Secondary, new Emoji("↩️"), row: 1);
        components.WithButton("Add Chips", $"addchips:{username},{userId}", ButtonStyle.Success, Emote.Parse("<:Resource_Chip:943313446940868678>"), row: 1);
        components.WithButton("Add Item", $"additem:{username},{userId}", ButtonStyle.Success, Emote.Parse("<:Item_Toolbox:1084821705316376576>"), row: 1);
        components.WithButton("Add Xp", $"addxp:{username},{userId}", ButtonStyle.Success, Emote.Parse("<:Resource_Experience:943325952212090880>"), row: 1);

        if (isBanned)
        {
            components.WithButton("Unban", $"unban:{username},{userId}", ButtonStyle.Success, Emote.Parse("<:Starling_Happy:946859412763578419>"), row: 1);
        }
        else
        {
            components.WithButton("Ban", $"ban:{username},{userId}", ButtonStyle.Danger, Emote.Parse("<:Story_LuckNorris:945537412199743488>"), row: 1);
        }

        return components.Build();
    }

    protected static MessageComponent CreateSemiModerationComponents(string username, string userId, string alliance)
    {
        var components = new ComponentBuilder();

        components.WithButton("Back", $"semiback:{username},{userId},{alliance}", ButtonStyle.Secondary, new Emoji("↩️"));
        components.WithButton("Chips bought", $"chipsbought:{userId}", ButtonStyle.Primary, Emote.Parse("<:Resource_PileOfChips:943313554742865951>"));

        components.WithButton("Add Chips", $"addchips:{username},{userId}", ButtonStyle.Success, Emote.Parse("<:Resource_Chip:943313446940868678>"));
        components.WithButton("Add Item", $"additem:{username},{userId}", ButtonStyle.Success, Emote.Parse("<:Item_Toolbox:1084821705316376576>"));
        components.WithButton("Add Xp", $"addxp:{username},{userId}", ButtonStyle.Success, Emote.Parse("<:Resource_Experience:943325952212090880>"));

        return components.Build();
    }

    protected async Task<User> GetUserByInput(string input)
    {
        if (string.IsNullOrEmpty(input)) input = Context.User.Username;

        var profile = await GLClient.Api.GetUserById(input);

        profile ??= await GLClient.Api.GetUserByName(input);

        return profile;
    }

    protected async Task<UserDto?> GetPhoenixUser(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            input = Context.User.Username;
        }

        UserDto? user = null;

        // extra check to see if all characters were numbers
        if (long.TryParse(input, out var userId))
        {
            user = await PhoenixWrapper.GetClient(Context.User.Id).V1.Users[userId].GetAsync();
        }

        // Get regular user to know its id
        user ??= await PhoenixWrapper.GetClient(Context.User.Id).V1.Users.ByUsername[input].GetAsync();

        return user;
    }

    protected async Task<UserDetailsDto?> GetFullPhoenixUser(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            input = Context.User.Username;
        }

        UserDetailsDto? detailedUser = null;

        // extra check to see if all characters were numbers
        if (long.TryParse(input, out var userId))
        {
            detailedUser = await PhoenixWrapper.GetClient(Context.User.Id).V1.Users[userId].Details.GetAsync();
        }

        // Not found, try fetching by name
        if (detailedUser == null)
        {
            // Get regular user to know its id
            var user = await PhoenixWrapper.GetClient(Context.User.Id).V1.Users.ByUsername[input].GetAsync();

            // Get user by id after getting it by name
            if (user != null)
            {
                detailedUser = await PhoenixWrapper.GetClient(Context.User.Id).V1.Users[detailedUser.Id ?? 0].Details.GetAsync();
            }
        }

        return detailedUser;
    }

    public async Task<ModResult> GetUserProfileAsync(string input)
    {
        var phoenixUser = await GetPhoenixUser(input);

        if (phoenixUser == null)
        {
            return new ModResult(ModResultType.NotFound, message: new ResponseMessage($"<:shrugR:945740284308893696> Could not find any user for **{input}**."));
        }

        var user = await GetUserByInput(input);

        // TODO: Check if user is banned
        /*if (user == null)
        {
            if (phoenixUser.Role != PhoenixRole.Banned)
            {
                return new ModResult(ModResultType.Success, new ResponseMessage($"The person **{phoenixUser.UserName} ({phoenixUser.UserId})** exists, but has no progress in Galaxy Life!"), phoenixUser, null);
            }
            else
            {
                return new ModResult(ModResultType.Success, new ResponseMessage($"**{phoenixUser.UserName} ({phoenixUser.UserId})** is banned and reset!"), phoenixUser, null);
            }
        }*/

        var stats = await GLClient.Api.GetUserStats(user.Id);

        // TODO: Get role and steamId
        /*var steamId = phoenixUser.SteamId ?? "No steam linked";
        var roleText = phoenixUser.Role == PhoenixRole.Banned ? "[BANNED]"
            : phoenixUser.Role == PhoenixRole.Donator ? "[Donator]"
            : phoenixUser.Role == PhoenixRole.Staff ? "[Staff]"
            : phoenixUser.Role == PhoenixRole.Administrator ? "[Admin]"
            : "";

        var color = phoenixUser.Role == PhoenixRole.Banned ? Color.Default
            : phoenixUser.Role == PhoenixRole.Donator ? new Color(15710778)
            : phoenixUser.Role == PhoenixRole.Staff ? new Color(2605694)
            : phoenixUser.Role == PhoenixRole.Administrator ? Color.DarkRed
            : Color.LightGrey;*/

        var color = Color.Default;
        var roleText = "Unknown role";

        var displayAlliance = "This user is not a member of any alliance.";

        if (!string.IsNullOrEmpty(user.AllianceId))
        {
            var alliance = await GLClient.Api.GetAlliance(user.AllianceId);

            // can happen due to 24hour player delay
            if (alliance == null)
            {
                displayAlliance = "This user has recently changed alliance, please wait for it to update!";
            }
            else
            {
                displayAlliance = $"This user is a member of **{alliance.Name}**.";
            }
        }

        var embed = new EmbedBuilder()
            .WithTitle($"{roleText} {phoenixUser.Username} | Profile")
            .WithThumbnailUrl(user.Avatar)
            .WithDescription($"{displayAlliance}\n\u200b")
            .AddField("Level", FormatNumber(user.Level), true)
            .AddField("Starbase", user.Planets[0].HQLevel, true)
            .AddField("Colonies", user.Planets.Count(x => x != null) - 1, true)
            .WithColor(color)
            .WithFooter(footer => footer
                .WithText($"ID: {phoenixUser.Id} • Account created on {phoenixUser.RegisteredOn.GetValueOrDefault():dd MMMM yyyy a\\t HH:mm}")
                .WithIconUrl(user.Avatar));

        /*if (!string.IsNullOrEmpty(phoenixUser.SteamId))
        {
            embed.WithUrl($"https://steamcommunity.com/profiles/{steamId.Replace("\"", "")}");
        }*/

        var message = new ResponseMessage("", [embed.Build()]);
        return new ModResult(ModResultType.Success, message, phoenixUser, user);
    }

    private static string FormatNumber(decimal number)
    {
        return number switch
        {
            >= 1_000_000_000 => $"{Math.Round(number / 1_000_000_000, 2)}B",
            >= 10_000_000 => $"{Math.Round(number / 1_000_000, 1)}M",
            >= 1_000_000 => $"{Math.Round(number / 1_000_000, 2)}M",
            >= 100_000 => $"{Math.Round(number / 1_000, 1)}K",
            >= 10_000 => $"{Math.Round(number / 1_000, 2)}K",
            _ => number.ToString()
        };
    }
}
