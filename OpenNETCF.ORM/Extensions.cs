﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Data;

namespace OpenNETCF.ORM
{
    public static class Extensions
    {
        internal static bool IsNullable(this Type type)
        {
            if (!type.IsGenericType) return false;
            if (type.Name == "Nullable`1") return true;

            return false;
        }

        public static DbType ToDbType(this Type type)
        {
            string typeName = type.FullName;

            if (type.IsNullable())
            {
                typeName = type.GetGenericArguments()[0].FullName;
            }

            switch (typeName)
            {
                case "System.String":
                    return DbType.String;
                case "System.Boolean":
                    return DbType.Boolean;
                case "System.Int16":
                    return DbType.Int16;
                case "System.UInt16":
                    return DbType.UInt16;
                case "System.Int32":
                    return DbType.Int32;
                case "System.UInt32":
                    return DbType.UInt32;
                case "System.DateTime":
                    return DbType.DateTime;

                case "System.Decimal":
                    return DbType.Decimal;
                case "System.Double":
                    return DbType.Double;
                case "System.Int64":
                    return DbType.Int64;
                case "System.UInt64":
                    return DbType.UInt64;
                case "System.Byte":
                    return DbType.Byte;
                case "System.Char":
                    return DbType.Byte;
                case "System.Guid":
                    return DbType.Guid;

                case "System.Byte[]":
                    return DbType.Binary;

                default:
                    if (type.IsEnum)
                    {
                        return DbType.Int32;
                    }

                    // everything else is an "object" and requires a custom serializer/deserializer
                    return DbType.Object;
            }
        }

        public static string ToSqlTypeString(this DbType type)
        {
            switch (type)
            {
                case DbType.DateTime:
                    return "datetime";
                case DbType.Int32:
                case DbType.UInt32:
                    return "int";
                case DbType.Int16:
                case DbType.UInt16:
                    return "smallint";
                case DbType.String:
                    return "nvarchar";
                case DbType.StringFixedLength:
                    return "nchar";
                case DbType.Boolean:
                    return "bit";
                case DbType.Object:
                    return "image";
                case DbType.Int64:
                    return "bigint";
                case DbType.UInt64:
                    return "bigint";
                case DbType.Byte:
                    return "tinyint";

                case DbType.Decimal:
                    return "numeric";
                case DbType.Double:
                    return "float";
                case DbType.Guid:
                    return "uniqueidentifier";

                default:
                    throw new NotSupportedException(
                        string.Format("Unable to determine convert DbType '{0}' to string", type.ToString()));
            }
        }
    }
}
