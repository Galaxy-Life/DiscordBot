using System.Linq;
using Discord;
using AdvancedBot.Core.Services.DataStorage;
using AdvancedBot.Core.Services;
using System.Threading.Tasks;
using System.Collections.Generic;
using Discord.Interactions;

namespace AdvancedBot.Core.Commands
{
    public class TopModule : InteractionModuleBase<SocketInteractionContext>
    {
        public GuildAccountService Accounts { get; set; }
        public CustomCommandService Commands { get; set; }
        public PaginatorService Paginator { get; set; }

        public override async Task BeforeExecuteAsync(ICommandInfo command)
        {
            
        }

        public override async Task AfterExecuteAsync(ICommandInfo command)
        {

        }

    
        public async Task SendPaginatedMessageAsync(IEnumerable<EmbedField> displayFields, IEnumerable<string> displayTexts, EmbedBuilder templateEmbed)
        {
            var displayItems = 0;
            
            if (displayTexts != null)
            {
                templateEmbed.WithDescription(string.Join("\n", displayTexts.Take(10)));
                displayItems = displayTexts.Count();
            }
            else if (displayFields != null)
            {
                displayItems = displayFields.Count();
                var fields = displayFields.Take(10).ToArray();

                for (int i = 0; i < fields.Length; i++)
                {
                    templateEmbed.AddField(fields[i].Name, fields[i].Value, fields[i].Inline);
                }
            }

            templateEmbed.WithTitle($"{templateEmbed.Title} (Page 1)");
            templateEmbed.WithFooter($"Total of {displayItems} players");

            await Paginator.HandleNewPaginatedMessageAsync(Context, displayFields, displayTexts, templateEmbed.Build());
            await Task.Delay(1000);
        }
    }
}
