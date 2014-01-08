using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using OpenNETCF.DreamFactory;

namespace OpenNETCF.ORM
{
    internal static class FieldFactory
    {
        private const int DefaultStringLength = 256;
        private const int MaxStringLength = 8000;

        public static Field GetFieldForAttribute(FieldAttribute attrib, KeyScheme keyScheme)
        {
            Field field = null;

            switch (attrib.DataType)
            {
                case System.Data.DbType.Int32:
                    field = new Field<int>(attrib.FieldName);
                    if (attrib.IsPrimaryKey)
                    {
                        if (keyScheme == KeyScheme.Identity)
                        {
                            // As of 10/28/2013, if a field is a PK, DreamFactory forces it to be an auto-incrementing number
                            field.IsPrimaryKey = true;
                            field.AutoIncrement = true;
                        }
                    }
                    break;
                case System.Data.DbType.Int64:
                    field = new Field<long>(attrib.FieldName);
                    break;
                case System.Data.DbType.Byte:
                    field = new Field<byte>(attrib.FieldName);
                    break;
                case System.Data.DbType.Int16:
                    field = new Field<short>(attrib.FieldName);
                    break;
                case System.Data.DbType.String:
                case System.Data.DbType.StringFixedLength:
                    field = new Field<string>(attrib.FieldName);
                    if (attrib.Length <= 0)
                    {
                        field.Length = DefaultStringLength;
                    }
                    else if (attrib.Length > MaxStringLength)
                    {
                        field.Length = MaxStringLength;
                    }
                    else
                    {
                        field.Length = attrib.Length;
                    }

                    break;
                case System.Data.DbType.Boolean:
                    field = new Field<bool>(attrib.FieldName);
                    break;
                case System.Data.DbType.Single:
                    field = new Field<float>(attrib.FieldName);
                    field.Scale = attrib.Scale;
                    field.Precision = attrib.Precision;
                    break;
                case System.Data.DbType.Double:
                    field = new Field<double>(attrib.FieldName);
                    field.Scale = attrib.Scale;
                    field.Precision = attrib.Precision;
                    break;
                case System.Data.DbType.Decimal:
                    field = new Field<decimal>(attrib.FieldName);
//                    field.Scale = attrib.Scale;
//                    field.Precision = attrib.Precision;
                    break;
                case System.Data.DbType.DateTime:
                    field = new Field<DateTime>(attrib.FieldName);
                    break;
                case System.Data.DbType.Time:
                    field = new Field<TimeSpan>(attrib.FieldName);
                    break;
                case System.Data.DbType.Guid:
                case System.Data.DbType.Binary:
                    field = new Field<byte[]>(attrib.FieldName);
                    break;
                default:
                    if (Debugger.IsAttached) Debugger.Break();
                    throw new NotSupportedException(string.Format("Field type '{0}' is not supported.", attrib.DataType.ToString()));
            }

            field.AllowsNull = attrib.AllowsNulls;

            return field;
        }
    }
}
