using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AdvancedBot.Core.Commands.Preconditions;
using AdvancedBot.Core.Entities;
using AdvancedBot.Core.Services;
using Discord;
using Discord.Interactions;

namespace AdvancedBot.Core.Commands.Modules
{
    [DontAutoRegister]
    [RequirePrivateList]
    [Group("telemetry", "Handles all commands regarding telemetry")]
    public class TelemetryModule : TopModule
    {
        public ModerationService ModService { get; set; }
        public GLService GLService { get; set; }

        [SlashCommand("battlelogs", "Shows information about a users battlelogs")]
        public async Task BattlelogsAsync(uint userId)
        {
            var result = await ModService.GetBattleLogTelemetry(Context.User.Id, userId);
            await SendResponseMessage(result.Message, false);
        }

        [SlashCommand("gifts", "Shows information about a users gifts")]
        public async Task GiftsAsync(uint userId)
        {
            var result = await ModService.GetGiftsTelemetry(Context.User.Id, userId);
            var user = await GLService.GetUserProfileAsync(userId.ToString());

            var users = new Dictionary<string, int>();

            for (int i = 0; i < result.Output.Count; i++)
            {
                var gift = result.Output[i];

                if (!users.TryGetValue(gift.FromUserId, out int value))
                {
                    users.Add(gift.FromUserId, 0);
                }

                users[gift.FromUserId] = value + 1;
            }

            var fields = users.OrderByDescending(x => x.Value).Select(x => new EmbedFieldBuilder() { Name = $"Id: {x.Key}", Value = $"Items sent: {x.Value}" }.Build());

            var templateEmbed = new EmbedBuilder()
                .WithTitle($"Gifts Telemetry for {user.User.Name} ({userId})");

            await SendPaginatedMessageAsync(fields, null, templateEmbed);
        }

        [SlashCommand("logins", "Shows information about a users logins")]
        public async Task LoginsAsync(uint userId)
        {
            var result = await ModService.GetLoginsTelemetry(Context.User.Id, userId);
            await SendResponseMessage(result.Message, false);
        }

        [SlashCommand("accounts", "Shows information about linked accounts to this user")]
        public async Task GetPossibleAltsAsync(uint userId)
        {
            var result = await ModService.GetPossibleAlts(Context.User.Id, userId);
            var user = await GLService.GetUserProfileAsync(userId.ToString());

            if (user.User == null)
            {
                await SendResponseMessage(new ResponseMessage() { Content = "No Galaxy Life Data for this user." }, false);
                return;
            }

            var fields = new List<EmbedField>();

            for (int i = 0; i < result.Output.Count; i++)
            {
                string trackerKey = result.Output.ElementAt(i).Key;
                var trackerResult = result.Output.ElementAt(i).Value;

                var a = trackerResult.First().Value;

                fields.Add(new EmbedFieldBuilder() { Name = "Fingerprint Id", Value = trackerKey }.Build());
                fields.AddRange(
                    trackerResult
                        .OrderByDescending(x => x.Value.AmountOfLogins)
                        .Select(result => new EmbedFieldBuilder()
                            .WithName($"Id: {result.Key}")
                            .WithValue($"Times 'logged in': {result.Value.AmountOfLogins}\nLast Login: {result.Value.LastLogin}")
                            .Build()));
            }

            var templateEmbed = new EmbedBuilder()
                  .WithTitle($"Possible Connections to {user.User.Name} ({userId})");

            await SendPaginatedMessageAsync(fields, null, templateEmbed);
        }
    }
}
