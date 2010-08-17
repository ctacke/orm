using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace OpenNETCF.ORM
{
    public class SqlCeEntityInfo : EntityInfo
    {
        private int m_pkOrdinal = -1;

        public SqlCeEntityInfo()
        {
            PrimaryKeyIndexName = null;
        }

        internal string PrimaryKeyIndexName { get; set; }
        internal List<string> IndexNames { get; set; }

        internal int PrimaryKeyOrdinal
        {
            get
            {
                if (m_pkOrdinal < 0)
                {
                    m_pkOrdinal = this.Fields.First(f => f.IsPrimaryKey).Ordinal;
                }
                return m_pkOrdinal;
            }
        }

    }
}
