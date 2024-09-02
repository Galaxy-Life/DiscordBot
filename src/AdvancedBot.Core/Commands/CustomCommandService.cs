using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdvancedBot.Core.Services;
using Discord;
using Discord.Commands;
using Humanizer;

namespace AdvancedBot.Core.Commands
{
    public class CustomCommandService : CommandService
    {
        public PaginatorService Paginator { get; set; }
        public ulong LogChannelId;
        private readonly string _documentationUrl;
        private readonly string _sourceRepo;
        private readonly string _contributers;
        private readonly bool _botIsPrivate;

        public CustomCommandService() : base() { }

        public CustomCommandService(CustomCommandServiceConfig config) : base(config)
        {
            _documentationUrl = config.DocumentationUrl;
            _sourceRepo = config.RepositoryUrl;
            _contributers = config.Contributers;
            _botIsPrivate = config.BotInviteIsPrivate;
            LogChannelId = config.LogChannelId;
        }

        public async Task SendBotInfoAsync(IInteractionContext context)
        {
            var documentation = string.IsNullOrEmpty(_documentationUrl) ? $"N/A" : $"[Click me!]({_documentationUrl})";
            var sourceRepo = string.IsNullOrEmpty(_sourceRepo) ? $"N/A" : $"[Click me!]({_sourceRepo})";
            var botInvite = _botIsPrivate ? $"Private Bot." : $"[Click me!](https://discordapp.com/api/oauth2/authorize?client_id={context.Client.CurrentUser.Id}&permissions=8&scope=bot)";

            var embed = new EmbedBuilder()
            {
                Title = "About the bot",
                Description = $"**Documentation:** {documentation}\n\n**Source code:** {sourceRepo}\n\n" +
                              $"**Developed by:** {_contributers}\n\n**Invite the bot:** {botInvite}",
                ThumbnailUrl = context.Client.CurrentUser.GetAvatarUrl(),
            }.Build();

            await context.Interaction.ModifyOriginalResponseAsync(x => x.Embed = embed);
        }

        public static string FormatCommandName(CommandInfo command)
            => $"{command.Module.Name}_{command.Name}".ToLower();

        public CommandInfo GetCommandInfo(string commandName)
        {
            var searchResult = Search(commandName);
            if (!searchResult.IsSuccess) throw new Exception(searchResult.ErrorReason);

            return searchResult.Commands.OrderBy(x => x.Command.Priority).FirstOrDefault().Command;
        }

        public static string GenerateCommandUsage(CommandInfo command, string prefix)
        {
            StringBuilder parameters = new StringBuilder();

            for (int i = 0; i < command.Parameters.Count; i++)
            {
                var pref = command.Parameters[i].IsOptional ? "[" : "<";
                var suff = command.Parameters[i].IsOptional ? "]" : ">";
                
                parameters.Append($"{pref}{command.Parameters[i].Name.Underscore().Dasherize()}{suff} ");
            }
            
            return $"{prefix}{command.Aliases[0]} {parameters}";
        }
    }
}
