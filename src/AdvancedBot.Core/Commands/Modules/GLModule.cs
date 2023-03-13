using Discord;
using Discord.Commands;
using Discord.Interactions;
using GL.NET;
using GL.NET.Entities;
using Humanizer.Localisation;
using Humanizer;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using AdvancedBot.Core.Services;

namespace AdvancedBot.Core.Commands.Modules
{
    [Name("gl")]
    public class GLModule : TopModule
    {
        private GLClient _client;
        public PaginatorService _paginator;

        public GLModule(GLClient client, PaginatorService paginator)
        {
            _client = client;
            _paginator = paginator;
        }

        [SlashCommand("status", "Shows the current status of the flash servers")]
        [Command("status")]
        public async Task DisplayServerStatusAsync()
        {
            var status = await _client.GetServerStatus();

            var embed = new EmbedBuilder()
            {
                Title = $"Server Status",
                Color = Color.Blue
            };

            for (int i = 0; i < status.Count; i++)
            {
                embed.AddField($"{status[i].Name} ({status[i].Ping}ms)", status[i].IsOnline ? "✅ Running" : "🛑 Down", true);
            }

            await ModifyOriginalResponseAsync(x => x.Embed = embed.Build() );
        }

        [SlashCommand("profile", "Displays a user's Galaxy Life profile")]
        [Command("profile")]
        [Discord.Commands.Summary("Displays a user's Galaxy Life profile")]
        public async Task ShowUserProfileAsync(string input = "")
        {
            var user = await GetUserByInput(input);

            if (user == null)
            {
                await ModifyOriginalResponseAsync(x => x.Content = $"<:shrugR:945740284308893696> No user found for **{input}**");
                return;
            }

            var steamId = await _client.GetSteamIdByUserId(user.Id) ?? "No steam linked";

            var embed = new EmbedBuilder()
                .WithTitle($"Game Profile of {user.Name}")
                .WithThumbnailUrl(user.Avatar)
                .WithDescription($"\nId: **{user.Id}**\nSteam Id: **{steamId.Replace("\"", "")}**")
                .WithFooter("I figured out how to add steam info!!!!!!!!");

            if (steamId != "No steam linked")
            {
                embed.WithUrl($"https://steamcommunity.com/profiles/{steamId.Replace("\"", "")}");
            }

