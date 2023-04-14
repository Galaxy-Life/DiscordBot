using System.Threading.Tasks;
using AdvancedBot.Core.Entities;
using AdvancedBot.Core.Entities.Enums;
using Discord;
using GL.NET;
using GL.NET.Entities;

namespace AdvancedBot.Core.Services
{
    public class ModerationService
    {
        private AuthorizedGLClient _gl;
        private LogService _logs;

        public ModerationService(AuthorizedGLClient gl, LogService logs)
        {
            _gl = gl;
            _logs = logs;
        }

        public async Task<ModResult> BanUserAsync(ulong discordId, uint userId, string reason)
        {
            var user = await _gl.GetPhoenixUserAsync(userId);

            if (user == null)
            {
                return new ModResult(ModResultType.NotFound, new ResponseMessage($"No user found for **{userId}**"));
            }

            if (user.Role == PhoenixRole.Banned)
            {
                return new ModResult(ModResultType.AlreadyDone, new ResponseMessage($"{user.UserName} ({user.UserId}) is not banned"), user);
            }

            if (!await _gl.TryBanUser(userId, reason))
            {
                return new ModResult(ModResultType.BackendError, new ResponseMessage($"Failed to ban {user.UserName} ({user.UserId})"), user);
            }

            await _gl.TryKickUserOfflineAsync(userId.ToString());
            await _logs.LogGameActionAsync(LogAction.Ban, discordId, userId, reason);

            var embed = new EmbedBuilder()
            {
                Title = $"{user.UserName} ({user.UserId}) is now banned in-game!",
                Color = Color.Red
            };

            var message = new ResponseMessage(embeds: new Embed[] { embed.Build() });
            return new ModResult(ModResultType.Success, message, user);
        }

        public async Task<ModResult> UnbanUserAsync(ulong discordId, uint userId)
        {
            var user = await _gl.GetPhoenixUserAsync(userId);

            if (user == null)
            {
                return new ModResult(ModResultType.NotFound, new ResponseMessage($"No user found for **{userId}**"));
            }

            if (user.Role != PhoenixRole.Banned)
            {
                return new ModResult(ModResultType.AlreadyDone, new ResponseMessage($"{user.UserName} ({user.UserId}) is not banned"), user);
            }

            if (!await _gl.TryUnbanUser(userId))
            {
                return new ModResult(ModResultType.BackendError, new ResponseMessage($"Failed to unban {user.UserName} ({user.UserId})"), user);
            }

            await _logs.LogGameActionAsync(LogAction.Unban, discordId, userId);

            var embed = new EmbedBuilder()
            {
                Title = $"{user.UserName} ({user.UserId}) is no longer banned in-game!",
                Color = Color.Green
            };

            var message = new ResponseMessage(embeds: new Embed[] { embed.Build() });
            return new ModResult(ModResultType.Success, message, user);
        }

        public async Task<ModResult> AddBetaToUserAsync(ulong discordId, uint userId)
        {
            var user = await _gl.GetPhoenixUserAsync(userId);

            if (user == null)
            {
                return new ModResult(ModResultType.NotFound, new ResponseMessage($"No user found for **{userId}**"));
            }

            if (!await _gl.AddGlBeta(userId))
            {
                return new ModResult(ModResultType.BackendError, new ResponseMessage($"Failed to add beta access to {user.UserName} ({user.UserId})"));
            }

            await _logs.LogGameActionAsync(LogAction.AddBeta, discordId, userId);

            var embed = new EmbedBuilder()
            {
                Title = $"{user.UserName} ({user.UserId}) now has access to beta",
                Color = Color.Green
            };

            var message = new ResponseMessage(embeds: new Embed[] { embed.Build() });
            return new ModResult(ModResultType.Success, message, user);
        }

        public async Task<ModResult> RemoveBetaFromUserAsync(ulong discordId, uint userId)
        {
            var user = await _gl.GetPhoenixUserAsync(userId);

            if (user == null)
            {
                return new ModResult(ModResultType.NotFound, new ResponseMessage($"No user found for **{userId}**"));
            }

            if (!await _gl.RemoveGlBeta(userId))
            {
                return new ModResult(ModResultType.BackendError, new ResponseMessage($"Failed to remove beta access from {user.UserName} ({user.UserId})"));
            }

            await _logs.LogGameActionAsync(LogAction.RemoveBeta, discordId, userId);

            var embed = new EmbedBuilder()
            {
                Title = $"{user.UserName} ({user.UserId}) no longer has beta access",
                Color = Color.Green
            };

            var message = new ResponseMessage(embeds: new Embed[] { embed.Build() });
            return new ModResult(ModResultType.Success, message, user);
        }

