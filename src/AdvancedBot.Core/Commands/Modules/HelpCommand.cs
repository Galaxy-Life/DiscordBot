using System.Threading.Tasks;
using Discord.Commands;

namespace AdvancedBot.Core.Commands.Modules
{
    public class HelpCommand : CustomModule
    {
        [Command("help")]
        [Summary("Displays the help command.")]
        public async Task Help()
            => await Commands.SendHelpCommand(Context);

        [Command("help")]
        [Summary("Displays the help command for a specific command.")]
        public async Task Help([Remainder]string input)
        {
            if (InputIsModule(input))
            {
                // TODO: implement
                return;
            }

            var command = Commands.GetCommandInfo(input);

            var embed = Commands.GetCommandHelpEmbed(command);
            var usageField = Commands.GenerateUsageField(command);

            embed.AddField(usageField);

            await ReplyAsync("", false, embed.Build());
        }

        private bool InputIsModule(string input)
        {
            return false;
        }
    }
}
