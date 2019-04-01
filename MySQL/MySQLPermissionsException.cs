using System;

namespace OpenNETCF.ORM
{
    public sealed class MySQLPermissionsException : Exception
    {
        internal MySQLPermissionsException(string message)
            : base(message)
        {
        }
    }
}
