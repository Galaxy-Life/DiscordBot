using AdvancedBot.Core.Entities.Enums;

namespace AdvancedBot.Core.Entities;

public class ChannelCounter
{
    public ChannelCounter() { }

    public ChannelCounter(ulong channelId, ChannelCounterType type)
    {
        ChannelId = channelId;
        Type = type;
    }

    public ulong ChannelId { get; set; }
    public ChannelCounterType Type { get; set; }
}
