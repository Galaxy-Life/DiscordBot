using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdvancedBot.Core.Services.DataStorage;
using Discord;
using Discord.Commands;

namespace AdvancedBot.Core.Commands
{
    public class CustomCommandService : CommandService
    {
        private GuildAccountService _accounts;

        private readonly string _documentationUrl = "";
        private readonly string _thumbnailUrl = "";
        private readonly string _sourceRepo = "https://github.com/svr333/AdvancedBot-Template";
        private readonly string _contributers = "<@202095042372829184>";

        public CustomCommandService() : base() { }

        public CustomCommandService(CustomCommandServiceConfig config) : base(config) { }

        public async Task SendHelpCommand(ICommandContext context)
        {
            var documentation = string.IsNullOrEmpty(_documentationUrl) ? $"N/A" : $"[Click me!]({_documentationUrl})";
            var sourceRepo = string.IsNullOrEmpty(_sourceRepo) ? $"N/A" : $"[Click me!]({_sourceRepo})";

            var embed = new EmbedBuilder()
            {
                Title = "About the bot",
                ThumbnailUrl = _thumbnailUrl,
            }
            .AddField("Documentation   ⠀", $"{documentation}   ⠀\n\u200b", true)
            .AddField("⠀   Source code    ", $"⠀ ⠀ {sourceRepo}   ⠀\n\u200b", true)
            .AddField("⠀   Made possible by:", $"⠀   {_contributers}\n\u200b", true)
            .WithFooter(context.User.Username, context.User.GetAvatarUrl())
            .Build();

            await context.Channel.SendMessageAsync("", false, embed);
        }

        public string AllCommandsToString()
            => string.Join(", ", Commands);

        public CommandInfo GetCommandInfo(string commandName)
        {
            var searchResult = Search(commandName);
            if (!searchResult.IsSuccess) throw new Exception(searchResult.ErrorReason);

            return searchResult.Commands.OrderBy(x => x.Command.Priority).FirstOrDefault().Command;
        }

        public EmbedBuilder GetCommandHelpEmbed(CommandInfo command)
            => new EmbedBuilder()
            {
                Title = $"**{command.Name}** | {string.Join(", ", command.Aliases)}\n",
                Description = $"{command.Summary}\n\u200b"
            };

        public EmbedFieldBuilder GenerateUsageField(CommandInfo command)
        {
            string commandExample = "";
            StringBuilder parameters = new StringBuilder();

            for (int i = 0; i < command.Parameters.Count; i++)
            {
                var pref = "<";
                var suff = ">";
                
                if (command.Parameters[i].IsOptional)
                {
                    pref = "["; suff = "]";
                }

                parameters.Append($"{pref}{command.Parameters[i]}{suff} ");
            }
            
            commandExample = $"\n!**{command.Aliases[0]} {parameters}**";

            return new EmbedFieldBuilder()
            {
                Name = $"Correct usage:",
                Value = commandExample,
                IsInline = false,
            };
        } 
    }
}
