using AdvancedBot.Core.Entities.Enums;
using Humanizer;

namespace AdvancedBot.Core.Entities
{
    public class ChannelCounterInfo
    {
        public ChannelCounterInfo(ChannelCounterType type)
        {
            Type = type;
            Trigger = type.Humanize().ToLower();
            CheckIntervalInMinutes = 6;
            Description = "No description provided.";

            switch (type)
            {
                case ChannelCounterType.FlashStatus:
                    Trigger = "flash";
                    Description = "Shows the current server status of flash servers.";
                    CheckIntervalInMinutes = 3;
                    break;
                default:
                    break;
            }
        }

        public ChannelCounterType Type { get; }
        public string Trigger { get; }
        public string Description { get; }
        public int CheckIntervalInMinutes { get; }
    }
}
