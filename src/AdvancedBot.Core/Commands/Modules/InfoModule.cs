using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;

namespace AdvancedBot.Core.Commands.Modules
{
    public class InfoModule : TopModule
    {
        [SlashCommand("help", "Gives basic info about the bot")]
        public async Task DisplayBotInfoAsync()
        {
            await Commands.SendBotInfoAsync(Context);
        }

        [SlashCommand("serverstats", "Shows guild stats related to the bot")]
        public async Task DisplayGuildOrDmStatsAsync()
        {
            var id = Context.Interaction.IsDMInteraction ? Context.User.Id : Context.Guild.Id;
            var guild = Accounts.GetOrCreateAccount(id, !Context.Interaction.IsDMInteraction);

            var fields = new List<EmbedField>();

            for (int i = 0; i < guild.CommandInfos.Count; i++)
            {
                var info = guild.CommandInfos[i];

                fields.Add(new EmbedFieldBuilder()
                            .WithName(info.Name)
                            .WithValue($"Executed {info.TimesRun} times ({info.TimesFailed} fails)")
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
