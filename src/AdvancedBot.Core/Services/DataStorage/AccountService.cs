using System;
using System.Linq;
using System.Linq.Expressions;
using AdvancedBot.Core.Entities;

namespace AdvancedBot.Core.Services.DataStorage;

public class AccountService
{
    private readonly LiteDBHandler _storage;

    public AccountService(LiteDBHandler storage)
    {
        _storage = storage;
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

    public Account[] GetAllAccounts()
    {
        return _storage.RestoreAll<Account>().ToArray();
    }

    public Account[] GetManyAccounts(Expression<Func<Account, bool>> predicate)
    {
        return _storage.RestoreMany(predicate).ToArray();
    }

    public void SaveAccount(Account account)
    {
        if (!_storage.Exists<Account>(x => x.Id == account.Id))
        {
            _storage.Store(account);
        }
        else
        {
            _storage.Update(account);
        }
    }
}
