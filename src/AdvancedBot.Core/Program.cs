using System.Threading.Tasks;

namespace AdvancedBot.Core
{
    public class Program
    {
        public static Task Main(string[] args)
            => new BotClient().InitializeAsync();
    }
}
