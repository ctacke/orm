using System;
using System.Net;

namespace System.Data.Common
{
#if WINDOWS_PHONE
    public interface IDbConnection : IDisposable
    {
        string ConnectionString { get; set; }
        int ConnectionTimeout { get; }
        string Database { get; }
        ConnectionState State { get; }
        IDbTransaction BeginTransaction();
        IDbTransaction BeginTransaction(IsolationLevel il);
        void ChangeDatabase(string databaseName);
        void Close();
        IDbCommand CreateCommand();
        void Open();
    }
#endif
}
