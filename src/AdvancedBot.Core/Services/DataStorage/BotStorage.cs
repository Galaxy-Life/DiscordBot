using System.Collections.Generic;
using System.Linq;
using AdvancedBot.Core.Entities;

namespace AdvancedBot.Core.Services.DataStorage
{
    public class BotStorage
    {
        private readonly LiteDBHandler _storage;

        public BotStorage(LiteDBHandler storage)
        {
            _storage = storage;
        }

        public void AddTempBan(Tempban ban)
        {
            _storage.Store(ban);
        }

        public List<Tempban> GetTempbans()
        {
            return _storage.RestoreAll<Tempban>().ToList();
        }

        public void RemoveTempban(Tempban tempban)
        {
            _storage.Remove<Tempban>(ban => ban.ModeratorId == tempban.ModeratorId && ban.UserId == tempban.UserId);
        }
    }
}
