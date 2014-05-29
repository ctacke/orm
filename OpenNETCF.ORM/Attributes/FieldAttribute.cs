using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Reflection;

namespace OpenNETCF.ORM
{
    [AttributeUsage(AttributeTargets.Property)]
    public class FieldAttribute : Attribute, ICloneable
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
            IsRowVersion = false;
        }

        public FieldAttribute(string name, DbType type)
            : this(name, type, false)
        {
        }

        public FieldAttribute(string name, DbType type, bool isPrimaryKey)
            : this()
        {
            this.FieldName = name;
            this.DataType = type;
            this.IsPrimaryKey = isPrimaryKey;
        }

        public object Clone()
        {
            return new FieldAttribute(FieldName, DataType, IsPrimaryKey)
            {
                Length = this.Length,
                Precision = this.Precision,
                Scale = this.Scale,
                AllowsNulls = this.AllowsNulls,
                RequireUniqueValue = this.RequireUniqueValue,
                Ordinal = this.Ordinal,
                SearchOrder = this.SearchOrder,
                DefaultType = this.DefaultType,
                IsRowVersion = this.IsRowVersion,
                PropertyInfo = this.PropertyInfo, // this might not be valid
                DataTypeIsValid = this.DataTypeIsValid
            };
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

        public DefaultType DefaultType { get; set; }
        public object DefaultValue { get; set; }
        
        private bool? m_isTimeSpan;

        public bool IsTimespan
        {
            get 
            {
                if(!m_isTimeSpan.HasValue)
                {
                    m_isTimeSpan = PropertyInfo.PropertyType.UnderlyingTypeIs<TimeSpan>();
                }
                return m_isTimeSpan.Value;
            }
        }

        /// <summary>
        /// rowversion or timestamp time for Sql Server
        /// </summary>
        public bool IsRowVersion { get; set; }
 
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
