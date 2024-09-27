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
        private readonly string documentationUrl;
        private readonly string sourceRepo;
        private readonly string contributors;
        private readonly bool botIsPrivate;

        public CustomCommandService() : base() { }

        public CustomCommandService(CustomCommandServiceConfig config) : base(config)
        {
            documentationUrl = config.DocumentationUrl;
            sourceRepo = config.RepositoryUrl;
            contributors = config.Contributors;
            botIsPrivate = config.BotInviteIsPrivate;
            LogChannelId = config.LogChannelId;
        }

        public async Task SendBotInfoAsync(IInteractionContext context)
        {
            string botName = context.Client.CurrentUser.Username;
            string botAvatar = context.Client.CurrentUser.GetDisplayAvatarUrl();

            string repoLink = string.IsNullOrEmpty(sourceRepo) ? $"Unavailable" : $"[GitHub repository]({sourceRepo})";
            string docsLink = string.IsNullOrEmpty(documentationUrl) ? $"Unavailable" : $"[Read the docs]({documentationUrl})";
            string inviteLink = $"[Invite link](https://discordapp.com/api/oauth2/authorize?client_id={context.Client.CurrentUser.Id}&permissions=8&scope=bot)";

            var embed = new EmbedBuilder()
                .WithTitle($"About {botName}")
                .WithColor(Color.DarkBlue)
                .WithDescription("This is the official Discord bot for Galaxy Life.\nFind information about players, alliances or server status!")
                .WithThumbnailUrl(botAvatar)
                .AddField("Documentation", docsLink, true)
                .AddField("Source Code", repoLink, true)
                .WithFooter(footer => footer
                    .WithText($"Bot information requested by {context.User.Username}#{context.User.Discriminator}")
                    .WithIconUrl(context.User.GetDisplayAvatarUrl()))
                .WithCurrentTimestamp();

            if (!botIsPrivate)
            {
                embed.AddField("Invitation", inviteLink, true);
            }

            embed.AddField("Developed by", contributors);

            await context.Interaction.ModifyOriginalResponseAsync(msg => msg.Embeds = new Embed[] { embed.Build() });
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
            StringBuilder parameters = new();

            for (int i = 0; i < command.Parameters.Count; i++)
            {
                string pref = command.Parameters[i].IsOptional ? "[" : "<";
                string suff = command.Parameters[i].IsOptional ? "]" : ">";

                parameters.Append($"{pref}{command.Parameters[i].Name.Underscore().Dasherize()}{suff} ");
            }

            return $"{prefix}{command.Aliases[0]} {parameters}";
        }
    }
}
