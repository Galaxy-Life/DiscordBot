using System;
using System.Linq;
using System.Linq.Expressions;
using AdvancedBot.Core.Entities;

namespace AdvancedBot.Core.Services.DataStorage;

public class AccountService
{
    private readonly LiteDBHandler storage;

    public AccountService(LiteDBHandler storage)
    {
        this.storage = storage;
    }

    public Account GetOrCreateAccount(ulong id, bool isGuild = false)
    {
        if (!storage.Exists<Account>(x => x.Id == id))
        {
            var account = new Account(id, isGuild);

            SaveAccount(account);
            return account;
        }

        return storage.RestoreSingle<Account>(x => x.Id == id);
    }

    public Account[] GetAllAccounts()
    {
        return storage.RestoreAll<Account>().ToArray();
    }

    public Account[] GetManyAccounts(Expression<Func<Account, bool>> predicate)
    {
        return storage.RestoreMany(predicate).ToArray();
    }

    public void SaveAccount(Account account)
    {
        if (!storage.Exists<Account>(x => x.Id == account.Id))
        {
            storage.Store(account);
        }
        else
        {
            storage.Update(account);
        }
    }
}
