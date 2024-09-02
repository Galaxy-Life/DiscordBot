using Discord.Commands;

namespace AdvancedBot.Core.Commands
{
    public class CustomCommandServiceConfig : CommandServiceConfig
    {
        public string DocumentationUrl { get; set; } = "https://github.com/svr333/AdvancedDiscordBot-Template/wiki";
        public string RepositoryUrl { get; set; } = "https://github.com/svr333/AdvancedBot-Template";
        public string Contributors { get; set; } = "<@202095042372829184>, <@424689465450037278>";
        public bool BotInviteIsPrivate { get; set; } = false;
        public ulong LogChannelId { get; set; }
    }
}
