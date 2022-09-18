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
    public class GLModule : TopModule
    {
        private GLAsyncClient _client;

        public GLModule(GLAsyncClient client)
        {
            _client = client;
        }

        [SlashCommand("status", "Shows the current status of the flash servers")]
        [Command("status")]
        [Discord.Commands.Summary("Shows the current status of the flash servers.")]
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

            await ReplyAsync("", false, embed.Build());
        }

        [SlashCommand("profile", "Displays a user's Galaxy Life profile")]
        [Command("profile")]
        [Discord.Commands.Summary("Displays a user's Galaxy Life profile.")]
        public async Task ShowUserProfileAsync(string input = "")
        {
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

            await ReplyAsync("", false, embed);
        }

        [SlashCommand("stats", "Displays a user's Galaxy Life stats")]
        [Command("stats")]
        [Discord.Commands.Summary("Displays a user's Galaxy Life stats.")]
        public async Task ShowUserStatsAsync(string input = "")
        {
            var user = await GetUserByInput(input);

            if (user == null)
            {
                throw new Exception($"No user found for {input}");
            }

            var stats = await _client.GetUserStats(user.Id);

            //var displayAlliance = user.Alliance == "None" ? "User is not in any alliance." : $"User is part of **{profile.Statistics.Alliance}**.";

            await ReplyAsync("", false, new EmbedBuilder()
            {
                Title = $"Statistics for {user.Name} ({user.Id})",
                Color = Color.DarkMagenta,
                ThumbnailUrl = user.Avatar,
                Description = $"{"Might be in some alliance idfk :/"}\nUser is level **{user.Level}**.\n\u200b"
            }
            .AddField("Experience", FormatNumbers(user.Score), true)
            .AddField("Starbase", user.Planets[0].HQLevel, true)
            .AddField("Colonies", user.Planets.Count(x => x != null) - 1, true)
            .AddField("Is Online", user.Online, true)
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
        [Discord.Commands.Summary("Displays a user's Galaxy Life stats.")]
        public async Task ShowUserAdvancedStatsAsync(string input = "")
        {
            var user = await GetUserByInput(input);

            if (user == null)
            {
                throw new Exception($"No user found for {input}");
            }

            var stats = await _client.GetUserStats(user.Id);

            //var displayAlliance = user.Alliance == "None" ? "User is not in any alliance." : $"User is part of **{profile.Statistics.Alliance}**.";

            await ReplyAsync("", false, new EmbedBuilder()
            {
                Title = $"Statistics for {user.Name} ({user.Id})",
                Color = Color.DarkMagenta,
                ThumbnailUrl = user.Avatar,
                Description = $"{"Might be in some alliance idfk :/"}\nUser is level **{user.Level}**.\n\u200b"
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

        [SlashCommand("advancedstats", "Displays a user's extensive Galaxy Life stats")]
        [Command("advancedstats")]
        [Alias("as")]
        [Discord.Commands.Summary("Displays a user's Galaxy Life stats.")]
        public async Task ShowAllianceAsync(string input)
        {
            var alliance = await _client.GetAlliance(input);

            if (alliance == null)
            {
                throw new Exception($"No alliance found for {input}");
            }

            await ReplyAsync(embed: new EmbedBuilder()
            {
                Title = ""
            }
            .WithTitle(alliance.Name)
            .WithDescription($"<:AFECounselor_Mobius:639094741631369247> Alliance owned by **someone** (00000)\n\u200b")
            .WithColor(Color.DarkPurple)
            .WithThumbnailUrl($"")
            .AddField("Members", alliance.Members.Length, true)
            .AddField("Warpoints", alliance.WarPoints, true)
            .AddField("Wars Participated", alliance.WarsWon + alliance.WarsLost, false)
            .AddField("Wars won", alliance.WarsWon, true)
            .WithFooter($"Run !members {input} to see its members.")
            .Build());
        }

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
