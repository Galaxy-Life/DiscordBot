using System;
using AdvancedBot.Core.Entities.Enums;

namespace AdvancedBot.Core.Entities
{
    public class Log
    {
        public Log() { }
        public Log(int id, LogAction type, ulong discordModId, uint victimGameId, string reason, DateTime? until = null)
        {
            Id = id;
            Type = type;
            DiscordModId = discordModId;
            VictimGameId = victimGameId;
            Reason = reason;
            At = DateTime.UtcNow;
            Until = until;
        }

        public int Id { get; set; }
        public LogAction Type { get; set; }
        public ulong DiscordModId { get; set; }
        public uint VictimGameId { get; set; }
        public string Reason { get; set; }
        public DateTime At { get; set; }
        public DateTime? Until { get; set; }
    }
}
