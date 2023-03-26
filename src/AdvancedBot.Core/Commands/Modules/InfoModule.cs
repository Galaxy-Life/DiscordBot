using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;

namespace AdvancedBot.Core.Commands.Modules
{
    public class InfoModule : TopModule
    {
        private CustomCommandService _commands;

        public InfoModule(CustomCommandService commands)
        {
            _commands = commands;
        }

        [SlashCommand("help", "Gives basic info about the bot")]
        public async Task DisplayBotInfoAsync()
        {
            await _commands.SendBotInfoAsync(Context);
        }

        [SlashCommand("serverstats", "Shows guild stats related to the bot")]
        public async Task DisplayGuildOrDmStatsAsync()
        {
            var id = Context.Interaction.IsDMInteraction ? Context.User.Id : Context.Guild.Id;
            var guild = Accounts.GetOrCreateAccount(id, !Context.Interaction.IsDMInteraction);

            var fields = new List<EmbedField>();
            var commands = guild.CommandStats.OrderByDescending(x => x.TimesRun).ToArray();

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
    }
}
