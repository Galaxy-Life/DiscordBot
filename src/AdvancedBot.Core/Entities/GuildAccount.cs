using System;
using System.Collections.Generic;

namespace AdvancedBot.Core.Entities
{
    public class Account
    {
        public ulong Id { get; set; }

        public bool IsGuild { get; set; }

        public List<CommandInfo> CommandInfos { get; set; } = new List<CommandInfo>();

        [Obsolete]
        public Account() {}

        public Account(ulong id, bool isGuild)
        {
            Id = id;
            IsGuild = isGuild;
        }
    }
}
