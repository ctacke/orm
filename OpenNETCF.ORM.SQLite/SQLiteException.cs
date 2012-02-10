using System;
using System.Net;

namespace OpenNETCF.ORM.SQLite
{
    public class SQLiteException : Exception
    {
        public SQLiteException(string message)
            : base(message)
        {
        }
    }
}
