using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AdvancedBot.Core.Commands.Preconditions;
using Discord;
using Discord.Commands;

namespace AdvancedBot.Core.Commands.Modules.Base
{
    [Group("purge")]
    [Summary("Category that holds all the purge commands.")]
    [RequireCustomPermission(GuildPermission.ManageMessages)]
    [RequireBotPermission(GuildPermission.ManageMessages)]
    public class PurgeModule : TopModule
    {
        [Command("")]
        [Summary("Purges the last x messages.")]
        public async Task DefaultPurgeAsync(int search = 100)
            => await ReplyAsync($"", false, await HandlePurgeCommandAsync(search));

        [Command("user")]
        [Summary("Purges the last x messages by said user.")]
        public async Task PurgeUserAsync(IUser user, int search = 100)
            => await ReplyAsync($"", false, await HandlePurgeCommandAsync(search, user));

        private async Task<Embed> HandlePurgeCommandAsync(int amount, IUser user = null)
        {
            if (amount <= 0)
                throw new Exception($"Please enter a positive number larger than 0.");
            
            amount = amount > 1000 ? 1000 : amount;

            var channel = (ITextChannel) Context.Channel;

            var messages = await GetLatestMessagesFromChannel(channel, amount, user);
            await channel.DeleteMessagesAsync(messages);

            var info = GetInfoFromMessages(messages.ToArray());
            return GetPurgeStatisticsEmbed(Context.User, info, amount);
        }

        private Dictionary<string, int> GetInfoFromMessages(IMessage[] msgs)
        {
            var messages = new Dictionary<string, int>();

            for (int i = 0; i < msgs.Length; i++)
            {
                if (messages.ContainsKey(msgs[i].Author.Mention))
                {
                    messages[msgs[i].Author.Mention]++;
                }
                else messages.Add(msgs[i].Author.Mention, 1);
            }

            return messages;
        }

        private Embed GetPurgeStatisticsEmbed(IUser purger, Dictionary<string, int> info, int totalMessages)
            => new EmbedBuilder()
            {
                Title = $"Checked {totalMessages} messages",
                Description = $"**Deleted messages:**\n{string.Join("\n", info.Select(x => $"**{x.Key}**: {x.Value}"))}\n\u200b",
                Color = Color.Blue
            }
            .WithFooter($"{purger.Username} ({purger.Id})", purger.GetAvatarUrl())
            .Build();

        private async Task<IEnumerable<IMessage>> GetLatestMessagesFromChannel(ITextChannel channel, int amount, IUser user)
        {
            var messages = await channel.GetMessagesAsync(amount + 1).FlattenAsync();
            var filtered = messages.Where(x => !x.IsPinned).Skip(1);

            if (user != null)
                filtered = filtered.Where(x => x.Author.Id == user.Id);

            return filtered;
        }
    }
}
