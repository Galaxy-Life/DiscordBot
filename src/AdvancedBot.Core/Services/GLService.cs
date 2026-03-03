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
using Phoenix.Api.Models;

namespace AdvancedBot.Core.Services;

public class GLService
{
    private readonly GLClient _client;

    public GLService(GLClient client)
    {
        _client = client;
    }

    public async Task<ModResult> GetUserStatsAsync(string input)
    {
        var user = await getUserByInput(input);

        if (user == null)
        {
            return new ModResult(ModResultType.NotFound, message: new ResponseMessage($"Could not find any user for **{input}**."));
        }

        var stats = await _client.Api.GetUserStats(user.Id);
        var displayAlliance = string.IsNullOrEmpty(user.AllianceId) ? "This user is not a member of any alliance." : $"This user is a member of **{user.AllianceId}**.";

        var embed = new EmbedBuilder()
            .WithTitle($"Statistics | {user.Name}")
            .WithColor(Color.DarkMagenta)
            .WithThumbnailUrl(user.Avatar)
            .WithDescription($"{displayAlliance}\n\u200b")
            .AddField("Level", user.Level, true)
            .AddField("Players attacked", stats.PlayersAttacked, true)
            .AddField("NPCs attacked", stats.NpcsAttacked, true)
            .AddField("Coins spent", FormatNumber(stats.CoinsSpent), true)
            .AddField("Minerals spent", FormatNumber(stats.MineralsSpent), true)
            .AddField("Friends helped", FormatNumber(stats.FriendsHelped), true)
            .AddField("Gifts received", FormatNumber(stats.GiftsReceived), true)
            .AddField("Gifts sent", FormatNumber(stats.GiftsSent), true)
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
        var alliance = await _client.Api.GetAlliance(input);

        if (alliance == null)
        {
            return new ModResult(ModResultType.NotFound, message: new ResponseMessage($"Could not find any alliance for **{input}**."));
        }

        var emblemUrl = $"https://cdn.galaxylifegame.net/content/img/alliance_flag/AllianceLogos/flag_{(int)alliance.Emblem.Shape}_{(int)alliance.Emblem.Pattern}_{(int)alliance.Emblem.Icon}.png";

        var owner = alliance.Members.FirstOrDefault(x => x.AllianceRole == AllianceRole.LEADER);

        var warsWon = alliance.WarsWon;
        var warsLost = alliance.WarsLost;
        var warsTotal = warsWon + warsLost;
        var winRatio = (warsTotal > 0) ? (double)warsWon / warsTotal * 100 : 0;

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
        var alliance = await _client.Api.GetAlliance(input);

        if (alliance == null)
        {
            return new ModResult(ModResultType.NotFound, message: new ResponseMessage($"No alliance found for **{input}**"));
        }

        var owner = alliance.Members.FirstOrDefault(x => x.AllianceRole == AllianceRole.LEADER);
        var captains = alliance.Members.Where(x => x.AllianceRole == AllianceRole.ADMIN);
        var regulars = alliance.Members.Where(x => x.AllianceRole == AllianceRole.REGULAR);

        var formattedCaptains = $"{string.Join(" | ", captains.Select(x => $"**{x.Name}** ({x.Id})"))}\n\u200b";
        var formattedMembers = $"{string.Join(", ", regulars.Select(x => x.Name))}";

        var emblemUrl = $"https://cdn.galaxylifegame.net/content/img/alliance_flag/AllianceLogos/flag_{(int)alliance.Emblem.Shape}_{(int)alliance.Emblem.Pattern}_{(int)alliance.Emblem.Icon}.png";

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
            profile = await _client.Api.GetUserById(input);
        }

        profile ??= await _client.Api.GetUserByName(input);

        return profile;
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
