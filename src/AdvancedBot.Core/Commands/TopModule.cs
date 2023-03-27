using System.Linq;
using Discord;
using AdvancedBot.Core.Services.DataStorage;
using AdvancedBot.Core.Services;
using System.Threading.Tasks;
using System.Collections.Generic;
using Discord.Interactions;
using GL.NET.Entities;
using GL.NET;
using System;

namespace AdvancedBot.Core.Commands
{
    public class TopModule : InteractionModuleBase<SocketInteractionContext>
    {
        public AccountService Accounts { get; set; }
        public AuthorizedGLClient GLClient { get; set; }
        public PaginatorService Paginator { get; set; }
        public LogService LogService { get; set; }

        public readonly List<ulong> PowerUsers = new List<ulong>() { 209801906237865984, 202095042372829184, 942849642931032164 };

        public override async Task BeforeExecuteAsync(ICommandInfo command)
        {
            if (command.SupportsWildCards || command.Name.Contains(":") || command.Name.Contains(","))
            {
                return;
            }

            await DeferAsync();
        }

        public override Task AfterExecuteAsync(ICommandInfo command)
        {
            return Task.CompletedTask;
        }

        protected async Task SendPaginatedMessageAsync(IEnumerable<EmbedField> displayFields, IEnumerable<string> displayTexts, EmbedBuilder templateEmbed)
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

        protected async Task<PhoenixUser> GetPhoenixUserByInput(string input, bool full = false)
        {
            if (string.IsNullOrEmpty(input)) input = Context.User.Username;
            PhoenixUser user = null;

            var digitString = new String(input.Where(Char.IsDigit).ToArray());

            // extra check to see if all characters were numbers
            if (digitString.Length == input.Length)
            {
                if (full)
                {
                    user = await GLClient.GetFullPhoenixUserAsync(input);
                }
                else
                {
                    user = await GLClient.GetPhoenixUserAsync(input);
                }
            }

            // try to get user by name
            if (user == null)
            {
                user = await GLClient.GetPhoenixUserByNameAsync(input);
            }

            // get user by id after getting it by name
            if (user != null && full)
            {
                user = await GLClient.GetFullPhoenixUserAsync(user.UserId);
            }

            return user;
        }
    }
}
