using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace AdvancedBot.Core.Commands.Modules.Base
{
    [Name("info")]
    [Summary("Category that handles all information regarding the bot.")]
    public class CommandInformationModule : TopModule
    {
        [Command("help")]
        [Summary("Displays information about the bot.")]
        public async Task Help()
            => await Commands.SendBotInfoAsync(Context);

        [Command("help")]
        [Summary("Displays information about a specific command or category.")]
        public async Task Help([Remainder]string input)
        {
            var guild = Accounts.GetOrCreateGuildAccount(Context.Guild.Id);
            var result = Commands.AdvancedSearch(input);
            EmbedBuilder embed;

            if (result.Value is null)
                embed = Commands.CreateModuleInfoEmbed(result.Key, guild.DefaultDisplayPrefix);
            else embed = Commands.CreateCommandInfoEmbed(result.Value, guild.DefaultDisplayPrefix);

            await ReplyAsync("", false, embed.Build());
        }

        [Command("commands")][Alias("cmds")]
        [Summary("Lists all commands the bot has.")]
        public async Task DisplayAllCommands()
            => await ReplyAsync(Commands.AllCommandsToString());

        [Command("modules")][Alias("categories")]
        [Summary("Lists all modules the bot has.")]
        public async Task DisplayAllModules()
            => await ReplyAsync(Commands.AllModulesToString());
    }
}
