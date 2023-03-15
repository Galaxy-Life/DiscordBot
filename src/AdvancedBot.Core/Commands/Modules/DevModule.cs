using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AdvancedBot.Core.Commands.Preconditions;
using AdvancedBot.Core.Entities;
using Discord;
using Discord.Interactions;

namespace AdvancedBot.Core.Commands.Modules
{
    [DontAutoRegister]
    [RequirePrivateList]
    public class DevModule : TopModule
    {
        private InteractionService _interactions;

        public DevModule(InteractionService interactions)
        {
            _interactions = interactions;
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

        [SlashCommand("addmoderationtoguild", "Adds moderation command to guild")]
        [EnabledInDm(false)]
        public async Task AddModerationModuleToGuildAsync(string guildId)
        {
            var moderationModule = _interactions.Modules.First(x => x.Name == "ModerationModule");
            await _interactions.AddModulesToGuildAsync(ulong.Parse(guildId), false, moderationModule);

            await ModifyOriginalResponseAsync(x => x.Content = $"Added moderation module to guild");
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
