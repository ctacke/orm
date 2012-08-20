using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace OpenNETCF.ORM
{
    class SQLCEValidator : DataStoreValidator
    {
        private const string StorePath = "\\SQLCETestStore.sdf";

        protected override IDataStore CreateStoreFile()
        {
            return new SqlCeDataStore(StorePath);
        }
    }
}
