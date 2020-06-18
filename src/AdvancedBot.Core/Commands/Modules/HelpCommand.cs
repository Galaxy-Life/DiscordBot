using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace AdvancedBot.Core.Commands.Modules
{
    public class HelpCommand : TopModule
    {
        [Command("help")]
        [Summary("Displays the help command.")]
        public async Task Help()
            => await Commands.SendBotInfoAsync(Context);

        [Command("help")]
        [Summary("Displays the help command for a specific command.")]
        public async Task Help([Remainder]string input)
        {
            var result = Commands.AdvancedSearch(input);
            EmbedBuilder embed;

            if (result.Value is null)
                embed = Commands.CreateModuleInfoEmbed(result.Key);
            else embed = Commands.CreateCommandInfoEmbed(result.Value);

            await ReplyAsync("", false, embed.Build());
        }

        private bool InputIsModule(string input)
        {
            return false;
        }
    }
}
