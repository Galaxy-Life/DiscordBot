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

namespace AdvancedBot.Core.Services
{
    public class GLService
    {
        private readonly GLClient _client;

        public GLService(GLClient client)
        {
            _client = client;
        }

        public async Task<ModResult> GetUserProfileAsync(string input)
        {
            var phoenixUser = await GetPhoenixUserByInput(input);

            if (phoenixUser == null)
            {
                return new ModResult(ModResultType.NotFound, message: new ResponseMessage($"<:shrugR:945740284308893696> No user found for **{input}**"));
            }

            var user = await GetUserByInput(input);

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

            var stats = await _client.Api.GetUserStats(user.Id);

            var steamId = phoenixUser.SteamId ?? "No steam linked";
            var roleText = phoenixUser.Role == PhoenixRole.Banned ? "[BANNED]"
                : phoenixUser.Role == PhoenixRole.Donator ? "[Donator]"
                : phoenixUser.Role == PhoenixRole.Staff ? "[Staff]"
                : phoenixUser.Role == PhoenixRole.Administrator ? "[Admin]"
                : "";

            var color = phoenixUser.Role == PhoenixRole.Banned ? Color.Default
                : phoenixUser.Role == PhoenixRole.Donator ? new Color(15710778)
                : phoenixUser.Role == PhoenixRole.Staff ? new Color(2605694)
                : phoenixUser.Role == PhoenixRole.Administrator ? Color.DarkRed
                : Color.LightGrey;

            var displayAlliance = "User is not in an alliance";

            if (!string.IsNullOrEmpty(user.AllianceId))
            {
                var alliance = await _client.Api.GetAlliance(user.AllianceId);

                // can happen due to 24hour player delay
                if (alliance == null)
                {
                    displayAlliance = "User has recently changed alliance, please wait for it to update!";
                }
                else
                {
                    displayAlliance = $"User is in **{alliance.Name}**.";
                }
            }

            var embed = new EmbedBuilder()
                .WithTitle($"{roleText} {phoenixUser.UserName}")
                .WithDescription($"Id: **{phoenixUser.UserId}**\n{displayAlliance}\n\u200b")
                .WithColor(color)
                .WithFooter($"Account created on {phoenixUser.Created.GetValueOrDefault().ToString("dd MMMM yyyy a\\t HH:mm")}");

            if (!string.IsNullOrEmpty(phoenixUser.SteamId))
            {
                embed.WithUrl($"https://steamcommunity.com/profiles/{steamId.Replace("\"", "")}");
            }

            if (user != null)
            {
                embed.WithThumbnailUrl(user.Avatar);
            }

            embed.AddField("Level", FormatNumbers(user.Level), true);
            embed.AddField("Starbase", user.Planets[0].HQLevel, true);
            embed.AddField("Colonies", user.Planets.Count(x => x != null) - 1, true);

            var message = new ResponseMessage("", new Embed[] { embed.Build() });
            return new ModResult(ModResultType.Success, message, phoenixUser, user);
        }

        public async Task<ModResult> GetUserStatsAsync(string input)
        {
            var user = await GetUserByInput(input);

            if (user == null)
            {
                return new ModResult(ModResultType.NotFound, message: new ResponseMessage($"<:shrugR:945740284308893696> No user found for **{input}**"));
            }

            var stats = await _client.Api.GetUserStats(user.Id);
            var displayAlliance = string.IsNullOrEmpty(user.AllianceId) ? "User is not in any alliance." : $"User is part of **{user.AllianceId}**.";

            var embed = new EmbedBuilder()
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
            .AddField("Troopsize donated", stats.TroopSizesDonated, true);

            var message = new ResponseMessage("", new Embed[] { embed.Build() });
            return new ModResult(ModResultType.Success, message, null, user);
        }

        public async Task<ModResult> GetAllianceAsync(string input)
        {
            var alliance = await _client.Api.GetAlliance(input);

            if (alliance == null)
            {
                return new ModResult(ModResultType.NotFound, message: new ResponseMessage($"<:shrugR:945740284308893696> No alliance found for **{input}**"));
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
            .WithFooter($"Run /members {input} to see its members.");

            if (alliance.InWar)
            {
                embed.AddField("In War With", alliance.OpponentAllianceId, true);
            }

            var message = new ResponseMessage("", new Embed[] { embed.Build() });
            return new ModResult(ModResultType.Success, message, null, null) { Alliance = alliance };
        }

        public async Task<ModResult> GetAllianceMembersAsync(string input)
        {
            var alliance = await _client.Api.GetAlliance(input);

            if (alliance == null)
            {
                return new ModResult(ModResultType.NotFound, message: new ResponseMessage($"<:shrugR:945740284308893696> No alliance found for **{input}**"));
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
            .AddField($"Members ({regulars.Count()})", string.IsNullOrEmpty(formattedMembers) ? "None" : formattedMembers);

            var message = new ResponseMessage("", new Embed[] { embed.Build() });
            return new ModResult(ModResultType.Success, message, null, null) { Alliance = alliance };
        }

        private async Task<User> GetUserByInput(string input)
        {
            User profile = null;
            var digitString = new String(input.Where(Char.IsDigit).ToArray());

            if (digitString.Length == input.Length)
            {
                profile = await _client.Api.GetUserById(input);
            }

            if (profile == null)
            {
                profile = await _client.Api.GetUserByName(input);
            }

            return profile;
        }

        private async Task<PhoenixUser> GetPhoenixUserByInput(string input, bool full = false)
        {
            PhoenixUser user = null;

            var digitString = new String(input.Where(Char.IsDigit).ToArray());

            // extra check to see if all characters were numbers
            if (digitString.Length == input.Length)
            {
                if (full)
                {
                    user = await _client.Phoenix.GetFullPhoenixUserAsync(input);
                }
                else
                {
                    user = await _client.Phoenix.GetPhoenixUserAsync(input);
                }
            }

            // try to get user by name
            if (user == null)
            {
                user = await _client.Phoenix.GetPhoenixUserByNameAsync(input);
            }

            // get user by id after getting it by name
            if (user != null && full)
            {
                user = await _client.Phoenix.GetFullPhoenixUserAsync(user.UserId);
            }

            return user;
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
