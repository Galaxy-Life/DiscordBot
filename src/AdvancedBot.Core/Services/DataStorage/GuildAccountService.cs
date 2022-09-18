using System.Collections.Generic;
using System.Linq;
using AdvancedBot.Core.Entities;
using AdvancedBot.Core.Commands;
using Discord.Interactions;

namespace AdvancedBot.Core.Services.DataStorage
{
    public class GuildAccountService
    {
        private LiteDBHandler _storage;
        private CustomInteractionService _commands;

        public GuildAccountService(LiteDBHandler storage, CustomInteractionService commands)
        {
            _storage = storage;
            _commands = commands;
        }

        internal GuildAccount GetOrCreateGuildAccount(ulong id)
        {
            if (!_storage.Exists<GuildAccount>(x => x.Id == id))
                CreateGuildAccount(id);
            return GetGuildAccount(id);
        }

        private void CreateGuildAccount(ulong id)
            => SaveGuildAccount(new GuildAccount() { Id = id, Commands = GenerateSettingsForAllCommands(_commands.SlashCommands)} );

        private GuildAccount GetGuildAccount(ulong id)
            => _storage.RestoreSingle<GuildAccount>(x => x.Id == id);

        internal void SaveGuildAccount(GuildAccount guild)
        {
            if (!_storage.Exists<GuildAccount>(x => x.Id == guild.Id))
                _storage.Store<GuildAccount>(guild);
            else _storage.Update<GuildAccount>(guild);
        }

        private List<CommandSettings> GenerateSettingsForAllCommands(IEnumerable<ICommandInfo> cmds)
        {
            var commands = cmds.ToArray();
            var allCommandSettings = new List<CommandSettings>();

            for (int i = 0; i < commands.Count(); i++)
            {
                var extendedCommandName = $"{commands[i].Module.Name}_{commands[i].Name}";

                var commandSettings = new CommandSettings()
                {
                    Name = extendedCommandName.ToLower(),
                    IsEnabled = true,
                    ChannelListIsBlacklist = true,
                    RolesListIsBlacklist = true,
                    WhitelistedChannels = new List<ulong>(),
                    WhitelistedRoles = new List<ulong>()
                };

                allCommandSettings.Add(commandSettings);
            }

            return allCommandSettings;
        }
    }
}
