using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using OpenNETCF.ORM;

namespace OpenNETCF.ORM
{
    class SQLiteValidator : DataStoreValidator
    {
        private const string StorePath = "\\SQLiteTestStore.sdf";

        protected override IDataStore CreateStoreFile()
        {
            return new SQLiteDataStore(StorePath);
        }
    }
}
