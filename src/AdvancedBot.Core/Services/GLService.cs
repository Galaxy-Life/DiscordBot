using System;
using System.Linq;
using System.Threading.Tasks;
using AdvancedBot.Core.Entities;
using AdvancedBot.Core.Entities.Enums;
using Discord;
using GL.NET;
using GL.NET.Entities;
using Humanizer;
using Humanizer.Localisation;

namespace AdvancedBot.Core.Services;

public class GLService
{
    private readonly GLClient client;

    public GLService(GLClient client)
    {
        this.client = client;
    }

    public async Task<ModResult> GetUserProfileAsync(string input)
    {
        var phoenixUser = await getPhoenixUserByInput(input);

        if (phoenixUser == null)
        {
            return new ModResult(ModResultType.NotFound, message: new ResponseMessage($"<:shrugR:945740284308893696> Could not find any user for **{input}**."));
        }

        var user = await getUserByInput(input);

        if (user == null)
        {
            if (phoenixUser.Role != PhoenixRole.Banned)
            {
                return new ModResult(ModResultType.Success, new ResponseMessage($"The person **{phoenixUser.UserName} ({phoenixUser.UserId})** exists, but has no progress in Galaxy Life!"), phoenixUser, null);
            }
            else
            {
                return new ModResult(ModResultType.Success, new ResponseMessage($"**{phoenixUser.UserName} ({phoenixUser.UserId})** is banned and reset!"), phoenixUser, null);
            }
        }

        var stats = await client.Api.GetUserStats(user.Id);

        string steamId = phoenixUser.SteamId ?? "No steam linked";
        string roleText = phoenixUser.Role == PhoenixRole.Banned ? "[BANNED]"
            : phoenixUser.Role == PhoenixRole.Donator ? "[Donator]"
            : phoenixUser.Role == PhoenixRole.Staff ? "[Staff]"
            : phoenixUser.Role == PhoenixRole.Administrator ? "[Admin]"
            : "";

        var color = phoenixUser.Role == PhoenixRole.Banned ? Color.Default
            : phoenixUser.Role == PhoenixRole.Donator ? new Color(15710778)
            : phoenixUser.Role == PhoenixRole.Staff ? new Color(2605694)
            : phoenixUser.Role == PhoenixRole.Administrator ? Color.DarkRed
            : Color.LightGrey;

        string displayAlliance = "This user is not a member of any alliance.";

        if (!string.IsNullOrEmpty(user.AllianceId))
        {
            var alliance = await client.Api.GetAlliance(user.AllianceId);

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
            .WithTitle($"{roleText} {phoenixUser.UserName} | Profile")
            .WithThumbnailUrl(user.Avatar)
            .WithDescription($"{displayAlliance}\n\u200b")
            .AddField("Level", formatNumber(user.Level), true)
            .AddField("Starbase", user.Planets[0].HQLevel, true)
            .AddField("Colonies", user.Planets.Count(x => x != null) - 1, true)
            .WithColor(color)
            .WithFooter(footer => footer
                .WithText($"ID: {phoenixUser.UserId} • Account created on {phoenixUser.Created.GetValueOrDefault():dd MMMM yyyy a\\t HH:mm}")
                .WithIconUrl(user.Avatar));

        if (!string.IsNullOrEmpty(phoenixUser.SteamId))
        {
            embed.WithUrl($"https://steamcommunity.com/profiles/{steamId.Replace("\"", "")}");
        }

        var message = new ResponseMessage("", [embed.Build()]);
        return new ModResult(ModResultType.Success, message, phoenixUser, user);
    }

    public async Task<ModResult> GetUserStatsAsync(string input)
    {
        var user = await getUserByInput(input);

        if (user == null)
        {
            return new ModResult(ModResultType.NotFound, message: new ResponseMessage($"<:shrugR:945740284308893696> Could not find any user for **{input}**."));
        }

        var stats = await client.Api.GetUserStats(user.Id);
        string displayAlliance = string.IsNullOrEmpty(user.AllianceId) ? "This user is not a member of any alliance." : $"This user is a member of **{user.AllianceId}**.";

        var embed = new EmbedBuilder()
            .WithTitle($"Statistics | {user.Name}")
            .WithColor(Color.DarkMagenta)
            .WithThumbnailUrl(user.Avatar)
            .WithDescription($"{displayAlliance}\n\u200b")
            .AddField("Level", user.Level, true)
            .AddField("Players attacked", stats.PlayersAttacked, true)
            .AddField("NPCs attacked", stats.NpcsAttacked, true)
            .AddField("Coins spent", formatNumber(stats.CoinsSpent), true)
            .AddField("Minerals spent", formatNumber(stats.MineralsSpent), true)
            .AddField("Friends helped", formatNumber(stats.FriendsHelped), true)
            .AddField("Gifts received", formatNumber(stats.GiftsReceived), true)
            .AddField("Gifts sent", formatNumber(stats.GiftsSent), true)
            .AddField("Obstacles recycled", stats.ObstaclesRecycled, true)
            .AddField("Troops trained", stats.TroopsTrained, true)
            .AddField("Troopsize donated", stats.TroopSizesDonated, true)
            .AddField("Nukes used", stats.NukesUsed, true)
            .WithFooter(footer => footer
                .WithText($"ID: {user.Id} • User has played for {TimeSpan.FromMilliseconds(stats.TotalPlayTimeInMs).Humanize(3, minUnit: TimeUnit.Minute)}")
                .WithIconUrl(user.Avatar))
            .Build();

        var message = new ResponseMessage("", [embed]);

        return new ModResult(ModResultType.Success, message, null, user);
    }

