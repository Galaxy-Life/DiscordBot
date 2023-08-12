using System.Threading.Tasks;
using AdvancedBot.Core.Commands.Preconditions;
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

        [SlashCommand("battlelogs", "")]
        public async Task BattlelogsAsync(uint userId)
        {
            var result = await ModService.GetBattleLogTelemetry(Context.User.Id, userId);
            await SendResponseMessage(result.Message, false);
        }

        [SlashCommand("gifts", "")]
        public async Task GiftsAsync(uint userId)
        {
            var result = await ModService.GetGiftsTelemetry(Context.User.Id, userId);
            var user = await GLService.GetUserProfileAsync(userId.ToString());

            var templateEmbed = new EmbedBuilder()
            {
                Title = $"Gifts Telemetry for {user.User.Name} ({userId})"
            };
        }

        [SlashCommand("logins", "")]
        public async Task LoginsAsync(uint userId)
        {
            var result = await ModService.GetLoginsTelemetry(Context.User.Id, userId);
            await SendResponseMessage(result.Message, false);
        }
    }
}
