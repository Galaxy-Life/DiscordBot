using System;

namespace AdvancedBot.Core.Entities
{
    public class Tempban
    {
        public Tempban() { }

        public Tempban(ulong modId, uint userId, DateTime until)
        {
            ModeratorId = modId;
            UserId = userId;
            BanEnd = until;
        }

        public ulong ModeratorId { get; set; }

        public uint UserId { get; set; }

        public DateTime BanStart { get; set; } = DateTime.UtcNow;

        public DateTime BanEnd { get; set; }
    }
}
