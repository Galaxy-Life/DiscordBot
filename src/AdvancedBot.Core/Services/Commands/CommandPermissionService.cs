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
    }
}