            await ModifyOriginalResponseAsync(x => x.Embed = embed.Build());
        }

        [SlashCommand("stats", "Displays a user's Galaxy Life stats")]
        [Command("stats")]
        [Discord.Commands.Summary("Displays a user's Galaxy Life stats")]
        public async Task ShowUserStatsAsync(string input = "")
        {
            var user = await GetUserByInput(input);

            if (user == null)
            {
                await ModifyOriginalResponseAsync(x => x.Content = $"<:shrugR:945740284308893696> No user found for **{input}**");
                return;
            }

            var displayAlliance = "User is not in any alliance.";

            if (!string.IsNullOrEmpty(user.AllianceId))
            {
                var alliance = await _client.GetAlliance(user.AllianceId);
                displayAlliance = $"User is in **{alliance.Name}**.";
            }

            var stats = await _client.GetUserStats(user.Id);

            await ModifyOriginalResponseAsync(x => x.Embed = new EmbedBuilder()
            {
                Title = $"Statistics for {user.Name} ({user.Id})",
                Color = Color.DarkMagenta,
                ThumbnailUrl = user.Avatar,
                Description = $"{displayAlliance}\nUser is level **{user.Level}**.\n\u200b"
            }
            .AddField("Experience", FormatNumbers(user.Experience), true)
            .AddField("Starbase", user.Planets[0].HQLevel, true)
            .AddField("Colonies", user.Planets.Count(x => x != null) - 1, true)
            .AddField("Players Attacked", stats.PlayersAttacked, true)
            .Build());
        }

        [SlashCommand("as", "Displays a user's extensive Galaxy Life stats")]
        public async Task ShowUserAsAsync(string input = "")
            => await ShowUserAdvancedStatsAsync(input);

        [SlashCommand("advancedstats", "Displays a user's extensive Galaxy Life stats")]
        [Command("advancedstats")]
        [Alias("as")]
        [Discord.Commands.Summary("Displays a user's Galaxy Life stats")]
        public async Task ShowUserAdvancedStatsAsync(string input = "")
        {
            var user = await GetUserByInput(input);

            if (user == null)
            {
                await ModifyOriginalResponseAsync(x => x.Content = $"<:shrugR:945740284308893696> No user found for **{input}**");
                return;
            }

            var stats = await _client.GetUserStats(user.Id);

            var displayAlliance = string.IsNullOrEmpty(user.AllianceId) ? "User is not in any alliance." : $"User is part of **{user.AllianceId}**.";

            await ModifyOriginalResponseAsync(x => x.Embed = new EmbedBuilder()
            {
                Title = $"Statistics for {user.Name} ({user.Id})",
                Color = Color.DarkMagenta,
                ThumbnailUrl = user.Avatar,
                Description = $"{displayAlliance}\nUser is level **{user.Level}**.\n\u200b"
            }
            .AddField("Level", user.Level, true)
            .AddField("Players Attacked", stats.PlayersAttacked, true)
            .AddField("Npcs Attacked", stats.NpcsAttacked, true)
            .AddField("Coins Spent", FormatNumbers(stats.CoinsSpent), true)
            .AddField("Minerals Spent", FormatNumbers(stats.MineralsSpent), true)
            .AddField("Friends Helped", FormatNumbers(stats.FriendsHelped), true)
            .AddField("Gifts Received", FormatNumbers(stats.GiftsReceived), true)
            .AddField("Gifts Sent", FormatNumbers(stats.GiftsSent), true)
            .AddField("PlayTime", TimeSpan.FromMilliseconds(stats.TotalPlayTimeInMs).Humanize(3, minUnit: TimeUnit.Minute), true)
            .AddField("Nukes Used", stats.NukesUsed, true)
            .AddField("Obstacles Recycled", stats.ObstaclesRecycled, true)
            .AddField("Troops trained", stats.TroopsTrained, true)
            .AddField("Troopsize donated", stats.TroopSizesDonated, true)
            .Build());
        }

        [SlashCommand("alliance", "Displays basic info about an alliance")]
        [Command("alliance")]
        [Discord.Commands.Summary("Displays basic info about an alliance")]
        public async Task ShowAllianceAsync([Remainder]string input)
        {
            var alliance = await _client.GetAlliance(input);

            if (alliance == null)
            {
                await ModifyOriginalResponseAsync(x => x.Content = $"<:shrugR:945740284308893696> No alliance found for **{input}**");
                return;
            }

            var owner = alliance.Members.FirstOrDefault(x => x.AllianceRole == AllianceRole.LEADER);

            var embed = new EmbedBuilder()
            .WithTitle(alliance.Name)
            .WithDescription($"<:AFECounselor_Mobius:1082315024829272154> Alliance owned by **{owner.Name}** ({owner.Id})\n\u200b")
            .WithColor(Color.DarkPurple)
            .WithThumbnailUrl($"https://cdn.galaxylifegame.net/content/img/alliance_flag/AllianceLogos/flag_{(int)alliance.Emblem.Shape}_{(int)alliance.Emblem.Pattern}_{(int)alliance.Emblem.Icon}.png")
            .AddField("Level", alliance.AllianceLevel, true)
            .AddField("Members", alliance.Members.Length, true)
            .AddField("Warpoints", alliance.WarPoints, true)
            .AddField("Wars Done", alliance.WarsWon + alliance.WarsLost, true)
            .AddField("Wars Won", alliance.WarsWon, true)
            .AddField("In War With", alliance.OpponentAllianceId, true)
            .WithFooter($"Run !members {input} to see its members.");

            await ModifyOriginalResponseAsync(x => x.Embed = embed.Build());
        }

        [SlashCommand("members", "Displays a user's extensive Galaxy Life stats")]
        [Command("members")]
        [Discord.Commands.Summary("Displays a user's Galaxy Life stats.")]
        public async Task ShowAllianceMembersAsync([Remainder]string input)
        {
            var alliance = await _client.GetAlliance(input);

            if (alliance == null)
            {
                await ModifyOriginalResponseAsync(x => x.Content = $"<:shrugR:945740284308893696> No alliance found for **{input}**");
                return;
            }

            var owner = alliance.Members.FirstOrDefault(x => x.AllianceRole == AllianceRole.LEADER);
            var captains = alliance.Members.Where(x => x.AllianceRole == AllianceRole.ADMIN);
            var regulars = alliance.Members.Where(x => x.AllianceRole == AllianceRole.REGULAR);

            var formattedCaptains = $"{string.Join(" | ", captains.Select(x => $"**{x.Name}** ({x.Id})"))}\n\u200b";
            var formattedMembers = $"{string.Join(", ", regulars.Select(x => x.Name))}";

            var embed = new EmbedBuilder()
                .WithTitle($"Members of {alliance.Name}")
                .WithColor(Color.DarkGreen)
                .WithThumbnailUrl($"https://cdn.galaxylifegame.net/content/img/alliance_flag/AllianceLogos/flag_{(int)alliance.Emblem.Shape}_{(int)alliance.Emblem.Pattern}_{(int)alliance.Emblem.Icon}.png")
                .AddField("Owner", $"**{owner.Name}** ({owner.Id})\n\u200b")
                .AddField($"Captains ({captains.Count()})", string.IsNullOrEmpty(formattedCaptains) ? "None\n\u200b" : formattedCaptains)
                .AddField($"Members ({regulars.Count()})", string.IsNullOrEmpty(formattedMembers) ? "None" : formattedMembers)
                .Build();

            await ModifyOriginalResponseAsync(x => x.Embed = embed);
        }

        [SlashCommand("lb", "Obtain the in-game leaderboard of a certain statistic")]
        [Command("lb", RunMode = Discord.Commands.RunMode.Async)]
        [Discord.Commands.Summary("Obtain the in-game leaderboard of a certain statistic")]
        public async Task GetLeaderboardAsync([Choice("Xp", "xp"), Choice("Xp From Attack", "attackXp"), Choice("Rivals Won", "rivalsWon")]string type)
        {
            List<string> displayTexts = new List<string>() { "Failed to get information" };
            var title = "Galaxy Life Leaderboard";

            switch (type)
            {
                case "attackXp":
                    title = "Xp From Attack Leaderboard";
                    displayTexts = (await _client.GetXpFromAttackLeaderboard()).Select(x => $"<:RedExp:1082428998182768701>{x.Level} **{x.Name}**").ToList();
                    break;
                case "rivalsWon":
                    title = "Rivals Won Leaderboard";
                    displayTexts = (await _client.GetRivalsWonLeaderboard()).Select(x => $"<:pistol:1082429024963395674>{x.RivalsWon} **{x.Name}**").ToList();
                    break;
                default:
                case "xp":
                    title = "Xp Leaderboard";
                    displayTexts = (await _client.GetXpLeaderboard()).Select(x => $"<:experience:920289172428849182> {x.Level} **{x.Name}**").ToList();
                    break;
            }

            if (displayTexts.Count == 0)
            {
                await ModifyOriginalResponseAsync(x => x.Content = $"<:BAAWorker_Happy:943308706555260928> Servers are still loading the leaderboard, please be patient!");
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


        [SlashCommand("compare", "Compare stats of two users", false, Discord.Interactions.RunMode.Async)]
        [Command("compare", RunMode = Discord.Commands.RunMode.Async)]
        [Discord.Commands.Summary("Compare stats of two users")]
        public async Task CompareUsersAsync(string input1, string input2)
        {
            if (input1.ToLower() == input2.ToLower())
            {
                await ModifyOriginalResponseAsync(x => x.Content = $"You cannot compare a user to itself!");
            }

            var baseUser = await GetUserByInput(input1);
            var secondUser = await GetUserByInput(input2);

            if (baseUser == null)
            {
                await ModifyOriginalResponseAsync(x => x.Content = $"<:shrugR:945740284308893696> No user found for **{input1}**");
                return;
            }

            if (secondUser == null)
            {
                await ModifyOriginalResponseAsync(x => x.Content = $"<:shrugR:945740284308893696> No user found for **{input2}**");
                return;
            }

            var baseUserStats = await _client.GetUserStats(baseUser.Id);
            var secondUserStats = await _client.GetUserStats(secondUser.Id);

            var expDifference = Math.Round((decimal)baseUser.Experience / secondUser.Experience, 2);

            await ModifyOriginalResponseAsync(x => x.Embed = new EmbedBuilder()
            {
                Title = $"Comparison between {baseUser.Name} & {secondUser.Name}",
                Description = $"{baseUser.Name} has **{expDifference}x** the experience of {secondUser.Name}\n" +
                              $"Difference of **{FormatNumbers(Math.Abs((decimal)baseUser.Experience - secondUser.Experience))}** experience.\n\n" +
                              $"{baseUser.Name} has **{FormatNumbers(baseUser.Experience)}** experience and is level **{baseUser.Level}**.\n" +
                              $"{secondUser.Name} has **{FormatNumbers(secondUser.Experience)}** experience and is level **{secondUser.Level}**.",
                Color = expDifference > 1 ? Color.DarkGreen : Color.DarkOrange
            }
            .Build());
        }

        [Command("compare")][Alias("c")]
        [Discord.Commands.Summary("Compare stats of two users")]
        public async Task CompareUsersAsync(string userToCompare)
            => await CompareUsersAsync(Context.User.Username, userToCompare);

        private async Task<User> GetUserByInput(string input)
        {
            if (string.IsNullOrEmpty(input)) input = Context.User.Username;

            var profile = await _client.GetUserById(input);

            if (profile == null)
            {
                profile = await _client.GetUserByName(input);
            }

            return profile;
        }

        private async Task SendPaginatedMessageAsync(IEnumerable<EmbedField> displayFields, IEnumerable<string> displayTexts, EmbedBuilder templateEmbed)
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

                for (int i = 0; i < fields.Length; i++)
                {
                    templateEmbed.AddField(fields[i].Name, fields[i].Value, fields[i].Inline);
                }
            }

            templateEmbed.WithTitle($"{templateEmbed.Title} (Page 1)");
            templateEmbed.WithFooter($"Total of {displayItems} players");

            await _paginator.HandleNewPaginatedMessageAsync(Context, displayFields, displayTexts, templateEmbed.Build());
            await Task.Delay(1000);
        }

        private string FormatNumbers(decimal experiencePoints)
        {
            // 1bil<
            if (experiencePoints > 1000000000) return $"{Math.Round(experiencePoints / 1000000000, 2)}B";

            // 10mil< 
            else if (experiencePoints > 10000000) return $"{Math.Round(experiencePoints / 1000000, 1)}M";

            // 1mil< 
            else if (experiencePoints > 1000000) return $"{Math.Round(experiencePoints / 1000000, 2)}M";

            // 100K<
            else if (experiencePoints > 10000) return $"{Math.Round(experiencePoints / 1000, 1)}K";

            // 10K<
            else if (experiencePoints > 10000) return $"{Math.Round(experiencePoints / 1000, 2)}K";

            else return experiencePoints.ToString();
        }
    }
}
