using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using LiteDB;

namespace AdvancedBot.Core.Services.DataStorage
{
    public class LiteDBHandler
    {
        private string _dbFileName = "Data.db";

        public void Store<T>(T item)
        {
            using (var db = new LiteDatabase(_dbFileName))
            {
                var collection = db.GetCollection<T>();
                collection.Insert(item);
            }
        }

        public void Update<T>(T item)
        {
            using (var db = new LiteDatabase(_dbFileName))
            {
                var collection = db.GetCollection<T>();
                collection.Update(item);
            }
        }

        public IEnumerable<T> RestoreMany<T>(Expression<Func<T, bool>> predicate)
        {
            using (var db = new LiteDatabase(_dbFileName))
            {
                var collection = db.GetCollection<T>();
                return collection.Find(predicate);
            }
        }

        public T RestoreSingle<T>(Expression<Func<T, bool>> predicate)
        {
            using (var db = new LiteDatabase(_dbFileName))
            {
                var collection = db.GetCollection<T>();
                return collection.Find(predicate).FirstOrDefault();
            }
        }

        public bool Exists<T>(Expression<Func<T, bool>> predicate)
        {
            using (var db = new LiteDatabase(_dbFileName))
            {
                var collection = db.GetCollection<T>();
                return collection.Exists(predicate);
            }
        }
    }
}
