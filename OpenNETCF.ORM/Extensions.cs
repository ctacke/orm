using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Data;

namespace OpenNETCF.ORM
{
    public static class Extensions
    {
        public static T[] ConvertAll<T>(this Array input)
        {
            var output = new T[input.Length];

            for (int i = 0; i < output.Length; i++)
            {
                output[i] = (T)input.GetValue(i);
            }

            return output;
        }

        public static object[] ConvertAll(this List<object> input, Type targetType)
        {
            var output = new object[input.Count];

            for (int i = 0; i < output.Length; i++)
            {
                output[i] = Convert.ChangeType(input[i], targetType, null);
            }

            return output;
        }

        internal static bool IsNullable(this Type type)
        {
            if (!type.IsGenericType) return false;
            if (type.Name == "Nullable`1") return true;

            return false;
        }

        public static Type ToManagedType(this DbType type)
        {
            return type.ToManagedType(false);
        }

        public static Type ToManagedType(this DbType type, bool isNullable)
        {
            switch (type)
            {
                case DbType.AnsiString:
                case DbType.AnsiStringFixedLength:
                case DbType.String:
                case DbType.StringFixedLength:
                    return typeof(string);
                case DbType.Boolean:
                    return isNullable ? typeof(bool?) : typeof(bool);
                case DbType.Int16:
                    return isNullable ? typeof(short?) : typeof(short);
                case DbType.UInt16:
                    return isNullable ? typeof(ushort?) : typeof(ushort);
                case DbType.Int32:
                    return isNullable ? typeof(int?) : typeof(int);
                case DbType.UInt32:
                    return isNullable ? typeof(uint?) : typeof(uint);
                case DbType.DateTime:
                    return isNullable ? typeof(DateTime?) : typeof(DateTime);
                case DbType.Decimal:
                    return isNullable ? typeof(decimal?) : typeof(decimal);
                case DbType.Single:
                    return isNullable ? typeof(float?) : typeof(float);
                case DbType.Double:
                    return isNullable ? typeof(double?) : typeof(double);
                case DbType.Int64:
                    return isNullable ? typeof(long?) : typeof(long);
                case DbType.UInt64:
                    return isNullable ? typeof(ulong?) : typeof(ulong);
                case DbType.Byte:
                    return isNullable ? typeof(byte?) : typeof(byte);
                case DbType.Guid:
                    return isNullable ? typeof(Guid?) : typeof(Guid);
                case DbType.Binary:
                    return typeof(byte[]);
                default:
                    throw new NotSupportedException();
            }
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
                case "System.TimeSpan":
                    return DbType.Int64;

                case "System.Single":
                    return DbType.Single;

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

        public static DbType ParseToDbType(this string dbTypeName)
        {
            switch (dbTypeName.ToLower())
            {
                case "datetime":
                    return DbType.DateTime;
                case "bigint":
                    return DbType.Int64;
                case "int":
                    return DbType.Int32;
                case "smallint":
                    return DbType.Int16;
                case "ntext":
                case "nvarchar":
                    return DbType.String;
                case "nchar":
                    return DbType.StringFixedLength;
                case "bit":
                    return DbType.Boolean;
                case "tinyint":
                    return DbType.Byte;
                case "numeric":
                    return DbType.Decimal;
                case "real":
                    return DbType.Single;
                case "float":
                    return DbType.Double;
                case "uniqueidentifier":
                    return DbType.Guid;
                case "image":
                case "binary":
                    return DbType.Binary;
                default:
                    throw new NotSupportedException(
                        string.Format("Unable to determine convert string '{0}' to DbType", dbTypeName));
            }
        }

        public static string ToSqlTypeString(this DbType type)
        {
            switch (type)
            {
                case DbType.DateTime:
                    return "datetime";
                case DbType.Time:
                case DbType.Int64:
                case DbType.UInt64:
                    return "bigint";
                case DbType.Int32:
                case DbType.UInt32:
                    return "integer";
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
                case DbType.Byte:
                    return "tinyint";
                case DbType.Decimal:
                    return "numeric";
                case DbType.Single:
                    return "real";
                case DbType.Double:
                    return "float";
                case DbType.Guid:
                    return "uniqueidentifier";
                case DbType.Binary:
                    return "image";
                default:
                    throw new NotSupportedException(
                        string.Format("Unable to determine convert DbType '{0}' to string", type.ToString()));
            }
        }

        public static bool UnderlyingTypeIs<T>(this Type checkType)
        {
            if ((checkType.IsGenericType) && (checkType.GetGenericTypeDefinition().Equals(typeof(Nullable<>))))
            {
                return Nullable.GetUnderlyingType(checkType).Equals(typeof(T));
            }
            else
            {
                return checkType.Equals(typeof(T));
            }
        }
    }
}
