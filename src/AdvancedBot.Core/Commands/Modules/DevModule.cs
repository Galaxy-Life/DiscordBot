using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AdvancedBot.Core.Commands.Preconditions;
using AdvancedBot.Core.Entities;
using AdvancedBot.Core.Entities.Enums;
using AdvancedBot.Core.Services.DataStorage;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace AdvancedBot.Core.Commands.Modules
{
    [DontAutoRegister]
    [RequirePrivateList]
    public class DevModule : TopModule
    {
        private InteractionService _interactions;
        private LiteDBHandler _storage;
        private CustomCommandService _commands;

        public DevModule(InteractionService interactions, LiteDBHandler storage, CustomCommandService commands)
        {
            _interactions = interactions;
            _storage = storage;
            _commands = commands;
        }

        [SlashCommand("allstats", "Shows combined stats from all servers")]
        public async Task ShowAllStatsAsync([Choice("All", "all"), Choice("Dms", "dms"), Choice("Guilds", "guilds")]string type)
        {
            Account[] accounts;

            if (type == "dms")
            {
                accounts = Accounts.GetManyAccounts(x => !x.IsGuild);
            }
            else if (type == "guilds")
            {
                accounts = Accounts.GetManyAccounts(x => x.IsGuild);
            }
            else accounts = Accounts.GetAllAccounts();

            var allInfos = CalculateCommandStatsOnAccounts(accounts);

            var fields = new List<EmbedField>();
            var commands = allInfos.OrderByDescending(x => x.TimesRun).ToArray();

            for (int i = 0; i < commands.Length; i++)
            {
                fields.Add(new EmbedFieldBuilder()
                            .WithName(commands[i].Name)
                            .WithValue($"Executed {commands[i].TimesRun} times ({commands[i].TimesFailed} fails)")
                            .Build());
            }

            var title = Context.Interaction.IsDMInteraction ? $"Stats for {Context.User.Username}'s DMS" : $"Stats for {Context.Guild.Name}";

            var templateEmbed = new EmbedBuilder()
            {
                Title = title
            };

            await SendPaginatedMessageAsync(fields, null, templateEmbed);
        }

        [SlashCommand("addmoduletoguild", "Adds moderation command to guild")]
        [EnabledInDm(false)]
        public async Task AddModerationModuleToGuildAsync(string guildId, string modulename)
        {
            var module = _interactions.Modules.First(x => x.Name.ToLower() == modulename.ToLower());
            
            if (module == null)
            {
                await ModifyOriginalResponseAsync(x => x.Content = $"Could not find {modulename}");
                return;
            }

            await _interactions.AddModulesToGuildAsync(ulong.Parse(guildId), false, (ModuleInfo)module);
            await ModifyOriginalResponseAsync(x => x.Content = $"Added {module.Name} module to guild with {module.SlashCommands.Count}");
        }

        [SlashCommand("temp", "post all")]
        public async Task TempCommand()
        {
            var logs = _storage.RestoreAll<Log>().ToArray();
            var unbanLogs = logs.Where(x => x.Type == LogAction.Ban && string.IsNullOrEmpty(x.Reason)).ToArray();

            for (int i = 0; i < unbanLogs.Length; i++)
            {
                unbanLogs[i].Type = LogAction.Unban;
                _storage.Update(unbanLogs[i]);
            }

            logs = _storage.RestoreAll<Log>().ToArray();

            var channel = await Context.Client.GetChannelAsync(_commands.LogChannelId) as ISocketMessageChannel;

            for (int i = 0; i < logs.Length; i++)
            {
                var user = await GLClient.GetUserById(logs[i].VictimGameId.ToString());

                await channel.SendMessageAsync(embed: LogService.GetEmbedForLog(logs[i], user));
            }

            await ModifyOriginalResponseAsync(x => x.Content = "Done");
        }

        private List<CommandStats> CalculateCommandStatsOnAccounts(Account[] accounts)
        {
            var allInfos = new List<CommandStats>();

            for (int i = 0; i < accounts.Length; i++)
            {
                for (int j = 0; j < accounts[i].CommandStats.Count; j++)
                {
                    var cmdStats = accounts[i].CommandStats[j];
                    var foundCmd = allInfos.Find(x => x.Name == cmdStats.Name);

                    if (foundCmd == null)
                    {
                        allInfos.Add(cmdStats);
                        continue;
                    }

                    foundCmd.TimesRun += cmdStats.TimesRun;
                    foundCmd.TimesFailed += cmdStats.TimesFailed;
                }
            }

            return allInfos;
        }
    }
}
