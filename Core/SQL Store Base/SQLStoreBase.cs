using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Threading;

namespace OpenNETCF.ORM
{
    public abstract class SQLStoreBase<TEntityInfo> : DataStore<TEntityInfo>, IDisposable, ITableBasedStore, ISQLBasedStore
        where TEntityInfo : EntityInfo, new()
    {
        protected List<IndexInfo> m_indexNameCache = new List<IndexInfo>();
        private IDbConnection m_connection;
        private ConnectionBehavior m_connectionBehavior;
        private int m_connectionCount = 0;

        private Dictionary<Type, MethodInfo> m_serializerCache = new Dictionary<Type, MethodInfo>();
        private Dictionary<Type, MethodInfo> m_deserializerCache = new Dictionary<Type, MethodInfo>();
        private Dictionary<Type, object[]> m_referenceCache = new Dictionary<Type, object[]>();

        public int DefaultStringFieldSize { get; set; }
        public int DefaultNumericFieldPrecision { get; set; }
        public int DefaultVarBinaryLength { get; set; }
        protected abstract string AutoIncrementFieldIdentifier { get; }

        public abstract override void CreateStore();
        public abstract override void DeleteStore();
        protected abstract void ValidateTable(IDbConnection connection, IEntityInfo entity);

        public abstract override bool StoreExists { get; }

        protected abstract void GetPrimaryKeyInfo(string entityName, out string indexName, out string columnName);

        public abstract override void OnInsert(object item, bool insertReferences);

        protected abstract IEnumerable<object> Select(Type objectType, IEnumerable<FilterCondition> filters, int fetchCount, int firstRowOffset, bool fillReferences);
        public abstract override IEnumerable<DynamicEntity> Select(string entityName);

        public abstract override void OnUpdate(object item, bool cascadeUpdates, string fieldName);

        public abstract override IEnumerable<T> Fetch<T>(int fetchCount, int firstRowOffset, string sortField, FieldSearchOrder sortOrder, FilterCondition filter, bool fillReferences);

        public abstract override int Count<T>(IEnumerable<FilterCondition> filters);

        public abstract string[] GetTableNames();

        protected abstract IDbCommand GetNewCommandObject();
        protected abstract IDbConnection GetNewConnectionObject();
        protected abstract IDataParameter CreateParameterObject(string parameterName, object parameterValue);

        protected IDbTransaction CurrentTransaction { get; set; }

        public abstract string ConnectionString { get; }

        private object m_transactionSyncRoot = new object();
        public int ConnectionPoolSize { get; set; }

        public SQLStoreBase()
        {
            DefaultStringFieldSize = 200;
            DefaultNumericFieldPrecision = 16;
            DefaultVarBinaryLength = 8000;
            m_connectionPool = new List<IDbConnection>();
            ConnectionPoolSize = 20;

            ConnectionBehavior = ORM.ConnectionBehavior.HoldMaintenance;
        }

        ~SQLStoreBase()
        {
            Dispose(false);
        }

        /// <summary>
        /// Derived classes should override this if the underlying engine supports compaction
        /// </summary>
        public virtual void CompactDatabase()
        {
        }

        public int OpenConnectionCount
        {
            get { return m_connectionCount; }
        }

        public ConnectionBehavior ConnectionBehavior
        {
            get { return m_connectionBehavior; }
            set
            {
                if (m_connectionBehavior == value) return;

                lock (m_transactionSyncRoot)
                {
                    if (CurrentTransaction != null)
                    {
                        throw new Exception("You cannot change ConnectionBehavior while a Transaction is pending");
                    }

                    m_connectionBehavior = m_nonTransactionConnectionBehavior = value;
                }
            }
        }

        protected virtual int MaxSizedStringLength
        {
            get { return 4000; }
        }

        protected virtual int MaxSizedBinaryLength
        {
            get { return 8000; }
        }

        protected virtual string ParameterPrefix
        {
            get { return "@"; }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    if (m_connection != null)
                    {
                        if (m_connection.State == ConnectionState.Open)
                        {
                            m_connection.Close();
                        }

                        m_connection.Dispose();

                        m_connection = null;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                if (Debugger.IsAttached) Debugger.Break();
            }
        }

        protected virtual string DefaultDateGenerator
        {
            get { return "GETDATE()"; }
        }

        protected virtual IDbConnection GetConnection(bool maintenance)
        {
            return GetConnection(maintenance, false);
        }

        private List<IDbConnection> m_connectionPool;
        private EventInfo m_disposedEvent;

        private IDbConnection GetPoolConnection()
        {
            lock(m_connectionPool)
            {
                IDbConnection connection = null;

                do
                {
                    connection = (from c in m_connectionPool
                                  where c.State != ConnectionState.Executing
                                  && c.State != ConnectionState.Fetching
                                  select c).FirstOrDefault();

                    if (connection != null)
                    {
                        return connection;
                    }

                    if (m_connectionPool.Count < ConnectionPoolSize)
                    {
                        connection = GetNewConnectionObject();

                        if (m_disposedEvent == null)
                        {
                            m_disposedEvent = connection.GetType().GetEvent("Disposed");

                            if (m_disposedEvent != null)
                            {
                                var target = this.GetType().GetMethod("ConnectionDisposed", BindingFlags.Instance | BindingFlags.NonPublic);

                                // while these objects (event and target method) exist in the CF, the handler never gets called.
                                // not sure if the CF just never raises the Disposed event on an IDbConnection or not
                                m_disposedEvent.AddEventHandler(connection,
                                    Delegate.CreateDelegate(m_disposedEvent.EventHandlerType, this, target)
                                    );
                            }
                        }

                        connection.Open();
                        m_connectionPool.Add(connection);
                        Interlocked.Increment(ref m_connectionCount);
                        Debug.WriteLine("Creating pooled connection");
                        return connection;
                    }

                    // pool is full, we have to wait
                    Thread.Sleep(1000);

                    // TODO: add a timeout?
                } while (connection == null);

                // this should never happen
                return null;
            }
        }

        private void ConnectionDisposed(object sender, EventArgs e)
        {
            var c = sender as IDbConnection;
            if (c != null)
            {
                m_connectionPool.Remove(c);
            }
        }

        private IDbConnection GetConnection(bool maintenance, bool isRetry)
        {
            IDbConnection result;
            
            switch (ConnectionBehavior)
            {
                case ConnectionBehavior.AlwaysNew:
                    var connection = GetNewConnectionObject();
                    connection.Open();
                    Interlocked.Increment(ref m_connectionCount);
                    result = connection;
                    break;
                case ConnectionBehavior.HoldMaintenance:
                    if (m_connection == null)
                    {
                        m_connection = GetNewConnectionObject();
                        m_connection.Open();
                        OnPersistentConnectionCreated(m_connection);
                        Interlocked.Increment(ref m_connectionCount);
                    }
                    if (maintenance)
                    {
                        while((m_connection.State == ConnectionState.Executing) 
                            || (m_connection.State == ConnectionState.Fetching))
                        {
                            Thread.Sleep(1000);
                        }
                        return m_connection;
                    }
                    var connection2 = GetPoolConnection();
                    Interlocked.Increment(ref m_connectionCount);
                    result = connection2;
                    break;
                case ConnectionBehavior.Persistent:
                    var pooledConnection = GetPoolConnection();
                    result = pooledConnection;
                    break;
                default:
                    throw new NotSupportedException();
            }

            // make sure the connection is open (in the event we has some network condition that closed it, etc.)
            if (result.State != ConnectionState.Open)
            {
                try
                {
                    result.Open();
                }
                catch
                {
                    if (Environment.OSVersion.Platform == PlatformID.WinCE)
                    {
                        // The CF doesn't appear to actually raise the Disposed event on the IDbCOnnection.  No idea why.
                        // This is a work-around for that behavior.
                        m_connectionPool.Remove(result);
                    }
                    else if (isRetry)
                    {
                        throw;
                    }
                    else
                    {
                        result.Dispose();
                        result = null;
                    }

                    // retry once
                    Thread.Sleep(1000);
                    return GetConnection(maintenance, true);
                }
            }

            return result;
        }

        protected virtual void OnPersistentConnectionCreated(IDbConnection connection) { }

        protected void ReleasePersistentConnection()
        {
            if (m_connection == null) return;
            try
            {
                var disp = m_connection as IDisposable;
                if (disp != null)
                {
                    // set the global ref to null to prevent recursion
                    m_connection = null;
                    try
                    {
                        // make sure it's disposed
                        disp.Dispose();
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                }
            }
            finally
            {
                m_connection = null;
            }
        }


        protected virtual void DoneWithConnection(IDbConnection connection, bool maintenance)
        {
            switch (ConnectionBehavior)
            {
                case ConnectionBehavior.AlwaysNew:
                    connection.Close();
                    connection.Dispose();
                    connection = null;
                    Interlocked.Decrement(ref m_connectionCount);
                    break;
                case ConnectionBehavior.HoldMaintenance:
                    if (maintenance) return;
                    connection.Close();
                    connection.Dispose();
                    connection = null;
                    Interlocked.Decrement(ref m_connectionCount);
                    break;
                case ConnectionBehavior.Persistent:
                    return;
                default:
                    throw new NotSupportedException();
            }
        }

        public int ExecuteNonQuery(string sql)
        {
            return ExecuteNonQuery(sql, false);
        }

        public int ExecuteNonQuery(string sql, bool throwExceptions)
        {
            var connection = GetConnection(false);
            try
            {
                using (var command = GetNewCommandObject())
                {
                    command.CommandText = sql;
                    command.Connection = connection;
                    command.Transaction = CurrentTransaction;
                    return command.ExecuteNonQuery();
                }
            }
            catch(Exception ex)
            {
                if (throwExceptions) throw;

                Debug.WriteLine("SQLStoreBase::ExecuteNonQuery threw: " + ex.Message);
                return 0;
            }
            finally
            {
                DoneWithConnection(connection, false);
            }
        }

        public object ExecuteScalar(string sql)
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Unix:
                    return ExecuteScalarSimulated(sql);
                default:
                    return ExecuteScalarActual(sql);
            }
        }

