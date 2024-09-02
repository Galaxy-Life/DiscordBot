using Discord;
using Discord.Interactions;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using AdvancedBot.Core.Services;
using GL.NET.Entities;

namespace AdvancedBot.Core.Commands.Modules
{
    public class GLModule : TopModule
    {
        public GLService GLService { get; set; }

        [SlashCommand("status", "Shows the current status of the flash servers")]
        public async Task DisplayServerStatusAsync()
        {
            var status = await GLClient.Api.GetServerStatus();

            var embed = new EmbedBuilder()
                .WithTitle($"Server Status")
                .WithColor(Color.DarkBlue)
                .WithDescription("Here is the current status of all Galaxy Life servers.\n\u200b")
                .WithThumbnailUrl(Context.Guild.IconUrl)
                .WithFooter(footer => footer
                    .WithText($"Servers status requested by {Context.User.Username}#{Context.User.Discriminator}")
                    .WithIconUrl(Context.User.GetAvatarUrl()))
                .WithCurrentTimestamp();

            for (int i = 0; i < status.Count; i++)
            {
                embed.AddField($"{status[i].Name} ({status[i].Ping}ms)", status[i].IsOnline ? "✅ Operational" : "🛑 Offline", true);
            }

            await ModifyOriginalResponseAsync(msg => msg.Embeds = new Embed[] { embed.Build() });
        }

        [SlashCommand("profile", "Displays a user's Galaxy Life profile")]
        public async Task ShowUserProfileAsync(string input = "")
        {
            var response = await GLService.GetUserProfileAsync(string.IsNullOrEmpty(input) ? Context.User.Username : input);
            await SendResponseMessage(response.Message, false);

            // no gl data found
            if (response.User == null) return;

            var components = CreateDefaultComponents(response.PhoenixUser.UserName, response.User.Id, response.User.AllianceId, false);
            await ModifyOriginalResponseAsync(msg => msg.Components = components);
        }

        [SlashCommand("stats", "Displays a user's Galaxy Life stats")]
        public async Task ShowUserStatsAsync(string input = "")
        {
            var response = await  GLService.GetUserStatsAsync(string.IsNullOrEmpty(input) ? Context.User.Username : input);
            await SendResponseMessage(response.Message, false);

            // no gl data found
            if (response.User == null) return;

            var components = CreateDefaultComponents(response.User.Name, response.User.Id, response.User.AllianceId, false);
            await ModifyOriginalResponseAsync(msg => msg.Components = components);
        }

        [SlashCommand("alliance", "Displays basic info about an alliance")]
        public async Task ShowAllianceAsync(string input)
        {
            var response = await GLService.GetAllianceAsync(input);
            await SendResponseMessage(response.Message, false);

            // no gl data found
            if (response.Alliance == null) return;

            var owner = response.Alliance.Members.First(x => x.AllianceRole == AllianceRole.LEADER);
            var components = CreateDefaultComponents(owner.Name, owner.Id, response.Alliance.Id, false);
            await ModifyOriginalResponseAsync(msg => msg.Components = components);
        }

        [SlashCommand("members", "Displays a user's extensive Galaxy Life stats")]
        public async Task ShowAllianceMembersAsync(string input)
        {
            var response = await GLService.GetAllianceMembersAsync(input);
            await SendResponseMessage(response.Message, false);

            // no gl data found
            if (response.Alliance == null) return;

            var owner = response.Alliance.Members.First(member => member.AllianceRole == AllianceRole.LEADER);
            var components = CreateDefaultComponents(owner.Name, owner.Id, response.Alliance.Id, false);
            await ModifyOriginalResponseAsync(msg => msg.Components = components);
        }

