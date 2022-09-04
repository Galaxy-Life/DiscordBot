using Discord;
using Discord.Commands;
using Discord.Interactions;
using GL.NET;
using GL.NET.Entities;
using SteamKit2;
using System;
using System.Threading.Tasks;

namespace AdvancedBot.Core.Commands.Modules
{
    [Name("gl")]
    public class GLModule : TopModule
    {
        private GLAsyncClient _client;
        private SteamClient _steam;

        public GLModule(GLAsyncClient client, SteamClient steam)
        {
            _client = client;
            _steam = steam;
        }

        [SlashCommand("status", "Shows the current status of the flash servers.")]
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
                embed.AddField(status[i].Name, status[i].IsOnline ? "✅ Running" : "🛑 Down", true);
            }

            await ReplyAsync("", false, embed.Build());
        }

        [SlashCommand("profile", "Displays a user's GL profile.")]
        [Command("profile")]
        [Discord.Commands.Summary("Displays a user's GL profile.")]
        public async Task ShowUserProfileAsync(string input = "")
        {
            var user = await GetUserByInput(input);

            var embed = new EmbedBuilder()
                .WithTitle($"Game Profile of {user.Name}")
                .WithUrl(user.Avatar)
                .WithThumbnailUrl(user.Avatar)
                .WithDescription($"\nId: **{user.Id}**")
                .Build();

            await ReplyAsync("", false, embed);
        }

        [SlashCommand("stats", "Displays a user's GL stats.")]
        [Command("stats")]
        [Discord.Commands.Summary("Displays a user's GL stats.")]
        public async Task ShowUserStatsAsync(string input = "")
        {
            var user = await GetUserByInput(input);
            var stats = await _client.GetUserStats(user.Id);

            //var displayAlliance = user.Alliance == "None" ? "User is not in any alliance." : $"User is part of **{profile.Statistics.Alliance}**.";

            await ReplyAsync("", false, new EmbedBuilder()
            {
                Title = $"Statistics for {user.Name} ({user.Id})",
                Color = Color.DarkMagenta,
                ThumbnailUrl = user.Avatar,
                Description = $"{"Might be in some alliance idfk :/"}\nUser is level **{user.Level}**.\n\u200b"
            }
            .AddField("Chips", user.Chips, true)
            .AddField("Experience", FormatNumbers(user.Score), true)
            .AddField("Starbase", user.Planets[0].HQLevel, true)
            .AddField("Colonies", user.Planets.Length - 1, true)
            .AddField("Is Online", user.Online, true)
            .AddField("Players Attacked", stats.PlayersAttacked, true)
            .WithFooter($"Requested by {Context.User.Username} | {Context.User.Id}")
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
