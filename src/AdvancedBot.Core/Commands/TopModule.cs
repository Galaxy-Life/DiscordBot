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
        public AccountService Accounts { get; set; }
        public CustomCommandService Commands { get; set; }
        public PaginatorService Paginator { get; set; }

        public override async Task BeforeExecuteAsync(ICommandInfo command)
        {
            await DeferAsync();
        }

        public override Task AfterExecuteAsync(ICommandInfo command)
        {
            return Task.CompletedTask;
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

            if (displayItems == 0)
            {
                await Context.Interaction.ModifyOriginalResponseAsync(x => x.Content = "Nothing to display here");
                return;
            }
            else if (displayItems > 10)
            {
                templateEmbed.WithTitle($"{templateEmbed.Title} (Page 1)");
            }

            await Paginator.HandleNewPaginatedMessageAsync(Context, displayFields, displayTexts, templateEmbed.Build());
            await Task.Delay(1000);
        }
    }
}
