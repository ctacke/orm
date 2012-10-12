using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenNETCF.ORM
{
    public interface ITableBasedStore
    {
        bool TableExists(string tableName);
        void TruncateTable(string tableName);
        void DropTable(string tableName);
    }
}
