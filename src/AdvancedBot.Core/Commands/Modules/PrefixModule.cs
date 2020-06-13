using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using AdvancedBot.Core.Commands.Preconditions;

namespace AdvancedBot.Core.Commands.Modules
{
    [RequireCustomPermission(GuildPermission.ManageGuild)]
    [Group("prefix")][Alias("prefixes")]
    public class PrefixModule : TopModule
    {
        [Command("add")]
        [Summary("Adds the given prefix to this guild's prefixlist.")]
        public async Task AddPrefix([Remainder]string prefix)
        {
            var guild = Accounts.GetOrCreateGuildAccount(Context.Guild.Id);
            guild.AddPrefix(prefix);
            Accounts.SaveGuildAccount(guild);

            await ReplyAsync($"Successfully added `{prefix}` to the current list.");
        }

        [Command("remove")]
        [Summary("Removes the given prefix to the guild's prefixlist.")]
        public async Task RemovePrefix([Remainder]string prefix)
        {
            var guild = Accounts.GetOrCreateGuildAccount(Context.Guild.Id);
            guild.RemovePrefix(prefix);
            Accounts.SaveGuildAccount(guild);

            await ReplyAsync($"Successfully removed `{prefix}` from the current list.");
        }

        [Command("clear")]
        [Summary("Clears all the prefixes for a certain guild.")]
        public async Task ClearPrefixes()
        {
            var guild = Accounts.GetOrCreateGuildAccount(Context.Guild.Id);
            guild.Prefixes.Clear();
            Accounts.SaveGuildAccount(guild);

            await ReplyAsync($"Successfully cleared the prefixes.\nOnly working prefix now is bot mention.");
        }

        [Command][Alias("list")][Name("")]
        [Summary("Lists the current prefixes.")]
        public async Task ListPrefix()
        {
            var guild = Accounts.GetOrCreateGuildAccount(Context.Guild.Id);

            await ReplyAsync($"**Prefixes for {Context.Guild.Name}:**\n" +
                            $"`{string.Join("`, `", guild.Prefixes)}`");
        }
    }
}
