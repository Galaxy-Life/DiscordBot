using System.Threading.Tasks;
using Discord.Interactions;

namespace AdvancedBot.Core.Commands.Modules
{
    public class HelpModule : TopModule
    {
        [SlashCommand("help", "Gives basic info about the bot")]
        public async Task DisplayBotInfoAsync()
        {
            await DeferAsync();

            await Commands.SendBotInfoAsync(Context);
        }
    }
}
