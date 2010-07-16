using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace OpenNETCF.ORM
{
    public class SqlCeEntityInfo : EntityInfo
    {
        public SqlCeEntityInfo()
        {
            PrimaryKeyIndexName = null;
        }

        internal string PrimaryKeyIndexName { get; set; }
        internal List<string> IndexNames { get; set; }
    }
}
