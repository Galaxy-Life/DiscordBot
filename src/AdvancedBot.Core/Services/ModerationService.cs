using System;
using System.Threading.Tasks;
using AdvancedBot.Core.Entities;
using AdvancedBot.Core.Entities.Enums;
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
                return new ModResult(ModResultType.NotFound);
            }

            if (user.Role == PhoenixRole.Banned)
            {
                return new ModResult(ModResultType.AlreadyDone, user);
            }

            if (!await _gl.TryBanUser(userId, reason))
            {
                return new ModResult(ModResultType.BackendError, user);
            }

            await _gl.TryKickUserOfflineAsync(userId.ToString());
            await _logs.LogGameActionAsync(LogAction.Ban, discordId, userId, reason);

            return new ModResult(ModResultType.Success, user);
        }

        public async Task<ModResult> UnbanUserAsync(ulong discordId, uint userId)
        {
            var user = await _gl.GetPhoenixUserAsync(userId);

            if (user == null)
            {
                return new ModResult(ModResultType.NotFound);
            }

            if (user.Role != PhoenixRole.Banned)
            {
                return new ModResult(ModResultType.AlreadyDone, user);
            }

            if (!await _gl.TryUnbanUser(userId))
            {
                return new ModResult(ModResultType.BackendError, user);
            }

            await _logs.LogGameActionAsync(LogAction.Unban, discordId, userId);
            return new ModResult(ModResultType.Success, user);
        }

        public async Task<ModResult> AddBetaToUserAsync(ulong discordId, uint userId)
        {
            var user = await _gl.GetPhoenixUserAsync(userId);

            if (user == null)
            {
                return new ModResult(ModResultType.NotFound);
            }

            if (!await _gl.AddGlBeta(userId))
            {
                return new ModResult(ModResultType.BackendError, user);
            }

            await _logs.LogGameActionAsync(LogAction.AddBeta, discordId, userId);
            return new ModResult(ModResultType.Success, user);
        }

        public async Task<ModResult> RemoveBetaFromUserAsync(ulong discordId, uint userId)
        {
            var user = await _gl.GetPhoenixUserAsync(userId);

            if (user == null)
            {
                return new ModResult(ModResultType.NotFound);
            }

            if (!await _gl.RemoveGlBeta(userId))
            {
                return new ModResult(ModResultType.BackendError, user);
            }

            await _logs.LogGameActionAsync(LogAction.RemoveBeta, discordId, userId);
            return new ModResult(ModResultType.Success, user);
        }

        public async Task<ModResult> GiveRoleAsync(ulong discordId, uint userId, PhoenixRole role)
        {
            var user = await _gl.GetPhoenixUserAsync(userId);

            if (user == null)
            {
                return new ModResult(ModResultType.NotFound);
            }

            if (user.Role == role)
            {
                return new ModResult(ModResultType.AlreadyDone, user);
            }    

            if (!await _gl.GiveRoleAsync(userId, role))
            {
                return new ModResult(ModResultType.BackendError, user);
            }

            await _logs.LogGameActionAsync(LogAction.GiveRole, discordId, userId, role.ToString());
            return new ModResult(ModResultType.Success, user);
        }

        public async Task<ModResult> GetChipsBoughtAsync(ulong discordId, uint userId)
        {
            var user = await _gl.GetUserById(userId.ToString());

            if (user == null)
            {
                return new ModResult(ModResultType.NotFound);
            }

            var chipsBought = await _gl.GetChipsBoughtAsync(userId.ToString());

            await _logs.LogGameActionAsync(LogAction.GetChipsBought, discordId, userId);
            return new ModResult(ModResultType.Success, null, user) { IntValue = chipsBought};
        }

        public async Task<ModResult> AddChipsAsync(ulong discordId, uint userId, int amount)
        {
            var user = await _gl.GetUserById(userId.ToString());

            if (user == null)
            {
                return new ModResult(ModResultType.NotFound);
            }
            
            if (!await _gl.TryAddChipsToUserAsync(userId.ToString(), amount))
            {
                return new ModResult(ModResultType.BackendError, null, user);
            }

            await _logs.LogGameActionAsync(LogAction.AddChips, discordId, userId, amount.ToString());
            return new ModResult(ModResultType.Success, null, user);
        }

        public async Task<ModResult> AddItemsAsync(ulong discordId, uint userId, string sku, int amount)
        {
            var user = await _gl.GetUserById(userId.ToString());

            if (user == null)
            {
                return new ModResult(ModResultType.NotFound);
            }
            
            if (!await _gl.TryAddItemToUserAsync(userId.ToString(), sku, amount))
            {
                return new ModResult(ModResultType.BackendError, null, user);
            }

            await _logs.LogGameActionAsync(LogAction.AddItem, discordId, userId, $"{sku}:{amount}");
            return new ModResult(ModResultType.Success, null, user);
        }
    }
}
