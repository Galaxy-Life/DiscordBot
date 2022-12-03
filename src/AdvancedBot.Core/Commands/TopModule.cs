﻿using System.Linq;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using AdvancedBot.Core.Entities;
using AdvancedBot.Core.Services.DataStorage;
using AdvancedBot.Core.Services;
using System.Threading.Tasks;
using System.Collections.Generic;
using Discord.Interactions;

namespace AdvancedBot.Core.Commands
{
    public class TopModule : InteractionModuleBase<SocketInteractionContext>
    {
        public GuildAccountService Accounts { get; set; }
        public CustomCommandService Commands { get; set; }
        public PaginatorService Paginator { get; set; }
        private CommandInfo _currentCommand;
        [DontInject]
        public string ExpandedCommandName => FormatCommandName(_currentCommand);

        public string FormatCommandName(CommandInfo command)
            => $"{command.Module.Name}_{command.Name}".ToLower();

        private bool CommandIsAllowedToRun(GuildAccount guild)
        {
            var currentCommand = guild.Commands.Find(x => x.Name == ExpandedCommandName);
            // If command doesn't exist in database, recreate it.
            if (currentCommand == null) guild.AddNewCommand(_currentCommand);
            Accounts.SaveGuildAccount(guild);

            currentCommand = guild.Commands.Find(x => x.Name == ExpandedCommandName);

            var userRoles = (Context.User as SocketGuildUser).Roles.ToList();
            
            if (!currentCommand.IsEnabled) return false;
            if (userRoles.Find(x => x.Id == guild.ModRoleId) != null) return true;

            var userHasRoleInList = UserHasRoleInList(currentCommand);
            if (currentCommand.RolesListIsBlacklist && userHasRoleInList
            || !currentCommand.RolesListIsBlacklist && !userHasRoleInList) 
                return false;
            
            var channelIsInList = currentCommand.WhitelistedChannels.Contains(Context.Channel.Id);
            if (currentCommand.ChannelListIsBlacklist && channelIsInList
            || !currentCommand.ChannelListIsBlacklist && !channelIsInList) 
                return false;

            return true;
        }

        private bool UserHasRoleInList(CommandSettings command)
        {
            var user = (Context.User as SocketGuildUser);
            var rolesInCommon = user.Roles.Select(x => x.Id).Intersect(command.WhitelistedRoles);

            if (!rolesInCommon.Any() || rolesInCommon == null) return false;
            return true;
        }
    
        public async Task<IUserMessage> SendPaginatedMessageAsync(IEnumerable<EmbedField> displayFields, IEnumerable<string> displayTexts, EmbedBuilder templateEmbed)
        {
            var displayItems = 0;
            
            if (displayTexts != null)
            {
                templateEmbed.WithDescription(string.Join("\n", displayTexts.Take(10)));
                displayItems = displayTexts.Count();
            }
            else if (displayFields != null)
            {
                displayItems = displayFields.Count();
                var fields = displayFields.Take(10).ToArray();

                for (int i = 0; i < fields.Length; i++)
                {
                    templateEmbed.AddField(fields[i].Name, fields[i].Value, fields[i].Inline);
                }
            }

            templateEmbed.WithTitle($"{templateEmbed.Title} | Page 1");
            templateEmbed.WithFooter($"{templateEmbed.Footer?.Text}\n{Context.User.Username} ({Context.User.Id}) | Total Display Items: {displayItems}");

            var message = await Paginator.HandleNewPaginatedMessageAsync(Context, displayFields, displayTexts, templateEmbed.Build());
            await Task.Delay(1000);    

            return message;
        }
    }
}
