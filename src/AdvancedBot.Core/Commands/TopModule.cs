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
using AdvancedBot.Core.Entities;

namespace AdvancedBot.Core.Commands
{
    public class TopModule : InteractionModuleBase<SocketInteractionContext>
    {
        public AccountService Accounts { get; set; }
        public AuthorizedGLClient GLClient { get; set; }
        public PaginatorService Paginator { get; set; }
        public LogService LogService { get; set; }

        public readonly List<ulong> PowerUsers = new List<ulong>() { 202095042372829184, 209801906237865984, 942849642931032164, 362271714702262273 };

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

        protected async Task SendResponseMessage(ResponseMessage response, bool followup)
        {
            if (followup)
            {
                await FollowupAsync(response.Content, response.Embeds, ephemeral: response.Ephemeral);
                return;
            }

            if (!string.IsNullOrEmpty(response.Content))
            {
                await ModifyOriginalResponseAsync(x => x.Content = response.Content);
            }
            if (response.Embeds != null)
            {
                await ModifyOriginalResponseAsync(x => x.Embeds = response.Embeds);
            }
        }

        protected MessageComponent CreateDefaultComponents(string username, string userId, string alliance, bool isBanned)
        {
            var components = new ComponentBuilder();
            // component magic, cannot handle empty values
            alliance = alliance == " " ? null : alliance;

            components.WithButton("Profile", $"profile:{username},{userId}", ButtonStyle.Primary, Emote.Parse("<:AFCElderby:943325489009934368>"));
            components.WithButton("Stats", $"stats:{username},{userId}", ButtonStyle.Primary, Emote.Parse("<:AACLooter:943311525320482877>"));

            if (!string.IsNullOrEmpty(alliance))
            {
                components.WithButton("Alliance", $"alliance:{alliance}", ButtonStyle.Primary, Emote.Parse("<:AFECounselor_Mobius:1082315024829272154>"));
                components.WithButton("Members", $"members:{alliance}", ButtonStyle.Primary, Emote.Parse("<:Major_Wor:944250193279324171>"));
            }

            if (PowerUsers.Contains(Context.User.Id))
            {
                components.WithButton("Moderation", $"moderation:{username},{userId},{alliance ?? " "},{isBanned}", ButtonStyle.Secondary, new Emoji("➕"));
            }

            return components.Build();
        }

        protected MessageComponent CreateModerationComponents(string username, string userId, string alliance, bool isBanned)
        {
            var components = new ComponentBuilder();

            components.WithButton("Add Beta", $"addbeta:{userId}", ButtonStyle.Success, Emote.Parse("<:based:943444391677263912>"), row: 0);
            components.WithButton("Remove Beta", $"removebeta:{userId}", ButtonStyle.Danger, Emote.Parse("<:Sadge:945682815327035432>"), row: 0);
            components.WithButton("Give Role", $"giverole:{userId}", ButtonStyle.Primary, Emote.Parse("<:AAAStarlingSwag:943310403658715196>"), disabled: true, row: 0);
            components.WithButton("Chips bought", $"chipsbought:{userId}", ButtonStyle.Primary, Emote.Parse("<:AACPileOfChips:943313554742865951>"), row: 0);

            components.WithButton("Back", $"back:{username},{userId},{alliance},{isBanned}", ButtonStyle.Secondary, new Emoji("↩️"), row: 1);
            components.WithButton("Add Chips", $"addchips:{username},{userId}", ButtonStyle.Success, Emote.Parse("<:CABGalaxy_Chip:943313446940868678>"), row: 1);
            components.WithButton("Add Item", $"additem:{username},{userId}", ButtonStyle.Success, Emote.Parse("<:gltoolbox:1084821705316376576>"), row: 1);

            if (isBanned)
            {
                components.WithButton("Unban", $"unban:{username},{userId}", ButtonStyle.Success, Emote.Parse("<:AABStarling_happy:946859412763578419>"), row: 1);
            }
            else
            {
                components.WithButton("Ban", $"ban:{username},{userId}", ButtonStyle.Danger, Emote.Parse("<:ABEKamikaze:943323658837958686>"), row: 1);
            }

            return components.Build();
        }

        protected async Task<User> GetUserByInput(string input)
        {
            if (string.IsNullOrEmpty(input)) input = Context.User.Username;

            var profile = await GLClient.GetUserById(input);

            if (profile == null)
            {
                profile = await GLClient.GetUserByName(input);
            }

            return profile;
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
