using System.Threading.Tasks;
using AdvancedBot.Core.Commands.Preconditions;
using AdvancedBot.Core.Entities.Enums;
using AdvancedBot.Core.Services;
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
        private LogService _log;

        public ModerationModule(GLClient client, LogService log)
        {
            _client = client;
            _log = log;
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

            _log.LogGameAction(LogAction.Ban, Context.User.Id, userId, reason);

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

            _log.LogGameAction(LogAction.Ban, Context.User.Id, userId);

            var embed = new EmbedBuilder()
            {
                Title = $"{user.UserName} ({user.UserId}) is no longer banned in-game!",
                Color = Color.Green
            };

            await ModifyOriginalResponseAsync(x => x.Embed = embed.Build() );
        }

        [SlashCommand("addbeta", "Adds GL Beta to a user")]
        public async Task AddBetaToUserAsync(uint userId)
        {
            var user = await _client.GetPhoenixUserAsync(userId);

            if (!await _client.AddGlBeta(userId))
            {
                await ModifyOriginalResponseAsync(x => x.Content = $"Failed to add beta access to {user.UserName} ({user.UserId})");
                return;
            }

            _log.LogGameAction(LogAction.AddBeta, Context.User.Id, userId);

            var embed = new EmbedBuilder()
            {
                Title = $"{user.UserName} ({user.UserId}) now has access to beta",
                Color = Color.Green
            };

            await ModifyOriginalResponseAsync(x => x.Embed = embed.Build() );
        }

        [SlashCommand("removebeta", "Adds GL Beta to a user")]
        public async Task RemoveBetaFromUserAsync(uint userId)
        {
            var user = await _client.GetPhoenixUserAsync(userId);

            if (!await _client.RemoveGlBeta(userId))
            {
                await ModifyOriginalResponseAsync(x => x.Content = $"Failed to remove beta access to {user.UserName} ({user.UserId})");
                return;
            }

            _log.LogGameAction(LogAction.RemoveBeta, Context.User.Id, userId);

            var embed = new EmbedBuilder()
            {
                Title = $"{user.UserName} ({user.UserId}) no longer has beta access",
                Color = Color.Red
            };

            await ModifyOriginalResponseAsync(x => x.Embed = embed.Build() );
        }
    }
}
