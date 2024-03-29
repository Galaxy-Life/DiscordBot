using System;
using System.Collections.Generic;

namespace AdvancedBot.Core.Entities
{
    public class Account
    {
        public ulong Id { get; set; }

        public bool IsGuild { get; set; }

        public List<CommandStats> CommandStats { get; set; } = new List<CommandStats>();
        public List<ChannelCounter> ChannelCounters { get; set; } = new List<ChannelCounter>();

        [Obsolete]
        public Account() {}

        public Account(ulong id, bool isGuild)
        {
            Id = id;
            IsGuild = isGuild;
        }
    }
}
