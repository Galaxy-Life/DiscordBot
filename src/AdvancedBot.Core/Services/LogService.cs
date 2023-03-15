using AdvancedBot.Core.Entities;
using AdvancedBot.Core.Entities.Enums;
using AdvancedBot.Core.Services.DataStorage;

namespace AdvancedBot.Core.Services
{
    public class LogService
    {
        private int _id = 0;
        private LiteDBHandler _storage;

        public LogService(LiteDBHandler storage)
        {
            _storage = storage;

            _id = _storage.RestoreCount<Log>();
        }

        public void LogGameAction(LogAction action, ulong discordModId, uint victimGameId, string reason = "")
        {
            _id++;
            var log = new Log(_id, action, discordModId, victimGameId, reason);

            _storage.Store(log);
        }
    }
}
