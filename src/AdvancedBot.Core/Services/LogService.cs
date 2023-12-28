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
        private LiteDBHandler _storage;
        private CustomCommandService _commands;
        private DiscordSocketClient _client;
        private GLClient _gl;

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

        public Embed GetEmbedForLog(Log log, User victim)
        {
            var embed = new EmbedBuilder()
            {
                Title = $"Case {log.Id} ({log.Type.Humanize()})",
                Color = GetColorBasedOnAction(log.Type)
            }
            .AddField("Moderator", $"<@{log.DiscordModId}>", true);

            // if alliance action this can be null
            if (victim != null)
            {
                embed.AddField("Victim", $"{victim.Name} ({victim.Id})", true);
            }

            switch (log.Type)
            {
                case LogAction.Unban:
                    if (!string.IsNullOrEmpty(log.Reason))
                    {
                        embed.AddField("Reason", log.Reason, true);
                    }

                    break;
                case LogAction.KickOffline:
                case LogAction.AddBeta:
                case LogAction.RemoveBeta:
                case LogAction.GetChipsBought:
                case LogAction.GetFull:
                case LogAction.EnableMaintenance:
                case LogAction.AddEmulate:
                case LogAction.RemoveEmulate:
                case LogAction.GetAccounts:
                default:
                    break;
                case LogAction.Ban:
                    embed.AddField("Ban Reason", log.Reason, true);
                    embed.AddField("Ends", log.Until == null ? "Never" : log.Until.Value.ToShortDateString());
                    break;                
                case LogAction.GiveRole:
                    embed.AddField("Role", log.Reason, true);
                    break;
                case LogAction.UpdateEmail:
                case LogAction.UpdateName:
                case LogAction.RenameAlliance:
                    var splits = log.Reason.Split(':');
                    embed.AddField("From", splits[0]);
                    embed.AddField("To", splits[1], true);
                    break;
                case LogAction.AddChips:
                    embed.AddField("Chips added", log.Reason);
                    break;
                case LogAction.AddItem:
                    var splitties = log.Reason.Split(':');
                    embed.AddField("Item added", $"{splitties[1]}x item {splitties[0]}");
                    break;
                case LogAction.AddXp:
                    embed.AddField("Xp added", log.Reason);
                    break;
                case LogAction.RemoveUserFromAlliance:
                case LogAction.MakeUserAllianceOwner:
                case LogAction.GetWarlogs:
                    embed.AddField("Alliance", log.Reason, true);
                    break;
                case LogAction.ReloadRules:
                    embed.AddField("Server", log.Reason, true);
                    break;
                case LogAction.GetTelemetry:
                    embed.AddField("Type", log.Reason, true);
                    break;
                case LogAction.RunKicker:
                case LogAction.ResetHelps:
                case LogAction.Reset:
                    embed.AddField("Server", log.Reason, true);
                    break;
                case LogAction.ForceWar:
                case LogAction.ForceStopWar:
                    var split = log.Reason.Split(':');
                    embed.AddField("Server", split[0], true);
                    embed.AddField("Alliance A", split[1]);
                    embed.AddField("Alliance B", split[2]);
                    break;
            }

            return embed.Build();
        }

        private Color GetColorBasedOnAction(LogAction action)
        {
            switch (action)
            {
                case LogAction.Ban:
                case LogAction.Reset:
                case LogAction.KickOffline:
                case LogAction.RemoveBeta:
                case LogAction.RemoveUserFromAlliance:
                case LogAction.EnableMaintenance:
                case LogAction.ReloadRules:
                    return Color.Red;
                case LogAction.Unban:
                case LogAction.AddBeta:
                case LogAction.GiveRole:
                    return Color.Green;
                case LogAction.UpdateEmail:
                case LogAction.UpdateName:
                case LogAction.GetFull:
                case LogAction.GetChipsBought:
                case LogAction.MakeUserAllianceOwner:
                case LogAction.GetWarlogs:
                    return new Color(15710778);
                case LogAction.AddChips:
                case LogAction.AddItem:
                case LogAction.RenameAlliance:
                default:
                    return Color.Blue;
            }
        }
    }
}
