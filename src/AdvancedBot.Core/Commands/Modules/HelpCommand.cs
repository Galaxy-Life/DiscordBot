using System.Threading.Tasks;
using Discord.Commands;

namespace AdvancedBot.Core.Commands.Modules
{
    public class HelpCommand : CustomModule
    {
        private CustomCommandService _commands;

        public HelpCommand(CustomCommandService commands)
        {
            _commands = commands;
        }

        [Command("help")]
        [Summary("Displays the help command.")]
        public async Task Help()
            => await _commands.SendHelpCommand(Context);

        [Command("help")]
        [Summary("Displays the help command for a specific command.")]
        public async Task Help(string input)
        {
            if (InputIsModule(input))
            {
                // TODO: implement
                return;
            }

            var command = _commands.GetCommandInfo(input);

            var embed = _commands.GetCommandHelpEmbed(command);
            var usageField = _commands.GenerateUsageField(command);

            embed.AddField(usageField);

            await ReplyAsync("", false, embed.Build());
        }

        private bool InputIsModule(string input)
        {
            return false;
        }
    }
}
