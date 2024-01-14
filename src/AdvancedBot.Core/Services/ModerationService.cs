using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;
using AdvancedBot.Core.Entities;
using AdvancedBot.Core.Entities.Enums;
using AdvancedBot.Core.Services.DataStorage;
using Discord;
using GL.NET;
using GL.NET.Entities;

namespace AdvancedBot.Core.Services
{
    public class ModerationService
    {
        private readonly GLClient _gl;
        private readonly LogService _logs;
        private readonly BotStorage _storage;

        private readonly Timer _banTimer = new Timer(1000 * 60 * 30);

        public ModerationService(GLClient gl, LogService logs, BotStorage storage)
        {
            _gl = gl;
            _logs = logs;
            _storage = storage;

            _banTimer.Elapsed += OnBanTimer;
            _banTimer.Start();
            OnBanTimer(null, null);
        }

        public async Task<ModResult> BanUserAsync(ulong discordId, uint userId, string reason, uint days = 0)
        {
            var user = await _gl.Phoenix.GetPhoenixUserAsync(userId);

            if (user == null)
            {
                return new ModResult(ModResultType.NotFound, new ResponseMessage($"No user found for **{userId}**"));
            }

            if (user.Role == PhoenixRole.Banned)
            {
                return new ModResult(ModResultType.AlreadyDone, new ResponseMessage($"{user.UserName} ({user.UserId}) is already banned"), user);
            }

            if (!await _gl.Phoenix.TryBanUser(userId, reason))
            {
                return new ModResult(ModResultType.BackendError, new ResponseMessage($"Failed to ban {user.UserName} ({user.UserId})"), user);
            }

            var embed = new EmbedBuilder()
            {
                Color = Color.Red
            };

            DateTime? banDuration = null;

            if (days == 0)
            {
                embed.WithTitle($"{user.UserName} ({user.UserId}) is now banned in-game permanently!");
            }
            else
            {
                embed.WithTitle($"{user.UserName} ({user.UserId}) is now banned in-game!");
                embed.WithDescription($"**Duration:** {days} days");

                banDuration = DateTime.UtcNow.AddDays(days);
                _storage.AddTempBan(new Tempban(discordId, userId, banDuration.Value));
            }

            await _gl.Production.TryKickUserOfflineAsync(userId.ToString());
            await _logs.LogGameActionAsync(LogAction.Ban, discordId, userId, reason, banDuration);

            var message = new ResponseMessage(embeds: new Embed[] { embed.Build() });
            return new ModResult(ModResultType.Success, message, user);
        }

