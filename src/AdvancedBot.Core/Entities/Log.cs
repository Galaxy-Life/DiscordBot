using System;
using AdvancedBot.Core.Entities.Enums;

namespace AdvancedBot.Core.Entities
{
    public class Log
    {
        public Log() {}
        public Log(int id, LogAction type, ulong discordModId, uint victimGameId, string reason)
        {
            Id = id;
            Type = type;
            DiscordModId = discordModId;
            VictimGameId = victimGameId;
            Reason = reason;
            At = DateTime.UtcNow;
        }

        public int Id { get; set; }
        public LogAction Type { get; set; }
        public ulong DiscordModId { get; set; }
        public uint VictimGameId { get; set; }
        public string Reason { get; set; }
        public DateTime At { get; set; }
    }
}
