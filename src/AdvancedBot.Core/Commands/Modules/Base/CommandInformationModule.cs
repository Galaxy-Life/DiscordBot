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
    }
}
