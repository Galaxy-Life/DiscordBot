using System.Collections.Generic;
using System.Linq;
using AdvancedBot.Core.Entities;

namespace AdvancedBot.Core.Services.DataStorage
{
    public class BotStorage
    {
        private readonly LiteDBHandler storage;

        public BotStorage(LiteDBHandler storage)
        {
            this.storage = storage;
        }

        public void AddTempBan(Tempban ban)
        {
            storage.Store(ban);
        }

        public List<Tempban> GetTempbans()
        {
            return storage.RestoreAll<Tempban>().ToList();
        }

        public void RemoveTempban(Tempban tempban)
        {
            storage.Remove<Tempban>(ban => ban.ModeratorId == tempban.ModeratorId && ban.UserId == tempban.UserId);
        }
    }
}
