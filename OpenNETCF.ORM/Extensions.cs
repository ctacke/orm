using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Data;

namespace OpenNETCF.ORM
{
    public static class Extensions
    {
        internal static bool IsSubclassOfRawGeneric(this Type generic, Type toCheck)
        {
            while (toCheck != typeof(object))
            {
                var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (cur.IsGenericType && generic.GetGenericTypeDefinition() == cur.GetGenericTypeDefinition())
                {
                    return true;
                }
                toCheck = toCheck.BaseType;
            }
            return false;
        }
        
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
                default:
                    if (type.IsEnum)
                    {
                        return DbType.Int32;
                    }

                    throw new NotSupportedException(
                        string.Format("Unable to determine DB type for Type '{0}'", type.Name));
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
                default:
                    throw new NotSupportedException(
                        string.Format("Unable to determine convert DbType '{0}' to string", type.ToString()));
            }
        }
    }
}