        [SlashCommand("lb", "Retrieve a specific statistic leaderboard")]
        public async Task GetLeaderboardAsync([
            Choice("Experience", "xp"), 
            Choice("Experience from attacks", "attackXp"), 
            Choice("Rivals", "rivalsWon"), 
            Choice("Warpoints", "warpoints"), 
            Choice("Alliances", "alliancewarpoints")
        ] string type)
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
                case "warpoints":
                    title = "Warpoints | Leaderboard";
                    displayTexts = (await GLClient.Api.GetWarpointLeaderboard())
                        .Select(player => $"<:Starling_Frenchling:1080133173352091708>{player.Warpoints} **{player.Name}**")
                        .ToList();
                    break;
                case "alliancewarpoints":
                    title = "Alliances | Leaderboard";
                    displayTexts = (await GLClient.Api.GetAllianceWarpointLeaderboard())
                        .Select(alliance => $"<:TopNotch:1117073442236276809>{alliance.Warpoints} **{alliance.Name}**")
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
                await ModifyOriginalResponseAsync(msg => msg.Content = $"<:BAAWorker_Happy:943308706555260928> Servers are still loading the leaderboard, please be patient!");
                return;
            }

            for (int i = 0; i < displayTexts.Count; i++)
            {
                displayTexts[i] = $"**#{i + 1}** | {displayTexts[i]}";
            }

            await SendPaginatedMessageAsync(
                null, 
                displayTexts, 
                new EmbedBuilder()
                    .WithTitle(title)
                    .WithColor(Color.Purple)
                    .WithThumbnailUrl(Context.Guild.IconUrl)
                    .WithFooter(footer => footer
                        .WithText($"Leaderbard requested by {Context.User.Username}#{Context.User.Discriminator}")
                        .WithIconUrl(Context.User.GetAvatarUrl()))
                    .WithCurrentTimestamp()
                    
            );
        }

        [SlashCommand("compare", "Compare two players on base statistics", false, RunMode.Async)]
        public async Task CompareUsersAsync(string firstPlayer, string secondPlayer)
        {
            if (firstPlayer.ToLower() == secondPlayer.ToLower())
            {
                await ModifyOriginalResponseAsync(msg => msg.Content = $"You must compare two differents players!");
            }

            var baseUser = await GetUserByInput(firstPlayer);
            var secondUser = await GetUserByInput(secondPlayer);

            if (baseUser == null)
            {
                await ModifyOriginalResponseAsync(msg => msg.Content = $"<:shrugR:945740284308893696> Could not find any player named **{firstPlayer}**");
                return;
            }

            if (secondUser == null)
            {
                await ModifyOriginalResponseAsync(msg => msg.Content = $"<:shrugR:945740284308893696> Could not find any player named **{secondPlayer}**");
                return;
            }

            var baseUserStats = await GLClient.Api.GetUserStats(baseUser.Id);
            var secondUserStats = await GLClient.Api.GetUserStats(secondUser.Id);

            var expDifference = Math.Round((decimal)baseUser.Experience / secondUser.Experience, 2);

            var embed = new EmbedBuilder()
                .WithTitle($"{baseUser.Name} vs {secondUser.Name}")
                .WithColor(expDifference > 1 ? Color.DarkGreen : Color.DarkOrange)
                .WithThumbnailUrl(Context.Guild.IconUrl)
                .WithDescription(
                    $"{baseUser.Name} has **{FormatNumbers(expDifference)}x** the experience of {secondUser.Name}\n" +
                    $"Difference of **{FormatNumbers(Math.Abs((decimal)baseUser.Experience - secondUser.Experience))}** experience.\n\n")
                .AddField($"{baseUser.Name}", $"Level **{baseUser.Level}**\nExperience: **{FormatNumbers(baseUser.Experience)}**", true)
                .AddField($"{secondUser.Name}", $"Level **{secondUser.Level}**\nExperience: **{FormatNumbers(secondUser.Experience)}**", true)
                .WithFooter(footer => footer
                    .WithText($"Comparison requested by {Context.User.Username}#{Context.User.Discriminator}")
                    .WithIconUrl(Context.User.GetAvatarUrl()))
                .WithCurrentTimestamp()
                .Build();

            await ModifyOriginalResponseAsync(msg => msg.Embeds = new Embed[] { embed });
        }

        private static string FormatNumbers(decimal experiencePoints)
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
