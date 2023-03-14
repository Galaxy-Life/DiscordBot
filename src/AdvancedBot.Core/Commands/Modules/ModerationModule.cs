using System.Threading.Tasks;
using AdvancedBot.Core.Commands.Preconditions;
using Discord;
using Discord.Interactions;
using GL.NET;
using GL.NET.Entities;

namespace AdvancedBot.Core.Commands.Modules
{
    [RequirePrivateList]
    [DontAutoRegister]
    [Group("game", "All commands handling in-game actions")]
    public class ModerationModule : TopModule
    {
        private GLClient _client;

        public ModerationModule(GLClient client)
        {
            _client = client;
        }

        [SlashCommand("ban", "Tries to ban a user")]
        public async Task TryBanUserAsync(uint userId, string reason)
        {
            var user = await _client.GetPhoenixUserAsync(userId);

            if (user.Role == PhoenixRole.Banned)
            {
                await ModifyOriginalResponseAsync(x => x.Content = $"{user.UserName} ({user.UserId}) is already banned!");
                return;
            }

            if (!await _client.TryBanUser(userId, reason))
            {
                await ModifyOriginalResponseAsync(x => x.Content = $"Failed to ban {user.UserName} ({user.UserId}).");
                return;
            }

            var embed = new EmbedBuilder()
            {
                Title = $"{user.UserName} ({user.UserId}) is now banned in-game!",
                Color = Color.Red
            };

            await ModifyOriginalResponseAsync(x => x.Embed = embed.Build() );
        }

        [SlashCommand("unban", "Tries to unban a user")]
        public async Task TryUnbanUserAsync(uint userId)
        {
            var user = await _client.GetPhoenixUserAsync(userId);

            if (user.Role != PhoenixRole.Banned)
            {
                await ModifyOriginalResponseAsync(x => x.Content = $"{user.UserName} ({user.UserId}) is not banned!");
                return;
            }

            if (!await _client.TryUnbanUser(userId))
            {
                await ModifyOriginalResponseAsync(x => x.Content = $"Failed to unban {user.UserName} ({user.UserId}).");
                return;
            }

            var embed = new EmbedBuilder()
            {
                Title = $"{user.UserName} ({user.UserId}) is no longer banned in-game!",
                Color = Color.Green
            };

            await ModifyOriginalResponseAsync(x => x.Embed = embed.Build() );
        }
    }
}
