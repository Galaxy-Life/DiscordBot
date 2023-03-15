using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using LiteDB;

namespace AdvancedBot.Core.Services.DataStorage
{
    public class LiteDBHandler
    {
        private const string _dbFileName = "Data.db";
        private LiteDatabase _db = new LiteDatabase(_dbFileName);

        public void Store<T>(T item)
        {
            var collection = _db.GetCollection<T>();
            collection.Insert(item);
        }

        public void Update<T>(T item)
        {
            var collection = _db.GetCollection<T>();
            collection.Update(item);
        }

        public IEnumerable<T> RestoreMany<T>(Expression<Func<T, bool>> predicate)
        {
            var collection = _db.GetCollection<T>();
            return collection.Find(predicate);
        }

        public IEnumerable<T> RestoreAll<T>()
        {
            var collection = _db.GetCollection<T>();
            return collection.FindAll();
        }

        public int RestoreCount<T>()
        {
            var collection = _db.GetCollection<T>();
            return collection.Count();
        }

        public T RestoreSingle<T>(Expression<Func<T, bool>> predicate)
        {
            var collection = _db.GetCollection<T>();
            return collection.Find(predicate).FirstOrDefault();
        }

        public bool Exists<T>(Expression<Func<T, bool>> predicate)
        {
            var collection = _db.GetCollection<T>();
            return collection.Exists(predicate);
        }
    }
}
