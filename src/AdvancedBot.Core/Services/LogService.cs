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
        private AuthorizedGLClient _gl;

        public LogService(LiteDBHandler storage, CustomCommandService commands, DiscordSocketClient client, AuthorizedGLClient gl)
        {
            _storage = storage;
            _commands = commands;
            _client = client;
            _gl = gl;

            _id = _storage.RestoreCount<Log>();
        }

        public async Task LogGameActionAsync(LogAction action, ulong discordModId, uint victimGameId, string reason = "")
        {
            _id++;
            var log = new Log(_id, action, discordModId, victimGameId, reason);

            _storage.Store(log);

            var channel = await _client.GetChannelAsync(_commands.LogChannelId) as ISocketMessageChannel;
            var user = await _gl.GetUserById(victimGameId.ToString());

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
                case LogAction.Reset:
                case LogAction.KickOffline:
                case LogAction.AddBeta:
                case LogAction.RemoveBeta:
                case LogAction.GetChipsBought:
                case LogAction.GetFull:
                case LogAction.EnableMaintenance:
                default:
                    break;
                case LogAction.Ban:
                    embed.AddField("Ban Reason", log.Reason);
                    break;                
                case LogAction.GiveRole:
                    embed.AddField("Role", ((PhoenixRole)int.Parse(log.Reason)).Humanize());
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
                case LogAction.RemoveUserFromAlliance:
                case LogAction.MakeUserAllianceOwner:
                case LogAction.GetWarlogs:
                    embed.AddField("Alliance", log.Reason);
                    break;
                case LogAction.ReloadRules:
                    embed.AddField("Server", log.Reason);
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
