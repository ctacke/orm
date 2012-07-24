
#if WINDOWS_PHONE

namespace System.Data.Common
{
    public interface IDbCommand : IDisposable
    {
        string CommandText { get; set; }
        IDbConnection Connection { set; }
        CommandType CommandType { get; set; }
        DbParameterCollection Parameters { get; }

        int ExecuteNonQuery();
        object ExecuteScalar();
        DbDataReader ExecuteReader();
    }

    public interface DbCommand : IDbCommand
    {
    }
}

#endif
