using Discord;
using Discord.Commands;
using Discord.Interactions;
using System.Threading.Tasks;
using SummaryAttribute = Discord.Commands.SummaryAttribute;

namespace AdvancedBot.Core.Commands.Modules.Base
{
    [Name("info")]
    [Summary("Category that handles all information regarding the bot.")]
    public class CommandInformationModule : TopModule
    {
        [SlashCommand("help", "Displays information about the bot")]
        [Command("help")]
        [Summary("Displays information about the bot.")]
        public async Task Help()
            => await Commands.SendBotInfoAsync(Context);

        [SlashCommand("help", "Displays information about a specific command or category")]
        [Command("help")]
        [Summary("Displays information about a specific command or category.")]
        public async Task Help([Remainder] string input)
        {
            var guild = Accounts.GetOrCreateGuildAccount(Context.Guild.Id);
            var result = Commands.AdvancedSearch(input);
            EmbedBuilder embed;

            if (result.Value is null)
                embed = Commands.CreateModuleInfoEmbed(result.Key, guild.DefaultDisplayPrefix);
            else embed = Commands.CreateCommandInfoEmbed(result.Value, guild.DefaultDisplayPrefix);

            await ReplyAsync("", false, embed.Build());
        }

        [SlashCommand("help", "Lists all commands the bot has")]
        [Command("commands")]
        [Alias("cmds")]
        [Summary("Lists all commands the bot has.")]
        public async Task DisplayAllCommands()
            => await ReplyAsync(Commands.AllCommandsToString());

        [SlashCommand("help", "Lists all modules the bot has")]
        [Command("modules")]
        [Alias("categories")]
        [Summary("Lists all modules the bot has.")]
        public async Task DisplayAllModules()
            => await ReplyAsync(Commands.AllModulesToString());
    }
}