        private object ExecuteScalarActual(string sql)
        {
            var connection = GetConnection(false);
            try
            {
                using (var command = GetNewCommandObject())
                {
                    command.Connection = connection;
                    command.CommandText = sql;
                    command.Transaction = CurrentTransaction;
                    return command.ExecuteScalar();
                }
            }
            finally
            {
                DoneWithConnection(connection, false);
            }
        }

        /// <summary>
        /// This is a "simulation" of ExecuteScalar, which is necessary because an actual ExecuteScaler in Mono will fail
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        private object ExecuteScalarSimulated(string sql)
        {
            var connection = GetConnection(false);
            try
            {
                object result = null;

                using (var command = GetNewCommandObject())
                {
                    command.Connection = connection;
                    command.CommandText = sql;
                    command.Transaction = CurrentTransaction;
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            if (reader.FieldCount > 0)
                            {
                                return reader[0];
                            }
                        }
                    }
                }

                return result;
            }
            finally
            {
                DoneWithConnection(connection, false);
            }
        }

        protected virtual string[] ReservedWords
        {
            get { return m_sqlReserved; }
        }

        private static string[] m_sqlReserved = new string[]
        {
            "IDENTITY" ,"ENCRYPTION" ,"ORDER" ,"ADD" ,"END" ,"OUTER" ,"ALL" ,"ERRLVL" ,"OVER" ,"ALTER" ,"ESCAPE" ,"PERCENT" ,"AND" ,"EXCEPT" ,"PLAN" ,"ANY" ,"EXEC" ,"PRECISION" ,"AS" ,"EXECUTE" ,"PRIMARY" ,"ASC",
            "EXISTS" ,"PRINT" ,"AUTHORIZATION" ,"EXIT" ,"PROC" ,"AVG" ,"EXPRESSION" ,"PROCEDURE" ,"BACKUP" ,"FETCH" ,"PUBLIC" ,"BEGIN" ,"FILE" ,"RAISERROR" ,"BETWEEN" ,"FILLFACTOR" ,"READ" ,"BREAK" ,"FOR" ,"READTEXT",
            "BROWSE" ,"FOREIGN" ,"RECONFIGURE" ,"BULK" ,"FREETEXT" ,"REFERENCES" ,"BY" ,"FREETEXTTABLE" ,"REPLICATION" ,"CASCADE" ,"FROM" ,"RESTORE" ,"CASE" ,"FULL" ,"RESTRICT" ,"CHECK" ,"FUNCTION" ,"RETURN" ,"CHECKPOINT",
            "GOTO" ,"REVOKE" ,"CLOSE" ,"GRANT" ,"RIGHT" ,"CLUSTERED" ,"GROUP" ,"ROLLBACK" ,"COALESCE" ,"HAVING" ,"ROWCOUNT" ,"COLLATE" ,"HOLDLOCK" ,"ROWGUIDCOL" ,"COLUMN" ,"IDENTITY" ,"RULE",
            "COMMIT" ,"IDENTITY_INSERT" ,"SAVE" ,"COMPUTE" ,"IDENTITYCOL" ,"SCHEMA" ,"CONSTRAINT" ,"IF" ,"SELECT" ,"CONTAINS" ,"IN" ,"SESSION_USER" ,"CONTAINSTABLE" ,"INDEX" ,"SET" ,"CONTINUE" ,"INNER" ,"SETUSER",
            "CONVERT" ,"INSERT" ,"SHUTDOWN" ,"COUNT" ,"INTERSECT" ,"SOME" ,"CREATE" ,"INTO" ,"STATISTICS" ,"CROSS" ,"IS" ,"SUM" ,"CURRENT" ,"JOIN" ,"SYSTEM_USER" ,"CURRENT_DATE" ,"KEY" ,"TABLE" ,"CURRENT_TIME" ,"KILL",
            "TEXTSIZE" ,"CURRENT_TIMESTAMP" ,"LEFT" ,"THEN" ,"CURRENT_USER" ,"LIKE" ,"TO" ,"CURSOR" ,"LINENO" ,"TOP" ,"DATABASE" ,"LOAD" ,"TRAN" ,"DATABASEPASSWORD" ,"MAX" ,"TRANSACTION" ,"DATEADD" ,"MIN" ,"TRIGGER",
            "DATEDIFF" ,"NATIONAL" ,"TRUNCATE" ,"DATENAME" ,"NOCHECK" ,"TSEQUAL" ,"DATEPART" ,"NONCLUSTERED" ,"UNION" ,"DBCC" ,"NOT" ,"UNIQUE" ,"DEALLOCATE", "NULL", "UPDATE", "DECLARE", "NULLIF", "UPDATETEXT",
            "DEFAULT", "OF", "USE", "DELETE", "OFF", "USER", "DENY", "OFFSETS", "VALUES", "DESC", "ON", "VARYING", "DISK", "OPEN", "VIEW", "DISTINCT", "OPENDATASOURCE", "WAITFOR", "DISTRIBUTED", "OPENQUERY", "WHEN", 
            "DOUBLE", "OPENROWSET", "WHERE", "DROP", "OPENXML", "WHILE", "DUMP", "OPTION", "WITH", "ELSE", "OR", "WRITETEXT" 
        };

        protected virtual void CreateTable(IDbConnection connection, IEntityInfo entity)
        {
            StringBuilder sql = new StringBuilder();

            if (ReservedWords.Contains(entity.EntityName, StringComparer.InvariantCultureIgnoreCase))
            {
                throw new ReservedWordException(entity.EntityName);
            }

            sql.AppendFormat("CREATE TABLE {0} (", entity.EntityName);

            int count = entity.Fields.Count;

            foreach (var field in entity.Fields)
            {
                //if (field is ReferenceFieldAttribute)
                //{
                //    count--;
                //    continue;
                //}

                if (ReservedWords.Contains(field.FieldName, StringComparer.InvariantCultureIgnoreCase))
                {
                    throw new ReservedWordException(field.FieldName);
                }

                sql.AppendFormat("{0} {1} {2}",
                    field.FieldName,
                    GetFieldDataTypeString(entity.EntityName, field),
                    GetFieldCreationAttributes(entity.EntityAttribute, field));

                if (--count > 0) sql.Append(", ");
            }

            sql.Append(")");

            Debug.WriteLine(sql);

            
            using (var command = GetNewCommandObject())
            {
                command.CommandText = sql.ToString();
                command.Connection = connection;
                command.Transaction = CurrentTransaction;
                command.ExecuteNonQuery();
            }

            // create indexes
            foreach (var field in entity.Fields)
            {
                if (field.SearchOrder != FieldSearchOrder.NotSearchable)
                {
                    VerifyIndex(entity.EntityName, field.FieldName, field.SearchOrder, connection);
                }
            }
        }

        protected class IndexInfo
        {
            public IndexInfo()
            {
                MaxCharLength = -1;
            }

            public string Name { get; set; }
            public int MaxCharLength { get; set; }
        }

        protected IndexInfo GetIndexInfo(string indexName)
        {
            return m_indexNameCache.FirstOrDefault(ii => ii.Name == indexName);
        }

        protected virtual string VerifyIndex(string entityName, string fieldName, FieldSearchOrder searchOrder)
        {
            return VerifyIndex(entityName, fieldName, searchOrder, null);
        }

        protected virtual string VerifyIndex(string entityName, string fieldName, FieldSearchOrder searchOrder, IDbConnection connection)
        {
            bool localConnection = false;
            if (connection == null)
            {
                localConnection = true;
                connection = GetConnection(true);
            }
            try
            {
                var indexName = string.Format("ORM_IDX_{0}_{1}_{2}", entityName, fieldName,
                    searchOrder == FieldSearchOrder.Descending ? "DESC" : "ASC");

                if (m_indexNameCache.FirstOrDefault(ii => ii.Name == indexName) != null) return indexName;

                using (var command = GetNewCommandObject())
                {
                    command.Connection = connection;

                    var sql = string.Format("SELECT COUNT(*) FROM information_schema.indexes WHERE INDEX_NAME = '{0}'", indexName);
                    command.CommandText = sql;

                    int i = (int)command.ExecuteScalar();

                    if (i == 0)
                    {
                        sql = string.Format("CREATE INDEX {0} ON [{1}]({2} {3})",
                            indexName,
                            entityName,
                            fieldName,
                            searchOrder == FieldSearchOrder.Descending ? "DESC" : string.Empty);

                        Debug.WriteLine(sql);

                        command.CommandText = sql;
                        command.ExecuteNonQuery();
                    }

                    var indexinfo = new IndexInfo
                    {
                        Name = indexName
                    };

                    sql = string.Format("SELECT CHARACTER_MAXIMUM_LENGTH FROM information_schema.columns WHERE TABLE_NAME = '{0}' AND COLUMN_NAME = '{1}'"
                        , entityName, fieldName);

                    command.CommandText = sql;

                    using (var reader = command.ExecuteReader())
                    {
                        // this should always return true
                        if (reader.Read())
                        {
                            var length = reader[0];
                            if (length != DBNull.Value)
                            {
                                indexinfo.MaxCharLength = Convert.ToInt32(length);
                            }
                        }
                        else
                        {
                            if (Debugger.IsAttached) Debugger.Break();
                        }
                    }

                    m_indexNameCache.Add(indexinfo);
                }

                return indexName;
            }
            finally
            {
                if (localConnection)
                {
                    DoneWithConnection(connection, true);
                }
            }
        }

        protected virtual string GetFieldDataTypeString(string entityName, FieldAttribute field)
        {
            // the SQL RowVersion is a special case
            if (field.IsRowVersion)
            {
                switch (field.DataType)
                {
                    case DbType.UInt64:
                    case DbType.Int64:
                        // no error
                        break;
                    default:
                        throw new FieldDefinitionException(entityName, field.FieldName, "rowversion fields must be an 8-byte data type (Int64 or UInt64)");
                }

                return "rowversion";
            }

            if (field.DataType == DbType.Binary)
            {
                // default to varbinary unless a Length is specifically supplied and it is >= 8000
                if (field.Length >= MaxSizedBinaryLength)
                {
                    return "image";
                }
                // if no length was supplied, default to DefaultVarBinaryLength (8000)
                return string.Format("varbinary({0})", field.Length == 0 ? DefaultVarBinaryLength : field.Length);
            }

            if ((field.DataType == DbType.String) && (field.Length > MaxSizedStringLength))
            {
                return "ntext";
            }

            return field.DataType.ToSqlTypeString();
        }

        protected virtual string GetFieldCreationAttributes(EntityAttribute attribute, FieldAttribute field)
        {
            StringBuilder sb = new StringBuilder();

            switch (field.DataType)
            {
                case DbType.String:
                case DbType.StringFixedLength:
                    if (field.Length > 0)
                    {
                        if (field.Length <= MaxSizedStringLength)
                        {
                            sb.AppendFormat("({0}) ", field.Length);
                        }
                        // SQLCE uses ntext, which cannot have a size
                        //else
                        //{
                        //    sb.AppendFormat("({0}) ", MaxSizedStringLength);
                        //}
                    }
                    else
                    {
                        sb.AppendFormat("({0}) ", DefaultStringFieldSize);
                    }
                    break;
                case DbType.Decimal:
                    int p = field.Precision == 0 ? DefaultNumericFieldPrecision : field.Precision;
                    sb.AppendFormat("({0},{1}) ", p, field.Scale);
                    break;
            }

            if ((field.DefaultType != DefaultType.None) || (field.DefaultValue != null))
            {
                if (field.DefaultType == DefaultType.CurrentDateTime)
                {
                    // allow an override of the actual default value - if none is provided, use the default SqlDateTimeDefault
                    if ((field.DefaultValue != null) && (field.DefaultValue is IDefaultValue))
                    {
                        sb.AppendFormat("DEFAULT {0} ", (field.DefaultValue as IDefaultValue).GetDefaultValue());
                    }
                    else
                    {
                        sb.AppendFormat("DEFAULT {0} ", DefaultDateGenerator);                    
                    }
                }
                else
                {
                    if (field.DefaultValue is string)
                    {
                        sb.AppendFormat("DEFAULT '{0}' ", field.DefaultValue);
                    }
                    else
                    {
                        sb.AppendFormat("DEFAULT {0} ", field.DefaultValue);
                    }
                }
            }

            if (field.IsPrimaryKey)
            {
                sb.Append("PRIMARY KEY ");

                if (attribute.KeyScheme == KeyScheme.Identity)
                {
                    switch(field.DataType)
                    {
                        case DbType.Int32:
                        case DbType.UInt32:
                        case DbType.Int64:
                        case DbType.UInt64:
                            sb.Append(AutoIncrementFieldIdentifier + " ");
                            break;
                        case DbType.Guid:
                            sb.Append("ROWGUIDCOL ");
                            break;
                        default:
                            throw new FieldDefinitionException(attribute.NameInStore, field.FieldName,
                                string.Format("Data Type '{0}' cannot be marked as an Identity field", field.DataType));
                    }
                }
            }

            if (!field.AllowsNulls)
            {
                sb.Append("NOT NULL ");
            }

            if (field.RequireUniqueValue)
            {
                sb.Append("UNIQUE ");
            }

            return sb.ToString();
        }

        protected virtual MethodInfo GetSerializer(Type itemType)
        {
            if(itemType.Equals(typeof(DynamicEntity)))
            {
                throw new NotSupportedException("Object Field serialization not supported for DynamicEntities");
            }

            if (m_serializerCache.ContainsKey(itemType))
            {
                return m_serializerCache[itemType];
            }

            var serializer = itemType.GetMethod("Serialize", BindingFlags.Public | BindingFlags.Instance);

            if (serializer == null) return null;

            m_serializerCache.Add(itemType, serializer);
            return serializer;
        }

        protected virtual MethodInfo GetDeserializer(Type itemType)
        {
            if (m_deserializerCache.ContainsKey(itemType))
            {
                return m_deserializerCache[itemType];
            }

            var deserializer = itemType.GetMethod("Deserialize", BindingFlags.Public | BindingFlags.Instance);

            if (deserializer == null) return null;

            m_deserializerCache.Add(itemType, deserializer);
            return deserializer;
        }

        /// <summary>
        /// Determines if the specified object already exists in the Store (by primary key value)
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public override bool Contains(object item)
        {
            var itemType = item.GetType();
            string entityName = m_entities.GetNameForType(itemType);

            var keyValue = this.Entities[entityName].Fields.KeyField.PropertyInfo.GetValue(item, null);

            var existing = Select(itemType, null, keyValue, -1, -1).FirstOrDefault();

            return existing != null;
        }

        /// <summary>
        /// Retrieves a single entity instance from the DataStore identified by the specified primary key value
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="primaryKey"></param>
        /// <returns></returns>
        public override T Select<T>(object primaryKey)
        {
            return (T)Select(typeof(T), null, primaryKey, -1, -1, true).FirstOrDefault();
        }

        public override T Select<T>(object primaryKey, bool fillReferences)
        {
            return (T)Select(typeof(T), null, primaryKey, -1, -1, fillReferences).FirstOrDefault();
        }

        /// <summary>
        /// Retrieves all entity instances of the specified type from the DataStore
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public override IEnumerable<T> Select<T>()
        {
            var type = typeof(T);
            var items = Select(type, null, null, -1, 0);
            return items.Cast<T>();
        }

        /// <summary>
        /// Retrieves all entity instances of the specified type from the DataStore
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public override IEnumerable<T> Select<T>(bool fillReferences)
        {
            var type = typeof(T);
            var items = Select(type, null, null, -1, 0, fillReferences);
            return items.Cast<T>();
        }

        /// <summary>
        /// Retrieves all entity instances of the specified type from the DataStore
        /// </summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        public override IEnumerable<object> Select(Type entityType)
        {
            return Select(entityType, true);
        }

        public override IEnumerable<object> Select(Type entityType, bool fillReferences)
        {
            var items = Select(entityType, null, null, -1, 0, fillReferences);
            return items;
        }

        public override IEnumerable<T> Select<T>(string searchFieldName, object matchValue)
        {
            return Select<T>(searchFieldName, matchValue, true);
        }

        public override IEnumerable<T> Select<T>(string searchFieldName, object matchValue, bool fillReferences)
        {
            var type = typeof(T);
            var items = Select(type, searchFieldName, matchValue, -1, 0, fillReferences);
            return items.Cast<T>();
        }

        public override IEnumerable<T> Select<T>(IEnumerable<FilterCondition> filters)
        {
            return Select<T>(filters, false);
        }

        public override IEnumerable<T> Select<T>(IEnumerable<FilterCondition> filters, bool fillReferences)
        {
            var objectType = typeof(T);
            return Select(objectType, filters, -1, 0, fillReferences).Cast<T>();
        }

        private IEnumerable<object> Select(Type objectType, string searchFieldName, object matchValue, int fetchCount, int firstRowOffset)
        {
            return Select(objectType, searchFieldName, matchValue, fetchCount, firstRowOffset, true);
        }

        protected virtual IEnumerable<object> Select(Type objectType, string searchFieldName, object matchValue, int fetchCount, int firstRowOffset, bool fillReferences)
        {
            var entityName = Entities.GetNameForType(objectType);
            // if the entity type hasn't already been registered, try to auto-register
            if (entityName == null)
            {
                AddType(objectType);
            }
            
            FilterCondition filter = null;

            if (searchFieldName == null)
            {
                if (matchValue != null)
                {
                    CheckPrimaryKeyIndex(entityName);

                    // searching on primary key
                    filter = new SqlFilterCondition
                    {
                        FieldName = (Entities[entityName] as SqlEntityInfo).PrimaryKeyColumnName,
                        Operator = FilterCondition.FilterOperator.Equals,
                        Value = matchValue,
                        PrimaryKey = true
                    };
                }
            }
            else
            {
                filter = new FilterCondition
                {
                    FieldName = searchFieldName,
                    Operator = FilterCondition.FilterOperator.Equals,
                    Value = matchValue
                };
            }

            return Select(
                objectType,
                (filter == null) ? null :
                    new FilterCondition[]
                    {
                        filter
                    },
                fetchCount,
                firstRowOffset,
                fillReferences);
        }

        private const int CommandCacheMaxLength = 10;
        protected Dictionary<string, IDbCommand> CommandCache = new Dictionary<string, IDbCommand>();

        /// <summary>
        /// Determines if the ORM engine should be allowed to cache commands of not.  If you frequently use the same FilterConditions on a Select call to a single entity, 
        /// using the command cache can improve performance by preventing the underlying SQL Compact Engine from recomputing statistics.
        /// </summary>
        public bool UseCommandCache { get; set; }

        public void ClearCommandCache()
        {
            lock (CommandCache)
            {
                foreach (var cmd in CommandCache)
                {
                    cmd.Value.Dispose();
                }
                CommandCache.Clear();
            }
        }
        
        public override IEnumerable<DynamicEntity> Select(string entityName, IEnumerable<FilterCondition> filters)
        {
            throw new NotSupportedException();
            //var connection = GetConnection(true);
            //try
            //{
            //    using (var command = BuildFilterCommand(entityName, filters, false))
            //    {
            //        command.Connection = connection;
            //        var reader = command.ExecuteReader();
            //    }
            //}
            //finally
            //{
            //    DoneWithConnection(connection, true);
            //}
        }

        protected virtual TCommand GetSelectCommand<TCommand, TParameter>(string entityName, IEnumerable<FilterCondition> filters, out bool tableDirect)
            where TCommand : DbCommand, new()
            where TParameter : IDataParameter, new()
        {
            tableDirect = false;
            return BuildFilterCommand<TCommand, TParameter>(entityName, filters);
        }

        protected TCommand BuildFilterCommand<TCommand, TParameter>(string entityName, IEnumerable<FilterCondition> filters)
            where TCommand : DbCommand, new()
            where TParameter : IDataParameter, new()
        {
            return BuildFilterCommand<TCommand, TParameter>(entityName, filters, false);
        }

        protected TCommand BuildFilterCommand<TCommand, TParameter>(string entityName, IEnumerable<FilterCondition> filters, bool isCount)
            where TCommand : DbCommand, new()
            where TParameter : IDataParameter, new()
        {
            var command = new TCommand();
            command.CommandType = CommandType.Text;
            var @params = new List<TParameter>();

            StringBuilder sb;

            if (isCount)
            {
                sb = new StringBuilder(string.Format("SELECT COUNT(*) FROM {0}", entityName));
            }
            else
            {
//                sb = new StringBuilder(string.Format("SELECT * FROM {0}", entityName));
                sb = new StringBuilder("SELECT ");

                var count = Entities[entityName].Fields.Count;
                var ordinal = 0;
                foreach (var field in Entities[entityName].Fields)
                {
                    field.Ordinal = ordinal;
                    ordinal++;
                    sb.Append(field.FieldName);
                    if (--count > 0) sb.Append(", ");
                }
                sb.Append(string.Format(" FROM {0}", entityName));
            }

            if (filters != null)
            {
                for (int i = 0; i < filters.Count(); i++)
                {
                    sb.Append(i == 0 ? " WHERE " : " AND ");

                    var filter = filters.ElementAt(i);
                    sb.Append(filter.FieldName);

                    switch (filters.ElementAt(i).Operator)
                    {
                        case FilterCondition.FilterOperator.Equals:
                            if ((filter.Value == null) || (filter.Value == DBNull.Value))
                            {
                                sb.Append(" IS NULL ");
                                continue;
                            }
                            sb.Append(" = ");
                            break;
                        case FilterCondition.FilterOperator.Like:
                            sb.Append(" LIKE ");
                            break;
                        case FilterCondition.FilterOperator.LessThan:
                            sb.Append(" < ");
                            break;
                        case FilterCondition.FilterOperator.GreaterThan:
                            sb.Append(" > ");
                            break;
                        default:
                            throw new NotSupportedException();
                    }

                    string paramName = string.Format("{0}p{1}", ParameterPrefix, i);
                    sb.Append(paramName);

                    var param = new TParameter()
                    {
                        ParameterName = paramName,
                        Value = filter.Value ?? DBNull.Value
                    };

                    @params.Add(param);
                }
            }
            var sql = sb.ToString();
            command.CommandText = sql;
            command.Parameters.AddRange(@params.ToArray());

            if ((UseCommandCache) && (!isCount))
            {
                lock (CommandCache)
                {
                    if (CommandCache.ContainsKey(sql))
                    {
                        command.Dispose();
                        command = (TCommand)CommandCache[sb.ToString()];

                        // use the cached command object, but we must copy over the new command parameter values
                        // or it will use the old ones
                        for (int p = 0; p < command.Parameters.Count; p++)
                        {
                            command.Parameters[p].Value = @params[p].Value;
                        }
                    }
                    else
                    {
                        CommandCache.Add(sql, command);

                        // trim the cache so it doesn't grow infinitely
                        if (CommandCache.Count > CommandCacheMaxLength)
                        {
                            CommandCache.Remove(CommandCache.First().Key);
                        }
                    }
                }
            }

            return command;
        }


        protected IDbCommand BuildFilterCommand(string entityName, IEnumerable<FilterCondition> filters, bool isCount)
        {
            var command = GetNewCommandObject();
            command.CommandType = CommandType.Text;

            var @params = new List<IDataParameter>();

            StringBuilder sb;

            if (isCount)
            {
                sb = new StringBuilder(string.Format("SELECT COUNT(*) FROM {0}", entityName));
            }
            else
            {
                //                sb = new StringBuilder(string.Format("SELECT * FROM {0}", entityName));
                sb = new StringBuilder("SELECT ");

                var count = Entities[entityName].Fields.Count;
                var ordinal = 0;
                foreach (var field in Entities[entityName].Fields)
                {
                    field.Ordinal = ordinal;
                    ordinal++;
                    sb.Append(field.FieldName);
                    if (--count > 0) sb.Append(", ");
                }
                sb.Append(string.Format(" FROM {0}", entityName));
            }

            if (filters != null)
            {
                for (int i = 0; i < filters.Count(); i++)
                {
                    sb.Append(i == 0 ? " WHERE " : " AND ");

                    var filter = filters.ElementAt(i);
                    sb.Append(filter.FieldName);

                    switch (filters.ElementAt(i).Operator)
                    {
                        case FilterCondition.FilterOperator.Equals:
                            if ((filter.Value == null) || (filter.Value == DBNull.Value))
                            {
                                sb.Append(" IS NULL ");
                                continue;
                            }
                            sb.Append(" = ");
                            break;
                        case FilterCondition.FilterOperator.Like:
                            sb.Append(" LIKE ");
                            break;
                        case FilterCondition.FilterOperator.LessThan:
                            sb.Append(" < ");
                            break;
                        case FilterCondition.FilterOperator.GreaterThan:
                            sb.Append(" > ");
                            break;
                        default:
                            throw new NotSupportedException();
                    }

                    string paramName = string.Format("{0}p{1}", ParameterPrefix, i);
                    sb.Append(paramName);

                    var param = command.CreateParameter();

                    param.ParameterName = paramName;
                    param.Value = filter.Value ?? DBNull.Value;

                    command.Parameters.Add(param);
                }
            }
            var sql = sb.ToString();
            command.CommandText = sql;

            if ((UseCommandCache) && (!isCount))
            {
                lock (CommandCache)
                {
                    if (CommandCache.ContainsKey(sql))
                    {
                        command.Dispose();
                        command = (DbCommand)CommandCache[sb.ToString()];

                        // use the cached command object, but we must copy over the new command parameter values
                        // or it will use the old ones
                        for (int p = 0; p < command.Parameters.Count; p++)
                        {
                            (command.Parameters[p] as IDbDataParameter).Value = @params[p].Value;
                        }
                    }
                    else
                    {
                        CommandCache.Add(sql, command);

                        // trim the cache so it doesn't grow infinitely
                        if (CommandCache.Count > CommandCacheMaxLength)
                        {
                            CommandCache.Remove(CommandCache.First().Key);
                        }
                    }
                }
            }

            return command;
        }

        protected void CheckPrimaryKeyIndex(string entityName)
        {
            var info = Entities[entityName] as SqlEntityInfo;

            string column, index;

            if (info.PrimaryKeyIndexName == null)
            {
                GetPrimaryKeyInfo(entityName, out index, out column);
                info.PrimaryKeyIndexName = index;
                info.PrimaryKeyColumnName = column;
            }
        }

        protected virtual void CheckOrdinals(string entityName)
        {
            if (Entities[entityName].Fields.OrdinalsAreValid) return;

            var connection = GetConnection(true);
            try
            {
                using (var command = GetNewCommandObject())
                { 
                    command.Connection = connection;

                    // only bring back the structure, not any data (for improved speed)
                    command.CommandText = string.Format("SELECT * FROM {0} WHERE 0 = 1", entityName);

                    using (var reader = command.ExecuteReader())
                    {
                        foreach (var field in Entities[entityName].Fields)
                        {
                            field.Ordinal = reader.GetOrdinal(field.FieldName);
                        }

                        Entities[entityName].Fields.OrdinalsAreValid = true;
                    }

                    command.Dispose();
                }
            }
            finally
            {
                DoneWithConnection(connection, true);
            }
        }

        /// <summary>
        /// Populates the ReferenceField members of the provided entity instance
        /// </summary>
        /// <param name="instance"></param>
        public override void FillReferences(object instance)
        {
            FillReferences(instance, null, null, false);
        }

        protected void FlushReferenceTableCache()
        {
            m_referenceCache.Clear();
        }

        protected void DoInsertReferences(object item, string entityName, KeyScheme keyScheme, bool beforeParentInsert)
        {
            // cascade insert any References
            // do this last because we need the PK from above
            foreach (var reference in Entities[entityName].References)
            {
                if (beforeParentInsert && (reference.ReferenceType == ReferenceType.ManyToOne)) // N:1
                {
                    // in an N:1 we need to insert the related item first, so it can get a PK assigned
                    var referenceEntity = reference.PropertyInfo.GetValue(item, null);

                    // is there anything to insert?
                    if (referenceEntity == null) continue;

                    var referenceEntityName = Entities.GetNameForType(reference.ReferenceEntityType);
                    var refPK = Entities[referenceEntityName].Fields.KeyField.PropertyInfo.GetValue(referenceEntity, null);

                    // does the reference entity already exist in the store?
                    var existing = Select(reference.ReferenceEntityType, null, refPK, -1, -1, true).FirstOrDefault();

                    if (existing == null)
                    {
                        Insert(referenceEntity);

                        // we then copy the PK of the reference item into the "local" FK field - need to re-query the key
                        refPK = Entities[referenceEntityName].Fields.KeyField.PropertyInfo.GetValue(referenceEntity, null);

                        // set the item key
                        // we already inserted, so we have to do an update
                        // TODO: in the future, we should move this up and do reference inserts first, then back=propagate references
                        Entities[entityName].Fields[reference.ForeignReferenceField].PropertyInfo.SetValue(item, refPK, null);
                    }
                    else
                    {
                        // TODO: should we look for reference entity updates?  That's complex and probably out of scope for the purposes of ORM
                    }
                }
                else if(!beforeParentInsert && (reference.ReferenceType == ReferenceType.OneToMany)) // 1:N
                {
                    // cascade insert any References
                    // do this last because we need the PK from above
                    string et = null;

                    var valueArray = reference.PropertyInfo.GetValue(item, null);
                    if (valueArray == null) continue;

                    //entityName = m_entities.GetNameForType(reference.ReferenceEntityType);
                    var fk = Entities[entityName].Fields[reference.ForeignReferenceField].PropertyInfo.GetValue(item, null);

                    // we've already enforced this to be an array when creating the store
                    foreach (var element in valueArray as Array)
                    {
                        if (et == null)
                        {
                            et = m_entities.GetNameForType(element.GetType());
                        }

                        // get the FK value
                        var keyValue = Entities[et].Fields.KeyField.PropertyInfo.GetValue(element, null);

                        bool isNew = false;


                        // only do an insert if the value is new (i.e. need to look for existing reference items)
                        // not certain how this will work right now, so for now we ask the caller to know what they're doing
                        switch (keyScheme)
                        {
                            case KeyScheme.Identity:
                                // SQLCE and SQLite start with an ID == 1, so 0 mean "not in DB"
                                isNew = keyValue.Equals(0) || keyValue.Equals(-1);
                                break;
                            case KeyScheme.GUID:
                                // TODO: see if PK field value == null
                                isNew = keyValue.Equals(null);
                                break;
                        }

                        if (isNew)
                        {
                            Entities[et].Fields[reference.ForeignReferenceField].PropertyInfo.SetValue(element, fk, null);
                            Insert(element);
                        }
                    }
                }

            }
        }
        
        protected void FillReferences(object instance, object keyValue, ReferenceAttribute[] fieldsToFill, bool cacheReferenceTable)
        {
            if (instance == null) return;

            Type type = instance.GetType();
            var entityName = m_entities.GetNameForType(type);

            if (entityName == null)
            {
                AddType(type);
                entityName = m_entities.GetNameForType(type);
            }

            if (Entities[entityName].References.Count == 0) return;

            Dictionary<ReferenceAttribute, object[]> referenceItems = new Dictionary<ReferenceAttribute, object[]>();

            // query the key if not provided
            if (keyValue == null)
            {
                keyValue = m_entities[entityName].Fields.KeyField.PropertyInfo.GetValue(instance, null);
            }

            // populate reference fields
            foreach (var reference in Entities[entityName].References)
            {
                if (fieldsToFill != null)
                {
                    if (!fieldsToFill.Contains(reference))
                    {
                        continue;
                    }
                }

                if (reference.ReferenceType == ReferenceType.ManyToOne)
                {
                    // In a N:1 relation, the local ('instance' coming in here) key is the FK and the remote it the PK.  
                    // We need to read the local FK, so we can go to the reference table and pull the one row with that PK value
                    keyValue = m_entities[entityName].Fields[reference.ForeignReferenceField].PropertyInfo.GetValue(instance, null);
                }

                // get the lookup values - until we support filtered selects, this may be very expensive memory-wise
                if (!referenceItems.ContainsKey(reference))
                {
                    IEnumerable<object> refData;
                    if (cacheReferenceTable)
                    {
                        // TODO: ref cache needs to be type->reftype->ref's, not type->refs

                        if (!m_referenceCache.ContainsKey(reference.ReferenceEntityType))
                        {
                            refData = Select(reference.ReferenceEntityType, null, null, -1, 0);
                            m_referenceCache.Add(reference.ReferenceEntityType, refData.ToArray());
                        }
                        else
                        {
                            refData = m_referenceCache[reference.ReferenceEntityType];
                        }
                    }
                    else
                    {
                        // FALSE for last parameter to prevent circular reference filling
                        refData = Select(reference.ReferenceEntityType, reference.ForeignReferenceField, keyValue, -1, 0, false);
                    }

                    // see if the reference type is known - if not, try to add it automatically
                    var name = Entities.GetNameForType(reference.ReferenceEntityType);
                    if (name == null)
                    {
                        AddType(reference.ReferenceEntityType);
                    }

                    referenceItems.Add(reference, refData.ToArray());
                }

                // get the lookup field
                var childEntityName = m_entities.GetNameForType(reference.ReferenceEntityType);

                var children = new List<object>();

                // now look for those that match our pk
                foreach (var child in referenceItems[reference])
                {
                    var childKey = m_entities[childEntityName].Fields[reference.ForeignReferenceField].PropertyInfo.GetValue(child, null);

                    // this seems "backward" because childKey may turn out null, 
                    // so doing it backwards (keyValue.Equals instead of childKey.Equals) prevents a null referenceexception
                    // we have to do the conversion becasue SQLite will have one of these as a 32-bit and the other as a 64-bit, and "Equals" will turn out false
                    if (keyValue.Equals(Convert.ChangeType(childKey, keyValue.GetType(), null)))
                    {
                        children.Add(child);
                    }
                }
                var carr = children.ConvertAll(reference.ReferenceEntityType);

                if (reference.PropertyInfo.PropertyType.IsArray)
                {
                    reference.PropertyInfo.SetValue(instance, carr, null);
//                    reference.PropertyInfo.SetValue(instance, Convert.ChangeType(carr, reference.PropertyInfo.PropertyType), null);
                }
                else
                {
                    var enumerator = carr.GetEnumerator();

                    if (enumerator.MoveNext())
                    {
                        reference.PropertyInfo.SetValue(instance, children[0], null);
                    }
                }
            }
        }

        /// <summary>
        /// Deletes all rows from the specified Table
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public virtual void TruncateTable(string tableName)
        {
            var connection = GetConnection(true);
            try
            {
                using (var command = GetNewCommandObject())
                {
                    command.Connection = connection;
                    command.CommandText = string.Format("DELETE FROM {0}", tableName);
                    command.ExecuteNonQuery();
                }
            }
            finally
            {
                DoneWithConnection(connection, true);
            }
        }

        public virtual bool TableExists(string tableName)
        {
            return GetTableNames().Contains(tableName, StringComparer.InvariantCultureIgnoreCase);
        }

        public virtual void DropTable(string tableName)
        {
            if (!TableExists(tableName)) return;

            var connection = GetConnection(true);
            try
            {
                using (var command = GetNewCommandObject())
                {
                    command.Connection = connection;
                    command.CommandText = string.Format("DROP TABLE {0}", tableName);
                    command.ExecuteNonQuery();
                }

                Entities.Remove(tableName);
            }
            finally
            {
                DoneWithConnection(connection, true);
            }
        }


        /// <summary>
        /// Deletes all entity instances of the specified type from the DataStore
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public override void Delete<T>()
        {
            if (typeof(T).Equals(typeof(DynamicEntity)))
            {
                throw new ArgumentException("DynamicEntities must be deleted with one of the other Delete overloads.");
            }

            var t = typeof(T);
            string entityName = m_entities.GetNameForType(t);

            if (entityName == null)
            {
                throw new EntityNotFoundException(t);
            }

            // TODO: handle cascade deletes?

            TruncateTable(entityName);
        }

        public override void Delete<T>(string fieldName, object matchValue)
        {
            if (typeof(T).Equals(typeof(DynamicEntity)))
            {
                throw new ArgumentException("DynamicEntities must be deleted with one of the other Delete overloads.");
            }

            string entityName = m_entities.GetNameForType(typeof(T));

            Delete(entityName, fieldName, matchValue);
        }

        /// <summary>
        /// Deletes entities of a given type where the specified field name matches a specified value
        /// </summary>
        /// <param name="entityType"></param>
        /// <param name="indexName"></param>
        /// <param name="matchValue"></param>
        protected void Delete(Type entityType, string fieldName, object matchValue)
        {
            if (entityType.Equals(typeof(DynamicEntity)))
            {
                throw new ArgumentException("DynamicEntities must be deleted with one of the other Delete overloads.");
            }

            string entityName = m_entities.GetNameForType(entityType);

            Delete(entityName, fieldName, matchValue);
        }

        public override void Delete(string entityName, string fieldName, object matchValue)
        {
            var connection = GetConnection(true);
            try
            {
                using (var command = GetNewCommandObject())
                {
                    command.Connection = connection;
                    command.Transaction = CurrentTransaction;
                    command.CommandText = string.Format("DELETE FROM {0} WHERE {1} = {2}val", entityName, fieldName, ParameterPrefix);
                    var param = CreateParameterObject(ParameterPrefix + "val", matchValue);
                    command.Parameters.Add(param);
                    command.ExecuteNonQuery();
                }
            }
            finally
            {
                DoneWithConnection(connection, true);
            }
        }

        /// <summary>
        /// Deletes the specified entity instance from the DataStore
        /// </summary>
        /// <param name="item"></param>
        /// <remarks>
        /// The instance provided must have a valid primary key value
        /// </remarks>
        public override void OnDelete(object item)
        {
            if (item is DynamicEntity)
            {
                var de = item as DynamicEntity;

                if (string.IsNullOrEmpty(de.KeyField))
                {
                    throw new PrimaryKeyRequiredException("A primary key is required on an Entity in order to perform a Delete");
                }

                Delete(de.EntityName, de.KeyField, de.Fields[de.KeyField]);
            }
            else
            {
                var type = item.GetType();
                string entityName = m_entities.GetNameForType(type);
                if (entityName == null)
                {
                    throw new EntityNotFoundException(type);
                }

                if (Entities[entityName].Fields.KeyField == null)
                {
                    throw new PrimaryKeyRequiredException("A primary key is required on an Entity in order to perform a Delete");
                }
                var keyValue = Entities[entityName].Fields.KeyField.PropertyInfo.GetValue(item, null);

                Delete(type, keyValue);
            }
        }

        protected virtual void Delete(Type t, object primaryKey)
        {
            string entityName = m_entities.GetNameForType(t);

            // if the entity type hasn't already been registered, try to auto-register
            if (entityName == null)
            {
                AddType(t);
            }

            Delete(entityName, primaryKey);
        }

        public override void Delete(string entityName, object primaryKey)
        {
            if (entityName == null)
            {
                throw new EntityNotFoundException(entityName);
            }

            if (Entities[entityName].Fields.KeyField == null)
            {
                throw new PrimaryKeyRequiredException("A primary key is required on an Entity in order to perform a Delete");
            }

            // handle cascade deletes
            foreach (var reference in Entities[entityName].References)
            {
                if (!reference.CascadeDelete) continue;

                Delete(reference.ReferenceEntityType, reference.ForeignReferenceField, primaryKey);
            }

            var keyFieldName = Entities[entityName].Fields.KeyField.FieldName;
            Delete(entityName, keyFieldName, primaryKey);
        }

        public override int Count(string entityName)
        {
            if (string.IsNullOrEmpty(entityName))
            {
                throw new EntityNotFoundException(entityName);
            }

            var connection = GetConnection(true);
            try
            {
                using (var command = GetNewCommandObject())
                {
                    command.Connection = connection;
                    command.CommandText = string.Format("SELECT COUNT(*) FROM {0}", entityName);
                    var count = command.ExecuteScalar();
                    return Convert.ToInt32(count);
                }
            }
            finally
            {
                DoneWithConnection(connection, true);
            }
        }

        /// <summary>
        /// Fetches a sorted list of entities, up to the requested number of entity instances, of the specified type from the DataStore, starting with the specified instance
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fetchCount"></param>
        /// <param name="firstRowOffset"></param>
        /// <param name="sortField"></param>
        /// <returns></returns>
        public override IEnumerable<T> Fetch<T>(int fetchCount, int firstRowOffset, string sortField)
        {
            return Fetch<T>(fetchCount, firstRowOffset, sortField, FieldSearchOrder.Ascending, null, false);
        }

        /// <summary>
        /// Fetches up to the requested number of entity instances of the specified type from the DataStore, starting with the first instance
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fetchCount"></param>
        /// <returns></returns>
        public override IEnumerable<T> Fetch<T>(int fetchCount)
        {
            var type = typeof(T);
            var items = Select(type, null, null, fetchCount, 0, false);
            return items.Cast<T>();
        }

        /// <summary>
        /// Fetches up to the requested number of entity instances of the specified type from the DataStore, starting with the specified instance
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fetchCount"></param>
        /// <param name="firstRowOffset"></param>
        /// <returns></returns>
        public override IEnumerable<T> Fetch<T>(int fetchCount, int firstRowOffset)
        {
            var type = typeof(T);
            var items = Select(type, null, null, fetchCount, firstRowOffset, false);
            return items.Cast<T>();
        }

        protected override void AfterAddEntityType(Type entityType, bool ensureCompatibility)
        {
            if ((StoreExists) && (ensureCompatibility))
            {
                var connection = GetConnection(true);
                try
                {
                    var name = Entities.GetNameForType(entityType);

                    // this will exist because the caller inserted it
                    var entity = Entities[name];

                    if (!TableExists(name))
                    {
                        CreateTable(connection, entity);
                    }
                    else
                    {
                        ValidateTable(connection, entity);
                    }

                }
                finally
                {
                    DoneWithConnection(connection, true);
                }
            }
        }

        protected override void OnDynamicEntityRegistration(DynamicEntityDefinition definition, bool ensureCompatibility)
        {
            if (definition.EntityName.Contains(' '))
            {
                throw new ArgumentException("Entity Names cannot contain spaces");
            }

            var connection = GetConnection(true);
            try
            {
                // this will exist because the caller inserted it
                var entity = Entities[definition.EntityName];

                if (!TableExists(definition.EntityName))
                {
                    CreateTable(connection, entity);
                }
                else
                {
                    ValidateTable(connection, entity);
                }

            }
            finally
            {
                DoneWithConnection(connection, true);
            }
        }

        public abstract override DynamicEntityDefinition DiscoverDynamicEntity(string entityName);

        /// <summary>
        /// Ensures that the underlying database tables contain all of the Fields to represent the known entities.
        /// This is useful if you need to add a Field to an existing store.  Just add the Field to the Entity, then 
        /// call EnsureCompatibility to have the field added to the database.
        /// </summary>
        public override void EnsureCompatibility()
        {
            if (!StoreExists)
            {
                CreateStore();
                return;
            }

            var connection = GetConnection(true);
            try
            {
                lock (Entities.SyncRoot)
                {
                    foreach (var entity in this.Entities)
                    {
                        ValidateTable(connection, entity);
                    }
                }
            }
            finally
            {
                DoneWithConnection(connection, true);
            }
        }

        private ConnectionBehavior m_nonTransactionConnectionBehavior;

        public override void BeginTransaction(IsolationLevel isolationLevel)
        {
            lock (m_transactionSyncRoot)
            {
                if (CurrentTransaction != null)
                {
                    throw new InvalidOperationException("Parallel transactions are not supported");
                }

                // we must escalate the connection behavior for the transaction to remain valid
                if (ConnectionBehavior != ORM.ConnectionBehavior.Persistent)
                {
                    m_nonTransactionConnectionBehavior = ConnectionBehavior;
                    ConnectionBehavior = ORM.ConnectionBehavior.Persistent;
                }


                if (m_connection == null)
                {
                    // force creation of the persistent connection
                    var c = GetConnection(false);
                    DoneWithConnection(c, false);
                }

                CurrentTransaction = m_connection.BeginTransaction(isolationLevel);
            }
        }

        public override void Commit()
        {
            if (CurrentTransaction == null)
            {
                throw new InvalidOperationException();
            }

            lock (m_transactionSyncRoot)
            {
                CurrentTransaction.Commit();
                CurrentTransaction.Dispose();
                CurrentTransaction = null;
                // revert connection behavior if we escalated
                ConnectionBehavior = m_nonTransactionConnectionBehavior;
            }
        }

        public override void Rollback()
        {
            if (CurrentTransaction == null)
            {
                throw new InvalidOperationException();
            }

            lock (m_transactionSyncRoot)
            {
                CurrentTransaction.Rollback();
                CurrentTransaction.Dispose();
                CurrentTransaction = null;
                // revert connection behavior if we escalated
                ConnectionBehavior = m_nonTransactionConnectionBehavior;
            }
        }

        protected void UpdateEntityPropertyInfo(string entityName, string fieldName, PropertyInfo pi)
        {
            this.Entities[entityName].Fields[fieldName].PropertyInfo = pi;
        }

        public IDataReader ExecuteReader(string sql)
        {
            return ExecuteReader(sql, null, false);
        }

        public virtual IDataReader ExecuteReader(string sql, bool throwExceptions)
        {
            return ExecuteReader(sql, null, throwExceptions);
        }

        public virtual IDataReader ExecuteReader(string sql, IEnumerable<IDataParameter> parameters)
        {
            return ExecuteReader(sql, parameters, false);
        }

        public virtual IDataReader ExecuteReader(string sql, IEnumerable<IDataParameter> parameters, bool throwExceptions)
        {
            return ExecuteReader(sql, parameters, CommandBehavior.Default, throwExceptions);
        }

        private IDbConnection m_readerConnection;

        public void CloseReader()
        {
            if (m_readerConnection != null)
            {
                m_readerConnection.Close();
                m_readerConnection.Dispose();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <param name="behavior"></param>
        /// <param name="throwExceptions"></param>
        /// <returns></returns>
        /// <remarks>You <b>MUST</b> call CloseReader after calling this method to prevent a leak</remarks>
        public virtual IDataReader ExecuteReader(string sql, IEnumerable<IDataParameter> parameters, CommandBehavior behavior, bool throwExceptions)
        {
            IDbConnection connection;

            if (ConnectionBehavior != ORM.ConnectionBehavior.Persistent)
            {
                m_readerConnection = GetNewConnectionObject();
                m_readerConnection.Open();
                connection = m_readerConnection;
            }
            else
            {
                connection = GetConnection(false);
            }

            try
            {
                using (var command = GetNewCommandObject())
                {
                    command.CommandText = sql;
                    command.Connection = connection;
                    command.Transaction = CurrentTransaction;
                    if(parameters != null)
                    {
                        foreach(var p in parameters)
                        {
                            command.Parameters.Add(p);
                        }
                    }

                    var reader = command.ExecuteReader(behavior);
                    return reader;
                }
            }
            catch (Exception ex)
            {
                if (throwExceptions) throw;

                Debug.WriteLine("SQLStoreBase::ExecuteReader threw: " + ex.Message);
                return null;
            }
            finally
            {
                if (ConnectionBehavior == ORM.ConnectionBehavior.Persistent)
                {
                    DoneWithConnection(connection, false);
                }
            }
        }

        public override IEnumerable<DynamicEntity> Fetch(string entityName, int fetchCount, int firstRowOffset, string sortField, FieldSearchOrder sortOrder, FilterCondition filter, bool fillReferences)
        {
            if(fillReferences) throw new NotSupportedException("References not supported with this version of Fetch on this Provider");
            if(filter != null) throw new NotSupportedException("Filter is not supported with this version of Fetch on this Provider.  Try post-filtering with LINQ or implement filtering in the derived DataStore.");

            // This is SQL Server syntax
            //var sql = new StringBuilder();
            //
            //if(fetchCount > 0)
            //{
            //    sql.AppendFormat("SELECT TOP ({0}) * FROM {1}", fetchCount, entityName);
            //}
            //else
            //{
            //    sql.AppendFormat("SELECT * FROM {0}", entityName);
            //}
            //
            //if(!string.IsNullOrEmpty( sortField))
            //{
            //    sql.AppendFormat(" ORDER BY {0} {1}", sortField, sortOrder == FieldSearchOrder.Descending ? "DESC" : "ASC");
            //}

            var sql = new StringBuilder(1024);

            sql.AppendFormat("SELECT * FROM {0}", entityName);

            if (!string.IsNullOrEmpty(sortField))
            {
                sql.AppendFormat(" ORDER BY {0} {1}", sortField, sortOrder == FieldSearchOrder.Descending ? "DESC" : "ASC");
            }
            else if (sortOrder != FieldSearchOrder.NotSearchable)
            {
                if (Entities[entityName].Fields.KeyField != null)
                {
                    sql.AppendFormat(" ORDER BY {0} {1}", Entities[entityName].Fields.KeyField.FieldName, sortOrder == FieldSearchOrder.Descending ? "DESC" : "ASC");
                }
            }

            if (fetchCount > 0)
            {
                sql.AppendFormat(" LIMIT {0}", fetchCount); // this is a SQLite thing
            }

            Select<object>(o => true).OrderBy(c => c.ToString());
            var connection = GetConnection(false);
            try
            {
                using(var command = GetNewCommandObject())
                {
                    command.Connection = connection;
                    command.CommandText = sql.ToString();

                    using(var reader = command.ExecuteReader())
                    {
                        while(reader.Read())
                        {
                            var entity = new DynamicEntity();
                            entity.EntityName = entityName;

                            // TODO: caching the ordinals would be faster
                            for(int i = 0 ; i < reader.FieldCount; i++)
                            {
                                entity.Fields.Add(reader.GetName(i), reader.GetValue(i));
                            }

                            yield return entity;
                        }
                    }
                }
            }
            finally
            {
                DoneWithConnection(connection, false);
            }
        }

        public override void Drop(string entityName)
        {
            var command = string.Format("DROP TABLE '{0}'", entityName);
            ExecuteNonQuery(command);

            Entities.Remove(entityName);
        }
    }
}
