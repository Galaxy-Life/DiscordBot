using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using AdvancedBot.Core.Commands.Preconditions;
using AdvancedBot.Core.Services.DataStorage;

namespace AdvancedBot.Core.Commands.Modules
{
    [RequireCustomPermission(GuildPermission.ManageGuild)]
    public class PrefixModule : CustomModule
    {
        private GuildAccountService _accounts;

        public PrefixModule(GuildAccountService accounts)
        {
            _accounts = accounts;
        }


        [Command("prefixadd")][Alias("padd")]
        public async Task AddPrefix([Remainder]string prefix)
        {
            var guild = _accounts.GetOrCreateGuildAccount(Context.Guild.Id);
            guild.AddPrefix(prefix);
            _accounts.SaveGuildAccount(guild);

            await ReplyAsync($"Successfully added `{prefix}` to the current list.");
        }

        [Command("prefixremove")][Alias("premove")]
        public async Task RemovePrefix([Remainder]string prefix)
        {
            var guild = _accounts.GetOrCreateGuildAccount(Context.Guild.Id);
            guild.RemovePrefix(prefix);
            _accounts.SaveGuildAccount(guild);

            await ReplyAsync($"Successfully removed `{prefix}` from the current list.");
        }

        [Command("prefixlist")][Alias("plist", "prefixes")]
        public async Task ListPrefix()
        {
            var guild = _accounts.GetOrCreateGuildAccount(Context.Guild.Id);

            await ReplyAsync($"Prefixes for **{Context.Guild.Name}**:\n" +
                            $"▬▬▬▬▬▬▬▬▬▬▬▬\n" + 
                            $"`{string.Join("`, `", guild.Prefixes)}`");
        }
    }
}
