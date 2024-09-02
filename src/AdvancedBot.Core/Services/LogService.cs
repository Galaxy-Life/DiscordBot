using System;
using System.Threading.Tasks;
using AdvancedBot.Core.Commands;
using AdvancedBot.Core.Entities;
using AdvancedBot.Core.Entities.Enums;
using AdvancedBot.Core.Services.DataStorage;
using Discord;
using Discord.WebSocket;
using GL.NET;
using GL.NET.Entities;
using Humanizer;

namespace AdvancedBot.Core.Services
{
    public class LogService
    {
        private int _id = 0;
        private readonly LiteDBHandler _storage;
        private readonly CustomCommandService _commands;
        private readonly DiscordSocketClient _client;
        private readonly GLClient _gl;

        public LogService(LiteDBHandler storage, CustomCommandService commands, DiscordSocketClient client, GLClient gl)
        {
            _storage = storage;
            _commands = commands;
            _client = client;
            _gl = gl;

            _id = _storage.RestoreCount<Log>();
        }

        public async Task LogGameActionAsync(LogAction action, ulong discordModId, uint victimGameId, string reason = "", DateTime? until = null)
        {
            _id++;
            var log = new Log(_id, action, discordModId, victimGameId, reason, until);

            _storage.Store(log);

            var channel = await _client.GetChannelAsync(_commands.LogChannelId) as ISocketMessageChannel;
            var user = await _gl.Api.GetUserById(victimGameId.ToString());

            await channel.SendMessageAsync(embed: GetEmbedForLog(log, user));
        }

        public static Embed GetEmbedForLog(Log log, User target)
        {
            var embed = new EmbedBuilder()
                .WithTitle($"Overview | {log.Type.Humanize()}")
                .WithColor(GetColorBasedOnAction(log.Type))
                .AddField("Moderator", $"<@{log.DiscordModId}>", true)
                .WithFooter(footer => footer
                    .WithText($"Case #{log.Id} • Requested by moderator with id {log.DiscordModId}"))
                .WithCurrentTimestamp();

            if (target != null)
            {
                embed.AddField("Target", $"{target.Name} (`{target.Id}`)", true);
            }

            switch (log.Type)
            {
                case LogAction.Unban:
                    if (!string.IsNullOrEmpty(log.Reason))
                    {
                        embed.AddField("Reason", log.Reason, true);
                    }
                    break;

                case LogAction.Ban:
                    embed.AddField("Reason", log.Reason);
                    embed.AddField("Expires on", log.Until == null ? "Never" : log.Until.Value.ToShortDateString());
                    break;

                case LogAction.GiveRole:
                    embed.AddField("Role", log.Reason, true);
                    break;

                case LogAction.UpdateEmail:
                case LogAction.UpdateName:
                case LogAction.RenameAlliance:
                    var splits = log.Reason.Split(':');
                    embed.AddField("Previous", splits[0]);
                    embed.AddField("Updated", splits[1], true);
                    break;

                case LogAction.AddChips:
                    embed.AddField("Chips added", log.Reason);
                    break;

                case LogAction.AddItem:
                    var itemSplit = log.Reason.Split(':');
                    embed.AddField("Item SKU", itemSplit[0]);
                    embed.AddField("Quantity", itemSplit[1]);
                    break;

                case LogAction.AddXp:
                    embed.AddField("Experience added", log.Reason);
                    break;

                case LogAction.RemoveUserFromAlliance:
                case LogAction.MakeUserAllianceOwner:
                case LogAction.GetWarlogs:
                    embed.AddField("Alliance", log.Reason, true);
                    break;

                case LogAction.ReloadRules:
                case LogAction.GetTelemetry:
                    embed.AddField("Server", log.Reason, true);
                    break;

                case LogAction.RunKicker:
                case LogAction.ResetHelps:
                case LogAction.Reset:
                    embed.AddField("Server", log.Reason, true);
                    break;

                case LogAction.ForceWar:
                case LogAction.ForceStopWar:
                    var warSplit = log.Reason.Split(':');
                    embed.AddField("Server", warSplit[0], true);
                    embed.AddField("Alliance A", warSplit[1]);
                    embed.AddField("Alliance B", warSplit[2]);
                    break;

                case LogAction.Compensate:
                    var compensateSplit = log.Reason.Split(':');
                    embed.AddField("Type", compensateSplit[0]);

                    if (compensateSplit.Length > 2)
                    {
                        embed.AddField("Item SKU", compensateSplit[1]);
                        embed.AddField("Quantity", compensateSplit[2]);
                    }
                    else
                    {
                        embed.AddField("Quantity", compensateSplit[1]);
                    }
                    break;

                case LogAction.KickOffline:
                case LogAction.AddBeta:
                case LogAction.RemoveBeta:
                case LogAction.GetChipsBought:
                case LogAction.GetChipsSpent:
                case LogAction.GetFull:
                case LogAction.EnableMaintenance:
                case LogAction.AddEmulate:
                case LogAction.RemoveEmulate:
                case LogAction.GetAccounts:
                default:
                    break;
            }

            return embed.Build();
        }

        private static Color GetColorBasedOnAction(LogAction action)
        {
            return action switch
            {
                LogAction.Ban or LogAction.Reset or LogAction.KickOffline or LogAction.RemoveBeta or 
                LogAction.RemoveUserFromAlliance or LogAction.EnableMaintenance or LogAction.ReloadRules
                    => Color.Red,

                LogAction.Unban or LogAction.AddBeta or LogAction.GiveRole
                    => Color.Green,

                LogAction.UpdateEmail or LogAction.UpdateName or LogAction.GetFull or LogAction.GetChipsBought or
                LogAction.MakeUserAllianceOwner or LogAction.GetWarlogs
                    => new Color(15710778),

                LogAction.AddChips or LogAction.AddItem or LogAction.RenameAlliance
                    => Color.Blue,

                _ => Color.Blue
            };
        }
    }
}
