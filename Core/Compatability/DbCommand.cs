using System;
using System.Net;

namespace System.Data.Common
{
#if WINDOWS_PHONE 
    public interface IDbCommand : IDisposable
    {
        Mono.Data.Sqlite.SqliteConnection
        string CommandText { get; set; }
        int CommandTimeout { get; set; }
        CommandType CommandType { get; set; }
        IDbConnection Connection { get; set; }
        IDataParameterCollection Parameters { get; }
        IDbTransaction Transaction { get; set; }
        UpdateRowSource UpdatedRowSource { get; set; }
        void Cancel();
        IDbDataParameter CreateParameter();
        int ExecuteNonQuery();
        IDataReader ExecuteReader();
        IDataReader ExecuteReader(CommandBehavior behavior);
        object ExecuteScalar();
        void Prepare();
    }

#endif
}
