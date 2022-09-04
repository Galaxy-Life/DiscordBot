using Discord.Commands;

namespace AdvancedBot.Core.Commands
{
    public class CustomCommandServiceConfig : CommandServiceConfig
    {
        public string DocumentationUrl { get; set; } = "https://github.com/svr333/AdvancedDiscordBot-Template/wiki";
        public string RepositoryUrl { get; set; } = "https://github.com/svr333/AdvancedBot-Template";
        public string Contributers { get; set; } = "<@202095042372829184>";
        public bool BotInviteIsPrivate { get; set; } = false;
    }
}
