using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace OpenNETCF.ORM.SQLite
{
    public class SQLiteDataStore : DataStore<SQLiteEntityInfo>, IDisposable
    {
        public override void CreateStore()
        {
            throw new NotImplementedException();
        }

        public override void DeleteStore()
        {
            throw new NotImplementedException();
        }

        public override bool StoreExists
        {
            get { throw new NotImplementedException(); }
        }

        public override void Insert(object item, bool insertReferences)
        {
            throw new NotImplementedException();
        }

        public override T[] Select<T>()
        {
            throw new NotImplementedException();
        }

        public override T Select<T>(object primaryKey)
        {
            throw new NotImplementedException();
        }

        public override T[] Select<T>(string searchFieldName, object matchValue)
        {
            throw new NotImplementedException();
        }

        public override void Update(object item)
        {
            throw new NotImplementedException();
        }

        public override void Delete(object item)
        {
            throw new NotImplementedException();
        }

        public override void Delete<T>(object primaryKey)
        {
            throw new NotImplementedException();
        }

        public override void FillReferences(object instance)
        {
            throw new NotImplementedException();
        }

        public override T[] Fetch<T>(int fetchCount)
        {
            throw new NotImplementedException();
        }

        public override T[] Fetch<T>(int fetchCount, int firstRowOffset)
        {
            throw new NotImplementedException();
        }

        public override T[] Fetch<T>(string searchFieldName, int fetchCount, int firstRowOffset)
        {
            throw new NotImplementedException();
        }

        public override int Count<T>()
        {
            throw new NotImplementedException();
        }

        public override void Delete<T>()
        {
            throw new NotImplementedException();
        }

        public override void Delete<T>(string fieldName, object matchValue)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
