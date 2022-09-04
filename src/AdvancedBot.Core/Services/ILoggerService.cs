using AdvancedBot.Core.Entities.Enums;

namespace AdvancedBot.Core.Services
{
    public interface ILoggerService
    {
        void LogBotAction();
        void LogGuildAction(LogAction action, int guildId, string message);
    }
}
