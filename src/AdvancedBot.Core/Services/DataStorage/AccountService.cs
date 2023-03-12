using AdvancedBot.Core.Entities;
using AdvancedBot.Core.Commands;

namespace AdvancedBot.Core.Services.DataStorage
{
    public class AccountService
    {
        private LiteDBHandler _storage;
        private CustomCommandService _commands;

        public AccountService(LiteDBHandler storage, CustomCommandService commands)
        {
            _storage = storage;
            _commands = commands;
        }

        public Account GetOrCreateAccount(ulong id, bool isGuild = false)
        {
            if (!_storage.Exists<Account>(x => x.Id == id))
            {
                var account = new Account(id, isGuild);

                SaveAccount(account);
                return account;
            }
            
            return _storage.RestoreSingle<Account>(x => x.Id == id);
        }

        public void SaveAccount(Account account)
        {
            if (!_storage.Exists<Account>(x => x.Id == account.Id))
            {
                _storage.Store<Account>(account);
            }
            else
            {
                _storage.Update<Account>(account);
            }
        }
    }
}
