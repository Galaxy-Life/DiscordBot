using System;

namespace AdvancedBot.Core.Entities
{
    public class CommandInfo
    {
        [Obsolete]
        public CommandInfo() {}

        public CommandInfo(string name)
        {
            Name = name;
        }

        public string Name { get; set; }
        public ulong TimesRun { get; set; }
        public ulong TimesFailed { get; set; }
    }
}
