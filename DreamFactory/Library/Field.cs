using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using OpenNETCF.Web;
using RestSharp;

namespace OpenNETCF.DreamFactory
{
    public class Field<T> : Field
    {
        private bool? m_isPK;
        private bool? m_autoincrement;

        public Field(string name)
            : this(name, name)
        {
        }

        public Field(string name, string label)
            : base(name, label)
        {
            ResetTypeName();
        }

        private void ResetTypeName()
        {
            var type = typeof(T);
            if (this.IsPrimaryKey.IsTrue())
            {
                TypeName = "id";
            }
            else
            {
                TypeName = type.ToDreamFactoryTypeName();
            }
        }

        public override bool? IsPrimaryKey
        {
            get { return m_isPK; }
            set
            {
                if ((value.IsTrue()) && (!typeof(T).Equals(typeof(int))))
                {
                    throw new NotSupportedException("PK must be a Field<int>");
                }

                m_isPK = value;
                ResetTypeName();
            }
        }

        public override bool? AutoIncrement
        {
            get { return m_isPK; }
            set
            {
                if ((value.IsTrue()) && (!typeof(T).Equals(typeof(int))))
                {
                    throw new NotSupportedException("Auto-increment must be a Field<int>");
                }

                m_autoincrement = value;
                ResetTypeName();
            }
        }
    }

    public abstract class Field
    {
        public string Name { get; protected set; }
        public string Label { get; protected set; }
        public string TypeName { get; protected set; }
//        public bool? IsForeignKey { get; set; }
        public bool? IsRequired { get; set; }
        public bool? AllowsNull { get; set; }
//        public bool? SupportsMultibyte { get; set; }
//        public bool? IsFixedLength { get; set; }
        public int? Length { get; set; }
        public int? Precision { get; set; }
        public int? Scale { get; set; }
//        public string Validation { get; set; }

        public abstract bool? IsPrimaryKey { get; set; }
        public abstract bool? AutoIncrement { get; set; }

        internal Field(string name, string label)
        {
            Name = name;
            Label = label;
        }

        //internal Field(FieldDescriptor descriptor)
        //{
        //    Name = descriptor.name;
        //    Precision = descriptor.precision;
        //    IsPrimaryKey = descriptor.is_primary_key;
        //    IsForeignKey = descriptor.is_foreign_key;
        //    AllowsNull = descriptor.allow_null;
        //    AutoIncrement = descriptor.auto_increment;
        //    IsFixedLength = descriptor.fixed_length;
        //    Label = descriptor.label;
        //    Length = descriptor.length;
        //    IsRequired = descriptor.required;
        //    Scale = descriptor.scale;
        //    SupportsMultibyte = descriptor.supports_multibyte;
        //    Validation = descriptor.validation;

        //    // descriptor.ref_fields;
        //    // descriptor.ref_table;

        //    // get "root" type name
        //    var typename = descriptor.db_type;
        //    var index = typename.IndexOf('(');
        //    if (index > 0)
        //    {
        //        typename = typename.Substring(0, index);
        //    }
        //    switch (typename)
        //    {
        //        case "tinyint":
        //            DataType = DbType.Byte;
        //            break;
        //        case "int":
        //            DataType = DbType.Int32;
        //            break;
        //        case "text":
        //            // uncertain if this is right
        //        case "varchar":
        //            DataType = DbType.String;
        //            break;
        //        case "varbinary":
        //        case "blob":
        //            DataType = DbType.Binary;
        //            break;
        //        case "float":
        //            DataType = DbType.Decimal;
        //            break;
        //        case "decimal":
        //            DataType = DbType.Double;
        //            break;
        //        case "datetime":
        //            DataType = DbType.DateTime;
        //            break;
        //        case "date":
        //            DataType = DbType.Date;
        //            break;
        //        case "time":
        //            DataType = DbType.Time;
        //            break;
        //        default:
        //            throw new NotSupportedException();
        //    }
        //}

    }
}
