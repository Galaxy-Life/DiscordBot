using System.Threading.Tasks;
using AdvancedBot.Core;

namespace AdvancedBot.Console
{
    public class Program
    {
        public static async Task Main(string[] args)
            => await new BotClient().InitializeAsync();
    }
}
