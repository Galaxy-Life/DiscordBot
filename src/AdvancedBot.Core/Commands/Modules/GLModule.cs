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

namespace AdvancedBot.Core.Commands.Modules
{
    [Name("gl")]
    public class GLModule : InteractionModuleBase<SocketInteractionContext>
    {
        private GLAsyncClient _client;

        public GLModule(GLAsyncClient client)
        {
            _client = client;
        }

        [SlashCommand("status", "Shows the current status of the flash servers")]
        [Command("status")]
        public async Task DisplayServerStatusAsync()
        {
            await DeferAsync();
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
            await DeferAsync();
            var user = await GetUserByInput(input);

            if (user == null)
            {
                throw new Exception($"No user found for {input}");
            }

            var embed = new EmbedBuilder()
                .WithTitle($"Game Profile of {user.Name}")
                .WithUrl(user.Avatar)
                .WithThumbnailUrl(user.Avatar)
                .WithDescription($"\nId: **{user.Id}**")
                .WithFooter("Steam info will be shown here later (need to figure out how first)")
                .Build();

            await ModifyOriginalResponseAsync(x => x.Embed = embed);
        }

        [SlashCommand("stats", "Displays a user's Galaxy Life stats")]
        [Command("stats")]
        [Discord.Commands.Summary("Displays a user's Galaxy Life stats")]
        public async Task ShowUserStatsAsync(string input = "")
        {
            await DeferAsync();
            var user = await GetUserByInput(input);

            if (user == null)
            {
                throw new Exception($"No user found for {input}");
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
            .AddField("Experience", FormatNumbers(user.Experience), true)
            .AddField("Starbase", user.Planets[0].HQLevel, true)
            .AddField("Colonies", user.Planets.Count(x => x != null) - 1, true)
            .AddField("Players Attacked", stats.PlayersAttacked, true)
            .WithFooter($"Requested by {Context.User.Username} | {Context.User.Id}")
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
            await DeferAsync();
            var user = await GetUserByInput(input);

            if (user == null)
            {
                throw new Exception($"No user found for {input}");
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
            await DeferAsync();
            var alliance = await _client.GetAlliance(input);

            if (alliance == null)
            {
                throw new Exception($"No alliance found for {input}");
            }

            var owner = alliance.Members.FirstOrDefault(x => x.AllianceRole == AllianceRole.LEADER);

            await ModifyOriginalResponseAsync(x => x.Embed = new EmbedBuilder()
            .WithTitle(alliance.Name)
            .WithDescription($"<:AFECounselor_Mobius:639094741631369247> Alliance owned by **{owner.Name}** ({owner.Id})\n\u200b")
            .WithColor(Color.DarkPurple)
            .WithThumbnailUrl($"https://cdn.galaxylifegame.net/content/img/alliance_flag/AllianceLogos/flag_{(int)alliance.Emblem.Shape}_{(int)alliance.Emblem.Pattern}_{(int)alliance.Emblem.Icon}.png")
            .AddField("Level", alliance.AllianceLevel, true)
            .AddField("Members", alliance.Members.Length, true)
            .AddField("Warpoints", alliance.WarPoints, true)
            .AddField("Wars Done", alliance.WarsWon + alliance.WarsLost, true)
            .AddField("Wars Won", alliance.WarsWon, true)
            .WithFooter($"Run !members {input} to see its members.")
            .Build());
        }

        [SlashCommand("members", "Displays a user's extensive Galaxy Life stats")]
        [Command("members")]
        [Discord.Commands.Summary("Displays a user's Galaxy Life stats.")]
        public async Task ShowAllianceMembersAsync([Remainder]string input)
        {
            await DeferAsync();
            var alliance = await _client.GetAlliance(input);

            if (alliance == null)
            {
                throw new Exception($"No alliance found for {input}");
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

        [SlashCommand("compare", "Compare stats of two users", false, Discord.Interactions.RunMode.Async)]
        [Command("compare", RunMode = Discord.Commands.RunMode.Async)]
        [Discord.Commands.Summary("Compare stats of two users")]
        public async Task CompareUsersAsync(string input1, string input2)
        {
            await DeferAsync();
            if (input1.ToLower() == input2.ToLower())
            {
                throw new Exception("Please enter two different users to compare.");
            }

            var baseUser = await GetUserByInput(input1);
            var secondUser = await GetUserByInput(input2);

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
