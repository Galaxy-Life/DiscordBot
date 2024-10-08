﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AdvancedBot.Core.Commands.Preconditions;
using AdvancedBot.Core.Entities;
using Discord;
using Discord.Interactions;

namespace AdvancedBot.Core.Commands.Modules;

[DontAutoRegister]
[RequirePrivateList]
public class DevModule : TopModule
{
    private readonly InteractionService _interactions;

    public DevModule(InteractionService interactions)
    {
        _interactions = interactions;
    }

    [SlashCommand("allstats", "Shows combined stats from all servers")]
    public async Task ShowAllStatsAsync([Choice("All", "all"), Choice("Dms", "dms"), Choice("Guilds", "guilds")] string type)
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

        var allInfos = calculateCommandStatsOnAccounts(accounts);

        var fields = new List<EmbedField>();
        var commands = allInfos.OrderByDescending(x => x.TimesRun).ToArray();

        for (var i = 0; i < commands.Length; i++)
        {
            fields.Add(
              new EmbedFieldBuilder()
                  .WithName(commands[i].Name)
                  .WithValue($"Executed {commands[i].TimesRun} times ({commands[i].TimesFailed} fails)")
                  .Build());
        }

        var title = Context.Interaction.IsDMInteraction ? $"Stats for {Context.User.Username}'s DMS" : $"Stats for {Context.Guild.Name}";

        var templateEmbed = new EmbedBuilder()
            .WithTitle(title);

        await SendPaginatedMessageAsync(fields, null, templateEmbed);
    }

    [SlashCommand("addmoduletoguild", "Adds moderation command to guild")]
    [CommandContextType(InteractionContextType.Guild, InteractionContextType.PrivateChannel)]
    public async Task AddModerationModuleToGuildAsync(string guildId, string modulename)
    {
        var module = _interactions.Modules.First(module => module.Name.Equals(modulename, System.StringComparison.CurrentCultureIgnoreCase));

        if (module == null)
        {
            await ModifyOriginalResponseAsync(msg => msg.Content = $"Could not find any module named **{modulename}**.");
            return;
        }

        await _interactions.AddModulesToGuildAsync(ulong.Parse(guildId), false, module);

        var embed = new EmbedBuilder()
            .WithTitle("Module successfully added")
            .WithDescription($"Added **{module.Name}** to the server.")
            .AddField("Slash Commands", $"{module.SlashCommands.Count}", true)
            .WithColor(Color.Green)
            .WithFooter(footer => footer
                .WithText($"Module added by {Context.User.Username}#{Context.User.Discriminator}")
                .WithIconUrl(Context.User.GetDisplayAvatarUrl()))
            .WithCurrentTimestamp()
            .Build();

        await ModifyOriginalResponseAsync(msg => msg.Embeds = new Embed[] { embed });
    }

    private static List<CommandStats> calculateCommandStatsOnAccounts(Account[] accounts)
    {
        var allInfos = new List<CommandStats>();

        for (var i = 0; i < accounts.Length; i++)
        {
            for (var j = 0; j < accounts[i].CommandStats.Count; j++)
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
