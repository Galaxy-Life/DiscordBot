using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdvancedBot.Core.Entities;
using AdvancedBot.Core.Services;
using AdvancedBot.Core.Services.DataStorage;
using Discord;
using Discord.Commands;
using Humanizer;

namespace AdvancedBot.Core.Commands
{
    public class CustomCommandService : CommandService
    {
        public PaginatorService Paginator { get; set; }
        private readonly string _documentationUrl = "";
        private readonly string _thumbnailUrl = "";
        private readonly string _sourceRepo = "https://github.com/svr333/AdvancedBot-Template";
        private readonly string _contributers = "<@202095042372829184>";
        private readonly string _botInvite = "";

        public CustomCommandService() : base() { }

        public CustomCommandService(CustomCommandServiceConfig config) : base(config) { }

        public async Task SendBotInfoAsync(ICommandContext context)
        {
            var documentation = string.IsNullOrEmpty(_documentationUrl) ? $"N/A" : $"[Click me!]({_documentationUrl})";
            var sourceRepo = string.IsNullOrEmpty(_sourceRepo) ? $"N/A" : $"[Click me!]({_sourceRepo})";
            var botInvite = string.IsNullOrEmpty(_botInvite) ? $"N/A" : $"[Click me!]({_botInvite})";

            var embed = new EmbedBuilder()
            {
                Title = "About the bot",
                Description = $"**Documentation:** {documentation}\n\n**Source code:** {sourceRepo}\n\n**Made possible by:** {_contributers}\n\n**Invite the bot:** {_botInvite}",
                ThumbnailUrl = _thumbnailUrl,
            }
            .WithFooter(context.User.Username, context.User.GetAvatarUrl())
            .Build();

            await context.Channel.SendMessageAsync("", false, embed);
        }

        public EmbedBuilder CreateModuleInfoEmbed(ModuleInfo module)
        {
            var submodulesField = "";
            var commandsField = "";

            var topModule = module.IsSubmodule && module.Parent.Group != "TopModule"
                            ? $"This category is a subcategory of **{module.Parent.Group.Transform(To.SentenceCase)}**.\n"
                            : string.Empty;

            var embed = new EmbedBuilder()
            {
                Title = $"Info for category: {module.Group.Transform(To.SentenceCase)}",
                Description = $"{topModule}{module.Summary}\n\n",
                Color = Color.Purple
            }
            .WithFooter($"{"command".ToQuantity(module.Commands.Count)} | {"category".ToQuantity(module.Submodules.Count)}");

            for (int i = 0; i < module.Submodules.Count; i++)
            {
                var currentModule = module.Submodules[i];

                submodulesField += 
                $"**{currentModule.Name.Transform(To.SentenceCase)}** | {"command".ToQuantity(currentModule.Commands.Count)} | " +
                $"{"subcategory".ToQuantity(currentModule.Submodules.Count)}\n" +
                $"{currentModule.Summary}\n\n";
            }

            for (int i = 0; i < module.Commands.Count; i++)
            {
                var currentCommand = module.Commands[i];

                commandsField += $"{GenerateCommandUsage(currentCommand)}\n{currentCommand.Summary}\n";
            }

            if (!string.IsNullOrEmpty(submodulesField)) embed.AddField($"Subcategories:", $"{submodulesField}");
            if (!string.IsNullOrEmpty(commandsField)) embed.AddField($"Commands:", commandsField);

            return embed;
        }

        public EmbedBuilder CreateCommandInfoEmbed(CommandInfo command)
        {
            return new EmbedBuilder()
            {
                Title = GenerateCommandUsage(command),
                Description = command.Summary,
                Color = Color.Purple
            };
        }

        public KeyValuePair<ModuleInfo, CommandInfo> AdvancedSearch(string input)
        {
            var result = new Dictionary<ModuleInfo, CommandInfo>();

            var allCommandAliases = ListAllCommandAliases();
            var possibleCommand = allCommandAliases.FirstOrDefault(x => x == input);

            var allModuleAlliases = ListAllModuleAliases();
            var possibleModule = allModuleAlliases.FirstOrDefault(x => x == input);

            if (!string.IsNullOrEmpty(possibleModule) || possibleModule == possibleCommand && !string.IsNullOrEmpty(possibleModule))
            {
                var module = Modules.FirstOrDefault(x => x.Aliases.Contains(possibleModule));
                result.Add(module, null);
            }

            else if (!string.IsNullOrEmpty(possibleCommand))
            {
                var cmd = Commands.FirstOrDefault(x => x.Aliases.Contains(possibleCommand));
                result.Add(cmd.Module, cmd);
            }     

            else throw new Exception("Could not find a module or command for the given input.");

            return result.FirstOrDefault();
        }

        public string AllCommandsToString()
            => string.Join(", ", Commands);

        public List<string> ListAllCommandAliases()
        {
            var aliases = new List<string>();
            var commands = Commands.ToList();

            for (int i = 0; i < commands.Count; i++)
            {
                aliases.AddRange(commands[i].Aliases);
            }

            return aliases;
        }

        public List<string> ListAllModuleAliases()
        {
            var aliases = new List<string>();
            var modules = Modules.ToList();

            for (int i = 0; i < modules.Count; i++)
            {
                aliases.AddRange(modules[i].Aliases);
            }

            return aliases;
        }

        public CommandInfo GetCommandInfo(string commandName)
        {
            var searchResult = Search(commandName);
            if (!searchResult.IsSuccess) throw new Exception(searchResult.ErrorReason);

            return searchResult.Commands.OrderBy(x => x.Command.Priority).FirstOrDefault().Command;
        }

        public EmbedBuilder GetCommandHelpEmbed(CommandInfo command)
            => new EmbedBuilder()
            .WithTitle($"**{command.Name}** | {string.Join(", ", command.Aliases)}\n")
            .WithDescription($"{command.Summary}\n\u200b");

        public string GenerateCommandUsage(CommandInfo command)
        {
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
            
            return $"\n!**{command.Aliases[0]} {parameters}**";
        }
    }
}