    public async Task<ModResult> GetAllianceAsync(string input)
    {
        var alliance = await client.Api.GetAlliance(input);

        if (alliance == null)
        {
            return new ModResult(ModResultType.NotFound, message: new ResponseMessage($"<:shrugR:945740284308893696> Could not find any alliance for **{input}**."));
        }

        string emblemUrl = $"https://cdn.galaxylifegame.net/content/img/alliance_flag/AllianceLogos/flag_{(int)alliance.Emblem.Shape}_{(int)alliance.Emblem.Pattern}_{(int)alliance.Emblem.Icon}.png";

        var owner = alliance.Members.FirstOrDefault(x => x.AllianceRole == AllianceRole.LEADER);

        int warsWon = alliance.WarsWon;
        int warsLost = alliance.WarsLost;
        int warsTotal = warsWon + warsLost;
        double winRatio = (warsTotal > 0) ? (double)warsWon / warsTotal * 100 : 0;

        var embed = new EmbedBuilder()
            .WithTitle($"{alliance.Name} | Profile")
            .WithDescription($"This alliance owned by **{owner.Name}** (`{owner.Id}`).\n\u200b")
            .WithColor(Color.DarkPurple)
            .WithThumbnailUrl(emblemUrl)
            .AddField("Level", alliance.AllianceLevel, true)
            .AddField("Members", $"{alliance.Members.Length} / 50", true)
            .AddField("Warpoints", alliance.WarPoints, true)
            .AddField("Wars", $"{warsWon}W/{warsLost}L ({winRatio:F2}%)", true)
            .WithFooter(footer => footer
                .WithText($"Run `/members {input}` to see its members")
                .WithIconUrl(emblemUrl))
            .WithCurrentTimestamp();

        if (alliance.InWar)
        {
            embed.AddField("At war against", alliance.OpponentAllianceId, true);
        }

        var message = new ResponseMessage("", [embed.Build()]);
        return new ModResult(ModResultType.Success, message, null, null) { Alliance = alliance };
    }

    public async Task<ModResult> GetAllianceMembersAsync(string input)
    {
        var alliance = await client.Api.GetAlliance(input);

        if (alliance == null)
        {
            return new ModResult(ModResultType.NotFound, message: new ResponseMessage($"<:shrugR:945740284308893696> No alliance found for **{input}**"));
        }

        var owner = alliance.Members.FirstOrDefault(x => x.AllianceRole == AllianceRole.LEADER);
        var captains = alliance.Members.Where(x => x.AllianceRole == AllianceRole.ADMIN);
        var regulars = alliance.Members.Where(x => x.AllianceRole == AllianceRole.REGULAR);

        string formattedCaptains = $"{string.Join(" | ", captains.Select(x => $"**{x.Name}** ({x.Id})"))}\n\u200b";
        string formattedMembers = $"{string.Join(", ", regulars.Select(x => x.Name))}";

        string emblemUrl = $"https://cdn.galaxylifegame.net/content/img/alliance_flag/AllianceLogos/flag_{(int)alliance.Emblem.Shape}_{(int)alliance.Emblem.Pattern}_{(int)alliance.Emblem.Icon}.png";

        var embed = new EmbedBuilder()
            .WithTitle($"{alliance.Name} | Members")
            .WithDescription($"This alliance owned by **{owner.Name}** (`{owner.Id}`)\n\u200b")
            .WithColor(Color.DarkGreen)
            .WithThumbnailUrl(emblemUrl)
            .AddField($"Captains | {captains.Count()}", string.IsNullOrEmpty(formattedCaptains) ? "None\n\u200b" : formattedCaptains)
            .AddField($"Members | {regulars.Count()}", string.IsNullOrEmpty(formattedMembers) ? "None" : formattedMembers)
            .WithFooter(footer => footer
                .WithText($"Run `/alliance {input}` to see its profile")
                .WithIconUrl(emblemUrl))
            .WithCurrentTimestamp()
            .Build();

        var message = new ResponseMessage("", [embed]);
        return new ModResult(ModResultType.Success, message, null, null) { Alliance = alliance };
    }

    private async Task<User> getUserByInput(string input)
    {
        User profile = null;
        string digitString = new(input.Where(char.IsDigit).ToArray());

        if (digitString.Length == input.Length)
        {
            profile = await client.Api.GetUserById(input);
        }

        profile ??= await client.Api.GetUserByName(input);

        return profile;
    }

    private async Task<PhoenixUser> getPhoenixUserByInput(string input, bool full = false)
    {
        PhoenixUser user = null;

        string digitString = new(input.Where(char.IsDigit).ToArray());

        // extra check to see if all characters were numbers
        if (digitString.Length == input.Length)
        {
            if (full)
            {
                user = await client.Phoenix.GetFullPhoenixUserAsync(input);
            }
            else
            {
                user = await client.Phoenix.GetPhoenixUserAsync(input);
            }
        }

        // try to get user by name
        user ??= await client.Phoenix.GetPhoenixUserByNameAsync(input);

        // get user by id after getting it by name
        if (user != null && full)
        {
            user = await client.Phoenix.GetFullPhoenixUserAsync(user.UserId);
        }

        return user;
    }

    private static string formatNumber(decimal number)
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
