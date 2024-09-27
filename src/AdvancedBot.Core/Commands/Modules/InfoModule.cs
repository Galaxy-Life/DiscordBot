using Discord;
using Discord.Interactions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AdvancedBot.Core.Commands.Modules
{
    public class InfoModule : TopModule
    {
        private readonly CustomCommandService _commands;

        public InfoModule(CustomCommandService commands)
        {
            _commands = commands;
        }

        [SlashCommand("help", "View basic help and information about the Galaxy Life bot.")]
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
                fields.Add(
                  new EmbedFieldBuilder()
                      .WithName(commands[i].Name)
                      .WithValue($"Executed {commands[i].TimesRun} times ({commands[i].TimesFailed} fails)")
                      .Build());
            }

            var title = Context.Interaction.IsDMInteraction ? $"Stats for {Context.User.Username}'s DMS" : $"Stats for {Context.Guild.Name}";
            var thumbnailUrl = Context.Interaction.IsDMInteraction ? Context.User.GetDisplayAvatarUrl() : Context.Guild.IconUrl;

            var templateEmbed = new EmbedBuilder()
                .WithTitle(title)
                .WithThumbnailUrl(thumbnailUrl)
                .WithFooter(footer => footer
                    .WithText($"{(
                        Context.Interaction.IsDMInteraction
                        ? $"{Context.User.Username}'s DMS stats"
                        : $"{Context.Guild.Name} guild stats")} requested by {Context.User.Username}#{Context.User.Discriminator}")
                    .WithIconUrl(Context.User.GetDisplayAvatarUrl()))
                .WithCurrentTimestamp();

            await SendPaginatedMessageAsync(fields, null, templateEmbed);
        }
    }
}
