using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Reflection;

namespace OpenNETCF.ORM
{
    public enum FieldSearchOrder
    {
        NotSearchable = 0,
        Ascending = 1,
        Descending = 2
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class FieldAttribute : Attribute
    {
        private DbType m_type;

        public FieldAttribute()
        {
            // set up defaults
            AllowsNulls = true;
            IsPrimaryKey = false;
            SearchOrder = FieldSearchOrder.NotSearchable;
            RequireUniqueValue = false;
            Ordinal = -1;
        }

        public string FieldName { get; set; }
        public int Length { get; set; }
        public int Precision { get; set; }
        public int Scale { get; set; }
        public bool AllowsNulls { get; set; }
        public bool IsPrimaryKey { get; set; }
        public bool RequireUniqueValue { get; set; }
        public int Ordinal { get; set; }
        public FieldSearchOrder SearchOrder { get; set; }
 
        public PropertyInfo PropertyInfo { get; internal set; }
        internal bool DataTypeIsValid { get; private set; }

        public DbType DataType 
        {
            get { return m_type; }
            set
            {
                m_type = value;
                DataTypeIsValid = true;
            }
        }

        public override string ToString()
        {
            return FieldName;
        }
    }
}