        public async Task<ModResult> UnbanUserAsync(ulong discordId, uint userId, bool auto = false)
        {
            var user = await _gl.Phoenix.GetPhoenixUserAsync(userId);

            if (user == null)
            {
                return new ModResult(ModResultType.NotFound, new ResponseMessage($"No user found for **{userId}**"));
            }

            if (user.Role != PhoenixRole.Banned)
            {
                return new ModResult(ModResultType.AlreadyDone, new ResponseMessage($"{user.UserName} ({user.UserId}) is not banned"), user);
            }

            if (!await _gl.Phoenix.TryUnbanUser(userId))
            {
                return new ModResult(ModResultType.BackendError, new ResponseMessage($"Failed to unban {user.UserName} ({user.UserId})"), user);
            }

            var extra = auto ? "Auto Unban" : "";
            await _logs.LogGameActionAsync(LogAction.Unban, discordId, userId, extra);

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
            var user = await _gl.Phoenix.GetPhoenixUserAsync(userId);

            if (user == null)
            {
                return new ModResult(ModResultType.NotFound, new ResponseMessage($"No user found for **{userId}**"));
            }

            if (!await _gl.Phoenix.AddGlBeta(userId))
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
            var user = await _gl.Phoenix.GetPhoenixUserAsync(userId);

            if (user == null)
            {
                return new ModResult(ModResultType.NotFound, new ResponseMessage($"No user found for **{userId}**"));
            }

            if (!await _gl.Phoenix.RemoveGlBeta(userId))
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

        public async Task<ModResult> AddEmulateToUserAsync(ulong discordId, uint userId)
        {
            var user = await _gl.Phoenix.GetPhoenixUserAsync(userId);

            if (user == null)
            {
                return new ModResult(ModResultType.NotFound, new ResponseMessage($"No user found for **{userId}**"));
            }

            if (!await _gl.Phoenix.AddEmulate(userId))
            {
                return new ModResult(ModResultType.BackendError, new ResponseMessage($"Failed to add emulate access to {user.UserName} ({user.UserId})"));
            }

            await _logs.LogGameActionAsync(LogAction.AddEmulate, discordId, userId);

            var embed = new EmbedBuilder()
            {
                Title = $"{user.UserName} ({user.UserId}) now has emulate access",
                Color = Color.Green
            };

            var message = new ResponseMessage(embeds: new Embed[] { embed.Build() });
            return new ModResult(ModResultType.Success, message, user);
        }

        public async Task<ModResult> RemoveEmulateFromUserAsync(ulong discordId, uint userId)
        {
            var user = await _gl.Phoenix.GetPhoenixUserAsync(userId);

            if (user == null)
            {
                return new ModResult(ModResultType.NotFound, new ResponseMessage($"No user found for **{userId}**"));
            }

            if (!await _gl.Phoenix.RemoveGlBeta(userId))
            {
                return new ModResult(ModResultType.BackendError, new ResponseMessage($"Failed to remove emulate access from {user.UserName} ({user.UserId})"));
            }

            await _logs.LogGameActionAsync(LogAction.RemoveEmulate, discordId, userId);

            var embed = new EmbedBuilder()
            {
                Title = $"{user.UserName} ({user.UserId}) no longer has emulate access",
                Color = Color.Green
            };

            var message = new ResponseMessage(embeds: new Embed[] { embed.Build() });
            return new ModResult(ModResultType.Success, message, user);
        }

        public async Task<ModResult> GiveRoleAsync(ulong discordId, uint userId, PhoenixRole role)
        {
            var user = await _gl.Phoenix.GetPhoenixUserAsync(userId);

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

            if (!await _gl.Phoenix.GiveRoleAsync(userId, role))
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
            var user = await _gl.Api.GetUserById(userId.ToString());

            if (user == null)
            {
                return new ModResult(ModResultType.NotFound, new ResponseMessage($"No user found for **{userId}**"));
            }

            var chipsBought = await _gl.Api.GetChipsBoughtAsync(userId.ToString());
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

        public async Task<ModResult> GetChipsSpentAsync(ulong discordId, uint userId)
        {
            var user = await _gl.Api.GetUserById(userId.ToString());

            if (user == null)
            {
                return new ModResult(ModResultType.NotFound, new ResponseMessage($"No user found for **{userId}**"));
            }

            var chipsSpent = await _gl.Api.GetChipsSpentAsync(userId.ToString());
            await _logs.LogGameActionAsync(LogAction.GetChipsSpent, discordId, userId);

            var embed = new EmbedBuilder()
            {
                Title = $"{user.Name} ({user.Id})",
                Description = $"**{chipsSpent}** chips spent",
                Color = Color.Blue
            };

            var message = new ResponseMessage(embeds: new Embed[] { embed.Build() });
            return new ModResult(ModResultType.Success, message, null, user) { IntValue = chipsSpent };
        }

        public async Task<ModResult> AddChipsAsync(ulong discordId, uint userId, int amount)
        {
            var user = await _gl.Api.GetUserById(userId.ToString());

            if (user == null)
            {
                return new ModResult(ModResultType.NotFound, new ResponseMessage($"No user found for **{userId}**"));
            }
            
            if (!await _gl.Production.TryAddChipsToUserAsync(userId.ToString(), amount))
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
            var user = await _gl.Api.GetUserById(userId.ToString());

            if (user == null)
            {
                return new ModResult(ModResultType.NotFound, new ResponseMessage($"No user found for **{userId}**"));
            }
            
            if (!await _gl.Production.TryAddItemToUserAsync(userId.ToString(), sku, amount))
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

        public async Task<ModResult> AddXpAsync(ulong discordId, uint userId, int amount)
        {
            var user = await _gl.Api.GetUserById(userId.ToString());

            if (user == null)
            {
                return new ModResult(ModResultType.NotFound, new ResponseMessage($"No user found for **{userId}**"));
            }
            
            if (!await _gl.Production.TryAddXpToUserAsync(userId.ToString(), amount))
            {
                return new ModResult(ModResultType.BackendError, new ResponseMessage($"Failed to add xp to {user.Name} ({user.Id})"), null, user);
            }

            await _logs.LogGameActionAsync(LogAction.AddXp, discordId, userId, $"{amount}");

            var embed = new EmbedBuilder()
            {
                Title = $"Added {amount} xp to {user.Name} ({user.Id})",
                Color = Color.Blue
            };

            var message = new ResponseMessage(embeds: new Embed[] { embed.Build() });
            return new ModResult(ModResultType.Success, message, null, user);
        }

        public async Task<ModResult> EnableMaintenance(ulong discordId, uint minutes)
        {
            var success = await _gl.Production.EnableMaintenance(minutes);

            if (!success)
            {
                return new ModResult(ModResultType.BackendError, new ResponseMessage($"Failed to enable maintenance for {minutes} minutes"));
            }

            await _logs.LogGameActionAsync(LogAction.EnableMaintenance, discordId, 0, minutes.ToString());

            var embed = new EmbedBuilder()
            {
                Title = $"Enabled maintenance for {minutes} minutes!",
                Color = Color.Red
            };

            var message = new ResponseMessage(embeds: new Embed[] { embed.Build() });
            return new ModResult(ModResultType.Success, message);
        }

        public async Task<ModResult> ReloadRules(ulong discordId, bool staging = false)
        {
            var success = staging ? await _gl.Staging.ReloadRules() : await _gl.Production.ReloadRules();
            var stagingText = staging ? "staging " : "";

            if (!success)
            {
                return new ModResult(ModResultType.BackendError, new ResponseMessage($"Failed to reload rules on the {stagingText}backend!"));
            }

            await _logs.LogGameActionAsync(LogAction.ReloadRules, discordId, 0, stagingText.TrimEnd());

            var embed = new EmbedBuilder()
            {
                Title = $"Reloaded rules on the {stagingText}server!",
                Color = Color.Red
            };

            var message = new ResponseMessage(embeds: new Embed[] { embed.Build() });
            return new ModResult(ModResultType.Success, message);
        }
    
        public async Task<ModResult> RunStagingKickerAsync(ulong discordId)
        {
            var success = await _gl.Staging.RunKicker();

            if (!success)
            {
                return new ModResult(ModResultType.BackendError, new ResponseMessage($"Failed to run kicker on staging!"));
            }

            await _logs.LogGameActionAsync(LogAction.RunKicker, discordId, 0, "Staging");

            var embed = new EmbedBuilder()
            {
                Title = $"Ran kicker on staging!",
                Color = Color.Red
            };

            var message = new ResponseMessage(embeds: new Embed[] { embed.Build() });
            return new ModResult(ModResultType.Success, message);
        }

        public async Task<ModResult> ResetHelpsStagingAsync(ulong discordId, uint userId)
        {
            var success = await _gl.Staging.ResetPlanetHelps(userId.ToString());

            if (!success)
            {
                return new ModResult(ModResultType.BackendError, new ResponseMessage($"Failed to reset helps for {userId}!"));
            }

            await _logs.LogGameActionAsync(LogAction.ResetHelps, discordId, 0, "Staging");

            var embed = new EmbedBuilder()
            {
                Title = $"Reset helps for {userId} on staging!",
                Color = Color.Red
            };

            var message = new ResponseMessage(embeds: new Embed[] { embed.Build() });
            return new ModResult(ModResultType.Success, message);
        }

        public async Task<ModResult> ForceWarStagingAsync(ulong discordId, string allianceA, string allianceB)
        {
            var success = await _gl.Staging.ForceWar(allianceA, allianceB);

            if (!success)
            {
                return new ModResult(ModResultType.BackendError, new ResponseMessage($"Failed to force war between **{allianceA}** and **{allianceB}**!"));
            }

            await _logs.LogGameActionAsync(LogAction.ForceWar, discordId, 0, $"Staging:{allianceA}:{allianceB}");

            var embed = new EmbedBuilder()
            {
                Title = $"Forced war between **{allianceA}** and **{allianceB}**!",
                Color = Color.Red
            };

            var message = new ResponseMessage(embeds: new Embed[] { embed.Build() });
            return new ModResult(ModResultType.Success, message);
        }

        public async Task<ModResult> ForceEndWarStagingAsync(ulong discordId, string allianceA, string allianceB)
        {
            var success = await _gl.Staging.ForceStopWar(allianceA, allianceB);

            if (!success)
            {
                return new ModResult(ModResultType.BackendError, new ResponseMessage($"Failed to end war between **{allianceA}** and **{allianceB}**!"));
            }

            await _logs.LogGameActionAsync(LogAction.ForceStopWar, discordId, 0, $"Staging:{allianceA}:{allianceB}");

            var embed = new EmbedBuilder()
            {
                Title = $"Forced to stop war between **{allianceA}** and **{allianceB}**!",
                Color = Color.Red
            };

            var message = new ResponseMessage(embeds: new Embed[] { embed.Build() });
            return new ModResult(ModResultType.Success, message);
        }

        public async Task<ModResult<List<BattleLog>>> GetBattleLogTelemetry(ulong discordId, uint userId)
        {
            var result = await _gl.Api.GetBattlelogTelemetry(userId.ToString());
            await _logs.LogGameActionAsync(LogAction.GetTelemetry, discordId, userId, "BattleLogs");

            return new ModResult<List<BattleLog>>(result, ModResultType.Success);
        }

        public async Task<ModResult<List<Gift>>> GetGiftsTelemetry(ulong discordId, uint userId)
        {
            var result = await _gl.Api.GetGiftsTelemetry(userId.ToString());
            await _logs.LogGameActionAsync(LogAction.GetTelemetry, discordId, userId, "Gifts");

            return new ModResult<List<Gift>>(result, ModResultType.Success);
        }

        public async Task<ModResult<List<Login>>> GetLoginsTelemetry(ulong discordId, uint userId)
        {
            var result = await _gl.Api.GetLoginsTelemetry(userId.ToString());
            await _logs.LogGameActionAsync(LogAction.GetTelemetry, discordId, userId, "Gifts");

            return new ModResult<List<Login>>(result, ModResultType.Success);
        }

        public async Task<ModResult<Dictionary<string, Dictionary<string, int>>>> GetPossibleAlts(ulong discordId, uint userId)
        {
            var result = await _gl.Api.GetFingerprint(userId.ToString());
            await _logs.LogGameActionAsync(LogAction.GetAccounts, discordId, userId);

            return new ModResult<Dictionary<string, Dictionary<string, int>>>(result, ModResultType.Success);
        }

        private void OnBanTimer(object sender, ElapsedEventArgs e)
        {
            var bans = _storage.GetTempbans();
            var bansToRemove = new List<Tempban>();

            for (int i = 0; i < bans.Count; i++)
            {
                var time = (bans[i].BanEnd - DateTime.UtcNow).TotalMinutes;

                if (time <= 0)
                {
                    // no need to await
                    UnbanUserAsync(bans[i].ModeratorId, bans[i].UserId, true).ConfigureAwait(false);
                    bansToRemove.Add(bans[i]);
                }
            }

            // remove bans that were handled
            for (int i = 0; i < bansToRemove.Count; i++)
            {
                _storage.RemoveTempban(bansToRemove[i]);
            }
        }
    }
}
