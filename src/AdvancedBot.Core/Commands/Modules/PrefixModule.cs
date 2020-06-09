using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using AdvancedBot.Core.Commands.Preconditions;
using AdvancedBot.Core.Services.DataStorage;

namespace AdvancedBot.Core.Commands.Modules
{
    [RequireCustomPermission(GuildPermission.ManageGuild)]
    [Group("prefix")]
    public class PrefixModule : CustomModule
    {
        private GuildAccountService _accounts;

        public PrefixModule(GuildAccountService accounts)
        {
            _accounts = accounts;
        }

        [Command("add")]
        public async Task AddPrefix([Remainder]string prefix)
        {
            var guild = _accounts.GetOrCreateGuildAccount(Context.Guild.Id);
            guild.AddPrefix(prefix);
            _accounts.SaveGuildAccount(guild);

            await ReplyAsync($"Successfully added `{prefix}` to the current list.");
        }

        [Command("remove")]
        public async Task RemovePrefix([Remainder]string prefix)
        {
            var guild = _accounts.GetOrCreateGuildAccount(Context.Guild.Id);
            guild.RemovePrefix(prefix);
            _accounts.SaveGuildAccount(guild);

            await ReplyAsync($"Successfully removed `{prefix}` from the current list.");
        }

        [Command][Alias("list")]
        public async Task ListPrefix()
        {
            var guild = _accounts.GetOrCreateGuildAccount(Context.Guild.Id);

            await ReplyAsync($"**Prefixes for {Context.Guild.Name}:**\n" +
                            $"`{string.Join("`, `", guild.Prefixes)}`");
        }
    }
}
