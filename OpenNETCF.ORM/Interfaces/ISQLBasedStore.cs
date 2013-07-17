using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace OpenNETCF.ORM
{
    public interface ISQLBasedStore : IDataStore
    {
        ConnectionBehavior ConnectionBehavior { get; set; }

        int ExecuteNonQuery(string sql, bool throwExceptions);
        int ExecuteNonQuery(string sql);
        object ExecuteScalar(string sql);
        IDataReader ExecuteReader(string sql);
        IDataReader ExecuteReader(string sql, bool throwExceptions);
        IDataReader ExecuteReader(string sql, IEnumerable<IDataParameter> parameters);
        IDataReader ExecuteReader(string sql, IEnumerable<IDataParameter> parameters, bool throwExceptions);
        IDataReader ExecuteReader(string sql, IEnumerable<IDataParameter> parameters, CommandBehavior behavior, bool throwExceptions);
    }
}
