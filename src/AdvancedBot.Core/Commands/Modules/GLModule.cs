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
            var status = await GLClient.GetServerStatus();

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
        public async Task ShowUserProfileAsync(string input = "")
        {
            var response = await GLService.GetUserProfileAsync(string.IsNullOrEmpty(input) ? Context.User.Username : input);
            await SendResponseMessage(response.Message, false);

            // no gl data found
            if (response.User == null)
            {
                return;
            }

            var components = CreateDefaultComponents(response.PhoenixUser.UserName, response.User.Id, response.User.AllianceId, false);
            await ModifyOriginalResponseAsync(x => x.Components = components);
        }

        [SlashCommand("stats", "Displays a user's Galaxy Life stats")]
        public async Task ShowUserStatsAsync(string input = "")
        {
            var response = await  GLService.GetUserStatsAsync(string.IsNullOrEmpty(input) ? Context.User.Username : input);
            await SendResponseMessage(response.Message, false);

            // no gl data found
            if (response.User == null)
            {
                return;
            }

            var components = CreateDefaultComponents(response.User.Name, response.User.Id, response.User.AllianceId, false);
            await ModifyOriginalResponseAsync(x => x.Components = components);
        }

        [SlashCommand("alliance", "Displays basic info about an alliance")]
        public async Task ShowAllianceAsync(string input)
        {
            var response = await GLService.GetAllianceAsync(input);
            await SendResponseMessage(response.Message, false);

            // no gl data found
            if (response.Alliance == null)
            {
                return;
            }

            var owner = response.Alliance.Members.First(x => x.AllianceRole == AllianceRole.LEADER);
            var components = CreateDefaultComponents(owner.Name, owner.Id, response.Alliance.Id, false);
            await ModifyOriginalResponseAsync(x => x.Components = components);
        }

        [SlashCommand("members", "Displays a user's extensive Galaxy Life stats")]
        public async Task ShowAllianceMembersAsync(string input)
        {
            var response = await GLService.GetAllianceMembersAsync(input);
            await SendResponseMessage(response.Message, false);

            // no gl data found
            if (response.Alliance == null)
            {
                return;
            }

            var owner = response.Alliance.Members.First(x => x.AllianceRole == AllianceRole.LEADER);
            var components = CreateDefaultComponents(owner.Name, owner.Id, response.Alliance.Id, false);
            await ModifyOriginalResponseAsync(x => x.Components = components);
        }

        [SlashCommand("lb", "Obtain the in-game leaderboard of a certain statistic")]
        public async Task GetLeaderboardAsync([Choice("Xp", "xp"), Choice("Xp From Attack", "attackXp"), Choice("Rivals Won", "rivalsWon")]string type)
        {
            List<string> displayTexts = new List<string>() { "Failed to get information" };
            var title = "Galaxy Life Leaderboard";

            switch (type)
            {
                case "attackXp":
                    title = "Xp From Attack Leaderboard";
                    displayTexts = (await GLClient.GetXpFromAttackLeaderboard()).Select(x => $"<:RedExp:1082428998182768701>{x.Level} **{x.Name}**").ToList();
                    break;
                case "rivalsWon":
                    title = "Rivals Won Leaderboard";
                    displayTexts = (await GLClient.GetRivalsWonLeaderboard()).Select(x => $"<:pistol:1082429024963395674>{x.RivalsWon} **{x.Name}**").ToList();
                    break;
                default:
                case "xp":
                    title = "Xp Leaderboard";
                    displayTexts = (await GLClient.GetXpLeaderboard()).Select(x => $"<:experience:920289172428849182> {x.Level} **{x.Name}**").ToList();
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

            var baseUserStats = await GLClient.GetUserStats(baseUser.Id);
            var secondUserStats = await GLClient.GetUserStats(secondUser.Id);

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
