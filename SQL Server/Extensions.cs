using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace OpenNETCF.ORM.SqlServer
{
    internal static class Extensions
    {
        public static SqlDbType ToSqlDbType(this DbType sourceType)
        {
            switch (sourceType)
            {
                case DbType.Int32:
                case DbType.UInt32:
                    return SqlDbType.Int;
                case DbType.Int64:
                case DbType.UInt64:
                    return SqlDbType.BigInt;
                case DbType.Int16:
                case DbType.UInt16:
                    return SqlDbType.SmallInt;
                case DbType.Boolean:
                    return SqlDbType.Bit;
                case DbType.String:
                case DbType.StringFixedLength:
                    return SqlDbType.NVarChar;
                case DbType.Single:
                case DbType.Decimal:
                    return SqlDbType.Decimal;
                case DbType.Double:
                    return SqlDbType.Float;
                case DbType.DateTime2:
                case DbType.Date:
                case DbType.DateTime:
                    return SqlDbType.DateTime;
                case DbType.Time:
                    return SqlDbType.Time;
                default:
                    throw new NotSupportedException();
            }
        }

        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }
}
