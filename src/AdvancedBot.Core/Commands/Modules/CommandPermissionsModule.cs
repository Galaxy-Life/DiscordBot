using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using AdvancedBot.Core.Commands.Preconditions;
using AdvancedBot.Core.Services.DataStorage;
using Discord;

namespace AdvancedBot.Core.Commands.Modules
{
    [RequireCustomPermission(GuildPermission.ManageChannels)]
    public class CommandPermissionsModule : CustomModule
    {
        private GuildAccountService _accounts;
        private CommandService _commands;

        public CommandPermissionsModule(GuildAccountService accounts, CommandService commands)
        {
            _accounts = accounts;
            _commands = commands;
        }
        
        [Command("command")][Alias("cmd")]
        [Summary("Enables or disables a command.")]
        public async Task SetStateOfCommand(string action, string commandName)
        {
            var result = _commands.Search(commandName);
            if (result.Error != null) throw new Exception("Command not found.");

            var guild = _accounts.GetOrCreateGuildAccount(Context.Guild.Id);
            var command = result.Commands.First().Command;
            var formattedName = FormatName(command);

            if (action == "enable") guild.EnableCommand(formattedName);
            else if (action == "disable") guild.DisableCommand(formattedName);
            else throw new Exception("Not a valid operation.");
           
            _accounts.SaveGuildAccount(guild);
            await ReplyAsync($"Successfully {action}d command {command.Name} in {command.Module.Name}");
        }

        [Command("commandsetrolelist")]
        public async Task SetWhitelistOrBlacklistForRoles(string commandName, string action)
        {
            var result = _commands.Search(commandName);
            if (result.Error != null) throw new Exception("Command not found.");

            var guild = _accounts.GetOrCreateGuildAccount(Context.Guild.Id);
            var command = result.Commands.First().Command;
            var formattedName = FormatName(command);

            if (action == "enable") guild.EnableWhitelist(formattedName, false);
            else if (action == "disable") guild.DisableWhitelist(formattedName, false);

            _accounts.SaveGuildAccount(guild);
            await ReplyAsync($"Successfully {action}d whitelist instead of blacklist for roles.");
        }

        [Command("commandsetchannellist")]
        public async Task SetWhitelistOrBlacklistForChannels(string commandName, string action)
        {
            var result = _commands.Search(commandName);
            if (result.Error != null) throw new Exception("Command not found.");

            var guild = _accounts.GetOrCreateGuildAccount(Context.Guild.Id);
            var command = result.Commands.First().Command;
            var formattedName = FormatName(command);

            if (action == "enable") guild.EnableWhitelist(formattedName, true);
            else if (action == "disable") guild.DisableWhitelist(formattedName, true);

            _accounts.SaveGuildAccount(guild);
            await ReplyAsync($"Successfully {action}d whitelist instead of blacklist for roles.");
        }

        [Command("commandwhitelist")]
        public async Task SetUserOrChannelInList(string action, string commandName, ulong id)
        {
            var result = _commands.Search(commandName);
            if (result.Error != null) throw new Exception("Command not found.");

            var guild = _accounts.GetOrCreateGuildAccount(Context.Guild.Id);
            var command = result.Commands.First().Command;
            var formattedName = FormatName(command);

            if (action == "add") guild.AddToWhitelist(formattedName, id, IdIsChannel(id));
            else if (action == "remove") guild.RemoveFromWhitelist(formattedName, id, IdIsChannel(id));
            else throw new Exception("Invalid operation.");

            _accounts.SaveGuildAccount(guild);
            await ReplyAsync($"Succesfully {action}ed id `{id}` to the list.");
        }

        [Command("modrole")]
        public async Task SetModRole(SocketRole role)
        {
            var guild = _accounts.GetOrCreateGuildAccount(Context.Guild.Id);
            guild.SetModRole(role.Id);
            _accounts.SaveGuildAccount(guild);

            await ReplyAsync($"Modrole has successfully been changed to `{role.Name}`\n" +
                            $"{role.Members.Count()} can now access all enabled commands anywhere.");
        }

        private string FormatName(CommandInfo command)
            => $"{command.Module.Name}_{command.Name}".ToLower();
    
        private bool IdIsChannel(ulong id)
        {
            var allegedChannel = Context.Client.GetChannel(id);
            var allegedRole = Context.Guild.GetRole(id);

            if (allegedChannel == null && allegedRole == null) throw new Exception("Specified Id is not a valid role or channel.");
            return (allegedRole == null);
        }
    }
}
