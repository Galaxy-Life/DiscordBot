using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using AdvancedBot.Core.Commands.Preconditions;
using AdvancedBot.Core.Services.DataStorage;

namespace AdvancedBot.Core.Commands.Modules
{
    [RequireCustomPermission(GuildPermission.ManageGuild)]
    [Group("prefix")][Alias("prefixes")]
    public class PrefixModule : CustomModule
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

        [Command][Alias("list")]
        [Summary("Lists the current prefixes.")]
        public async Task ListPrefix()
        {
            var guild = Accounts.GetOrCreateGuildAccount(Context.Guild.Id);

            await ReplyAsync($"**Prefixes for {Context.Guild.Name}:**\n" +
                            $"`{string.Join("`, `", guild.Prefixes)}`");
        }
    }
}
