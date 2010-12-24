using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace OpenNETCF.ORM
{
    public class SqlEntityInfo : EntityInfo
    {
        private int m_pkOrdinal = -1;

        public SqlEntityInfo()
        {
            PrimaryKeyIndexName = null;
        }

        public string PrimaryKeyIndexName { get; set; }
        public List<string> IndexNames { get; set; }

        public int PrimaryKeyOrdinal
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
