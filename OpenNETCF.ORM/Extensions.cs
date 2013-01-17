using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Security.Cryptography;

namespace OpenNETCF.ORM
{
    public class SqlIndexInfo
    {
        public string IndexName { get; set; }
        public string TableName { get; set; }
        public string[] Fields { get; set; }
        public FieldSearchOrder SearchOrder { get; set; }
        public bool IsUnique { get; set; }

        public bool IsComposite
        {
            get { return Fields.Length > 1; }
        }
    }

    public static class Extensions
    {
        public static SqlIndexInfo ParseToIndexInfo(this string sql)
        {
            sql = sql.Replace("\n", string.Empty).Replace("\r", string.Empty).Replace("\t", string.Empty).Trim();
            var info = new SqlIndexInfo();

            var i = sql.IndexOf("CREATE INDEX", 0, StringComparison.InvariantCultureIgnoreCase);
            if (i < 0)
            {
                i = sql.IndexOf("CREATE UNIQUE INDEX", 0, StringComparison.InvariantCultureIgnoreCase);
                info.IsUnique = true;
                if (i < 0)
                {
                    throw new ArgumentException("String is not valid CREATE INDEX SQL");
                }
            }
            var indexNameStart = i + "CREATE INDEX".Length + 1;

            i = sql.IndexOf(" ON ", 0, StringComparison.InvariantCultureIgnoreCase);
            if (i < 0) throw new ArgumentException("String is not valid CREATE INDEX SQL");
            var indexNameEnd = i;
            var tableNameStart = i + " ON ".Length;

            i = sql.IndexOf("(", 0, StringComparison.InvariantCultureIgnoreCase);
            if (i < 0) throw new ArgumentException("String is not valid CREATE INDEX SQL");
            var tableNameEnd = i;
            var fieldNamesStart = i + 1;

            i = sql.IndexOf(")", 0, StringComparison.InvariantCultureIgnoreCase);
            if (i < 0) throw new ArgumentException("String is not valid CREATE INDEX SQL");
            var fieldNamesEnd = i;

            info.IndexName = sql.Substring(indexNameStart, indexNameEnd - indexNameStart).Trim(' ', '[', ']');
            info.TableName = sql.Substring(tableNameStart, tableNameEnd - tableNameStart).Trim(' ', '[', ']');
            var tempfields = (from f in sql.Substring(fieldNamesStart, fieldNamesEnd - fieldNamesStart).Split(',')
                          where !string.IsNullOrEmpty(f)
                          select f.Replace("[", string.Empty).Replace("]", string.Empty).Trim());

            var fields = new List<string>();
            // look for ordering
            foreach(var f in tempfields)
            {
                if (f.Contains(' '))
                {
                    if (f.IndexOf("ASC", 0, StringComparison.InvariantCultureIgnoreCase) >= 0)
                    {
                        info.SearchOrder = FieldSearchOrder.Ascending;
                    }
                    else
                    {
                        info.SearchOrder = FieldSearchOrder.Descending;
                    }
                    fields.Add(f.Substring(0, f.IndexOf(' ')));
                }
                else
                {
                    fields.Add(f);
                }
            }

            info.Fields = fields.ToArray();

            return info;
        }

        public static T[] ConvertAll<T>(this Array input)
        {
            var output = new T[input.Length];

            for (int i = 0; i < output.Length; i++)
            {
                output[i] = (T)input.GetValue(i);
            }

            return output;
        }

        public static Array ConvertAll(this List<object> input, Type targetType)
        {
            var output = Array.CreateInstance(targetType, input.Count);
            
            for (int i = 0; i < output.Length; i++)
            {
                output.SetValue(Convert.ChangeType(input[i], targetType, null), i);
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
            return ParseToDbType(dbTypeName, false);
        }

        // SQLite uses 'integer' for 64-bit, SQL COmpact uses it for 32-bit
        public static DbType ParseToDbType(this string dbTypeName, bool integerIs64bit)
        {
            var test = dbTypeName.ToLower();

            switch (test)
            {
                case "datetime":
                    return DbType.DateTime;
                case "bigint":
                case "rowversion":
                    return DbType.Int64;
                case "int":
                case "integer":
                    if (integerIs64bit)
                    {
                        return DbType.Int64;
                    }
                    return DbType.Int32;
                case "smallint":
                    return DbType.Int16;
                case "string":
                case "ntext":
                case "nvarchar":
                case "varchar":
                case "text":
                    return DbType.String;
                case "nchar":
                    return DbType.StringFixedLength;
                case "bit":
                    return DbType.Boolean;
                case "tinyint":
                    return DbType.Byte;
                case "numeric":
                case "money":
                    return DbType.Decimal;
                case "real":
                    return DbType.Single;
                case "float":
                    return DbType.Double;
                case "uniqueidentifier":
                    return DbType.Guid;
                case "image":
                case "binary":
                case "varbinary":
                case "blob":
                    return DbType.Binary;
                default:
                    // if case it has a length suffix
                    if (test.StartsWith("nvarchar") || test.StartsWith("nchar"))
                    {
                        return DbType.StringFixedLength;
                    }

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

        public static void BulkInsert(this IDataStore store, IEnumerable<object> items)
        {
            store.BulkInsert(items, false);
        }

        public static void BulkInsert(this IDataStore store, IEnumerable<object> items, bool insertReferences)
        {
            foreach (var i in items)
            {
                store.Insert(i, insertReferences);
            }
        }

        public static void CreateOrUpdateStore(this IDataStore store)
        {
            if(store.StoreExists)
            {
                store.EnsureCompatibility();
            }
            else
            {
                store.CreateStore();
            }
        }

        public static string GenerateHash(this ReferenceAttribute r)
        {
            var hash = string.Format("{0}{1}{2}", r.PropertyInfo.Name, r.ReferenceEntityType.Name, r.ReferenceField);
            return hash;
        }
    }
}
