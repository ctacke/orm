using System;
using System.Net;

namespace OpenNETCF.ORM
{
    public class SQLiteException : Exception
    {
        public SQLiteException(string message)
            : base(message)
        {
        }
    }
}
