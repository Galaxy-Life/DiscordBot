using System.Threading.Tasks;
using AdvancedBot.Core.Services;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace AdvancedBot.Core.Commands
{
    public class CustomInteractionService : InteractionService
    {
        public PaginatorService Paginator { get; set; }
        private readonly string _documentationUrl;
        private readonly string _sourceRepo;
        private readonly string _contributers;
        private readonly bool _botIsPrivate;

        public CustomInteractionService(DiscordSocketClient client, CustomInteractionServiceConfig config) : base(client, config)
        {
            _documentationUrl = config.DocumentationUrl;
            _sourceRepo = config.RepositoryUrl;
            _contributers = config.Contributers;
            _botIsPrivate = config.BotInviteIsPrivate;
        }

        public async Task<IUserMessage> SendBotInfoAsync(SocketInteractionContext context)
        {
            var documentation = string.IsNullOrEmpty(_documentationUrl) ? $"N/A" : $"[Click me!]({_documentationUrl})";
            var sourceRepo = string.IsNullOrEmpty(_sourceRepo) ? $"N/A" : $"[Click me!]({_sourceRepo})";
            var botInvite = _botIsPrivate ? $"Bot is private" : $"[Click me!](https://discordapp.com/api/oauth2/authorize?client_id={context.Client.CurrentUser.Id}&permissions=8&scope=bot)";

            var embed = new EmbedBuilder()
            {
                Title = "About the bot",
                Description = $"For a bare list of all commands, execute `!commands`\nFor a bare list of categories, execute `!modules`\n\n" +
                              $"**Documentation:** {documentation}\n\n**Source code:** {sourceRepo}\n\n" +
                              $"**Made possible by:** {_contributers}\n\n**Invite the bot:** {botInvite}",
                ThumbnailUrl = context.Client.CurrentUser.GetAvatarUrl(),
            }
            .WithFooter(context.User.Username, context.User.GetAvatarUrl())
            .Build();

            var message = await context.Channel.SendMessageAsync("", false, embed);
            return message;
        }
    }
}
