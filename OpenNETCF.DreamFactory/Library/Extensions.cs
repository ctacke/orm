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
    internal static class Extensions
    {
        public static string ToDreamFactoryDB_TypeName(this DbType t)
        {
            switch (t)
            {
                case DbType.Byte:
                    return "tinyint";
                case DbType.Int32:
                    return "int";
                case DbType.String:
                    return "varchar";
                case DbType.Binary:
                    return "varbinary";
                case DbType.Decimal:
                    return "float";
                case DbType.Double:
                    return "decimal";
                case DbType.DateTime:
                    return "datetime";
                case DbType.Date:
                    return "date";
                case DbType.Time:
                    return "time";
                default:
                    throw new ArgumentException(string.Format("Field type '{0} not supported", t.ToString()));
            }
        }

        public static bool IsFalse(this bool? b)
        {
            if (b.HasValue && !b.Value) return true;
            return false;
        }

        public static bool IsTrue(this bool? b)
        {
            if(b.HasValue && b.Value) return true;
            return false;
        }

        public static string ToDreamFactoryTypeName(this Type t)
        {
            var tc = Type.GetTypeCode(t);

            // do a quick type check for supported types
            switch (tc)
            {
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                    return "integer";
                case TypeCode.String:
                    return "string";
                case TypeCode.Boolean:
                    return "boolean";
                case TypeCode.Double:
                case TypeCode.Single:
                    return "float";
                case TypeCode.Decimal:
                    return "decimal";
                case TypeCode.DateTime:
                    return "datetime";
                case TypeCode.Object:
                    if (t.Equals(typeof(TimeSpan)))
                    {
                        return "time";
                    }
                    else if (t.Equals(typeof(byte[])))
                    {
                        return "blob";
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                default:
                    throw new NotSupportedException();
            }
        }

        public static string ToDreamFactoryTypeName(this DbType t)
        {
            switch (t)
            {
                case DbType.Boolean:
                    return "boolean";
                case DbType.Byte:
                    return "byte";
                case DbType.Int32:
                    return "int";
                case DbType.String:
                    return "string";
                case DbType.Binary:
                    return "blob";
                case DbType.Decimal:
                    return "float";
                case DbType.Double:
                case DbType.Single:
                    return "decimal";
                case DbType.DateTime:
                    return "datetime";
                case DbType.Date:
                    return "date";
                case DbType.Time:
                    return "time";
                default:
                    throw new ArgumentException(string.Format("Field type '{0} not supported", t.ToString()));
            }
        }

        public static FieldDescriptor AsFieldDescriptor(this Field f)
        {
            var fd = new FieldDescriptor()
            {
                name = f.Name,
                label = f.Label,
                type = f.TypeName
            };

            if (f.IsRequired.IsTrue())
            {
                fd.required = true;
            }

            if (f.AllowsNull.IsFalse())
            {
                fd.allow_null = false;
            }

            if (f.Length.HasValue)
            {
                fd.length = f.Length.Value;
            }

            if (f.Precision.HasValue)
            {
                fd.precision = f.Precision.Value;
            }

            if (f.Scale.HasValue)
            {
                fd.scale = f.Scale.Value;
            }

            if (f.AutoIncrement.IsTrue())
            {
                fd.auto_increment = f.AutoIncrement.Value;
            }

            return fd;
        }

        public static Field AsField(this FieldDescriptor f)
        {
            Field field;

            switch (f.type)
            {
                case "id":
                    field = new Field<int>(f.name, f.label) { IsPrimaryKey = true };
                    break;
                case "integer":
                    field = new Field<int>(f.name, f.label);
                    break;
                case "string":
                case "text":
                    field = new Field<string>(f.name, f.label);
                    break;
                case "boolean":
                    field = new Field<bool>(f.name, f.label);
                    break;
                case "binary":
                case "blob":
                    field = new Field<byte[]>(f.name, f.label);
                    break;
                case "float":
                    field = new Field<float>(f.name, f.label);
                    break;
                case "decimal":
                    field = new Field<decimal>(f.name, f.label);
                    break;
                case "double":
                    field = new Field<double>(f.name, f.label);
                    break;
                case "datetime":
                case "date":
                    field = new Field<DateTime>(f.name, f.label);
                    break;
                case "time":
                case "timestamp":
                case "timestamp_on_update":
                    field = new Field<TimeSpan>(f.name, f.label);
                    break;
                case "reference":
                    field = new Field<int>(f.name, f.label);
                    // TODO: handle reference field
                    break;
                default:
                    if (Debugger.IsAttached) Debugger.Break();
                    throw new NotSupportedException();
            }

            // populate other props
            if (f.auto_increment.IsTrue())
            {
                field.AutoIncrement = true;
            }

            if (f.allow_null.IsFalse())
            {
                field.AllowsNull = false;
            }

            field.Precision = f.precision;
            field.Scale = f.scale;
            field.Length = f.length;
            field.IsRequired = f.required;

            return field;
        }
    }
}
