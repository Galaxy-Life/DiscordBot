using System;
using AdvancedBot.Core.Commands;
using AdvancedBot.Core.Entities;
using Discord.Commands;

namespace AdvancedBot.Core.Services.Commands
{
    public class CommandPermissionService
    {
        private CustomCommandService _commands;

        public CommandPermissionService(CustomCommandService commands)
        {
            _commands = commands;
        }

        public void EnableGuildCommandOrModule(GuildAccount guild, string input)
        {
            var result = _commands.AdvancedSearch(input);

            if (result.Key is null)            
                guild.EnableCommand(_commands.FormatCommandName(result.Value));
            else
                EnableEntireModuleInGuild(guild, result.Key);
        }

        public void DisableGuildCommandOrModule(GuildAccount guild, string input)
        {
            var result = _commands.AdvancedSearch(input);

            if (result.Key is null)            
                guild.DisableCommand(_commands.FormatCommandName(result.Value));
            else
                DisableEntireModuleInGuild(guild, result.Key);
        }

        public void EnableWhitelistForCommandOrModule(GuildAccount guild, string input, bool isChannel)
        {
            var result = _commands.AdvancedSearch(input);

            if (result.Key is null)            
                guild.EnableWhitelist(_commands.FormatCommandName(result.Value), isChannel);
            else
                EnableWhitelistForModule(guild, result.Key, isChannel);
        }

        public void DisableWhitelistForCommandOrModule(GuildAccount guild, string input, bool isChannel)
        {
            var result = _commands.AdvancedSearch(input);

            if (result.Key is null)            
                guild.DisableWhitelist(_commands.FormatCommandName(result.Value), isChannel);
            else
                DisableWhitelistForModule(guild, result.Key, isChannel);
        }

        internal void ToggleDeleteMessageForCommandOrModule(GuildAccount guild, string input)
        {
            var result = _commands.AdvancedSearch(input);

            if (result.Key is null)
                guild.ToggleDeleteMsgOnCommand(_commands.FormatCommandName(result.Value));
            else 
                ToggleDeleteMessageForModule(guild, result.Key);
        }

        public void AddIdToWhitelistForCommandOrModule(GuildAccount guild, string input, ulong id, bool isChannel)
        {
            var result = _commands.AdvancedSearch(input);

            if (result.Key is null)            
                guild.AddToWhitelist(_commands.FormatCommandName(result.Value), id, isChannel);
            else
                AddIdToWhitelistForModule(guild, result.Key, id, isChannel);
        }

        public void RemoveIdFromWhitelistForCommandOrModule(GuildAccount guild, string input, ulong id, bool isChannel)
        {
            var result = _commands.AdvancedSearch(input);

            if (result.Key is null)            
                guild.RemoveFromWhitelist(_commands.FormatCommandName(result.Value), id, isChannel);
            else
                RemoveIdFromWhitelistForModule(guild, result.Key, id, isChannel);
        }

        private void EnableEntireModuleInGuild(GuildAccount guild, ModuleInfo module)
        {
            for (int i = 0; i < module.Commands.Count; i++)
            {
                var cmd = module.Commands[i];
                guild.EnableCommand(_commands.FormatCommandName(cmd));
            }

            for (int i = 0; i < module.Submodules.Count; i++)
            {
                EnableEntireModuleInGuild(guild, module.Submodules[i]);
            }
        }

        private void DisableEntireModuleInGuild(GuildAccount guild, ModuleInfo module)
        {
            for (int i = 0; i < module.Commands.Count; i++)
            {
                var cmd = module.Commands[i];
                guild.DisableCommand(_commands.FormatCommandName(cmd));
            }

            for (int i = 0; i < module.Submodules.Count; i++)
            {
                DisableEntireModuleInGuild(guild, module.Submodules[i]);
            }
        }

        private void ToggleDeleteMessageForModule(GuildAccount guild, ModuleInfo module)
        {
            for (int i = 0; i < module.Commands.Count; i++)
            {
                guild.ToggleDeleteMsgOnCommand(_commands.FormatCommandName(module.Commands[i]));
            }

            for (int i = 0; i < module.Submodules.Count; i++)
            {
                ToggleDeleteMessageForModule(guild, module.Submodules[i]);
            }
        }
    
        private void EnableWhitelistForModule(GuildAccount guild, ModuleInfo module, bool isChannel)
        {
            for (int i = 0; i < module.Commands.Count; i++)
            {
                var cmd = module.Commands[i];
                guild.EnableWhitelist(_commands.FormatCommandName(cmd), isChannel);
            }

            for (int i = 0; i < module.Submodules.Count; i++)
            {
                EnableWhitelistForModule(guild, module.Submodules[i], isChannel);
            }
        }

        private void DisableWhitelistForModule(GuildAccount guild, ModuleInfo module, bool isChannel)
        {
            for (int i = 0; i < module.Commands.Count; i++)
            {
                var cmd = module.Commands[i];
                guild.DisableWhitelist(_commands.FormatCommandName(cmd), isChannel);
            }

            for (int i = 0; i < module.Submodules.Count; i++)
            {
                DisableWhitelistForModule(guild, module.Submodules[i], isChannel);
            }
        }
    
        private void AddIdToWhitelistForModule(GuildAccount guild, ModuleInfo module, ulong id,bool isChannel)
        {
            for (int i = 0; i < module.Commands.Count; i++)
            {
                var cmd = module.Commands[i];
                guild.AddToWhitelist(_commands.FormatCommandName(cmd), id, isChannel);
            }

            for (int i = 0; i < module.Submodules.Count; i++)
            {
                AddIdToWhitelistForModule(guild, module.Submodules[i], id, isChannel);
            }
        }

        private void RemoveIdFromWhitelistForModule(GuildAccount guild, ModuleInfo module, ulong id, bool isChannel)
        {
            for (int i = 0; i < module.Commands.Count; i++)
            {
                var cmd = module.Commands[i];
                guild.RemoveFromWhitelist(_commands.FormatCommandName(cmd), id, isChannel);
            }

            for (int i = 0; i < module.Submodules.Count; i++)
            {
                RemoveIdFromWhitelistForModule(guild, module.Submodules[i], id, isChannel);
            }
        }
    }
}