        public async Task<ModResult> GiveRoleAsync(ulong discordId, uint userId, PhoenixRole role)
        {
            var user = await _gl.GetPhoenixUserAsync(userId);

            if (user == null)
            {
                return new ModResult(ModResultType.NotFound, new ResponseMessage($"No user found for **{userId}**"));
            }

            var roleText = role == PhoenixRole.Donator ? "a Donator"
                : role == PhoenixRole.Staff ? "a Staff Member"
                : role == PhoenixRole.Administrator ? "an Admin"
                : role.ToString();

            if (user.Role == role)
            {
                return new ModResult(ModResultType.AlreadyDone, new ResponseMessage($"User is already {roleText}!"));
            }    

            if (!await _gl.GiveRoleAsync(userId, role))
            {
                return new ModResult(ModResultType.BackendError, new ResponseMessage($"Failed to give {role} to {user.UserName} ({user.UserId})"), user);
            }

            await _logs.LogGameActionAsync(LogAction.GiveRole, discordId, userId, role.ToString());

            var embed = new EmbedBuilder()
            {
                Title = $"{user.UserName} ({user.UserId}) is now {roleText}",
                Color = Color.Blue
            };

            var message = new ResponseMessage(embeds: new Embed[] { embed.Build() });
            return new ModResult(ModResultType.Success, message, user);
        }

        public async Task<ModResult> GetChipsBoughtAsync(ulong discordId, uint userId)
        {
            var user = await _gl.GetUserById(userId.ToString());

            if (user == null)
            {
                return new ModResult(ModResultType.NotFound, new ResponseMessage($"No user found for **{userId}**"));
            }

            var chipsBought = await _gl.GetChipsBoughtAsync(userId.ToString());
            await _logs.LogGameActionAsync(LogAction.GetChipsBought, discordId, userId);

            var embed = new EmbedBuilder()
            {
                Title = $"{user.Name} ({user.Id})",
                Description = $"**{chipsBought}** chips bought",
                Color = Color.Blue
            };

            var message = new ResponseMessage(embeds: new Embed[] { embed.Build() });
            return new ModResult(ModResultType.Success, message, null, user) { IntValue = chipsBought };
        }

        public async Task<ModResult> AddChipsAsync(ulong discordId, uint userId, int amount)
        {
            var user = await _gl.GetUserById(userId.ToString());

            if (user == null)
            {
                return new ModResult(ModResultType.NotFound, new ResponseMessage($"No user found for **{userId}**"));
            }
            
            if (!await _gl.TryAddChipsToUserAsync(userId.ToString(), amount))
            {
                return new ModResult(ModResultType.BackendError, new ResponseMessage($"Failed to add chips to {user.Name} ({user.Id})"), null, user);
            }

            await _logs.LogGameActionAsync(LogAction.AddChips, discordId, userId, amount.ToString());

            var embed = new EmbedBuilder()
            {
                Title = $"Added {amount} chips to {user.Name} ({user.Id})",
                Color = Color.Blue
            };

            var message = new ResponseMessage(embeds: new Embed[] { embed.Build() });
            return new ModResult(ModResultType.Success, message, null, user);
        }

        public async Task<ModResult> AddItemsAsync(ulong discordId, uint userId, string sku, int amount)
        {
            var user = await _gl.GetUserById(userId.ToString());

            if (user == null)
            {
                return new ModResult(ModResultType.NotFound, new ResponseMessage($"No user found for **{userId}**"));
            }
            
            if (!await _gl.TryAddItemToUserAsync(userId.ToString(), sku, amount))
            {
                return new ModResult(ModResultType.BackendError, new ResponseMessage($"Failed to add item with sku {sku} to {user.Name} ({user.Id})"), null, user);
            }

            await _logs.LogGameActionAsync(LogAction.AddItem, discordId, userId, $"{sku}:{amount}");

            var embed = new EmbedBuilder()
            {
                Title = $"Added item {sku} {amount}x to {user.Name} ({user.Id})",
                Color = Color.Blue
            };

            var message = new ResponseMessage(embeds: new Embed[] { embed.Build() });
            return new ModResult(ModResultType.Success, message, null, user);
        }
    }
}
