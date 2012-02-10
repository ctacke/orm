using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Data;
using System.Data.Common;
using System.Reflection;

namespace OpenNETCF.ORM
{
    public abstract class SQLStoreBase<TEntityInfo> : DataStore<TEntityInfo>, IDisposable
        where TEntityInfo : EntityInfo, new()
    {
        private List<IndexInfo> m_indexNameCache = new List<IndexInfo>();
        private DbConnection m_connection;
        private Dictionary<Type, MethodInfo> m_serializerCache = new Dictionary<Type, MethodInfo>();
        private Dictionary<Type, MethodInfo> m_deserializerCache = new Dictionary<Type, MethodInfo>();

        public int DefaultStringFieldSize { get; set; }
        public int DefaultNumericFieldPrecision { get; set; }
        public int DefaultVarBinaryLength { get; set; }
        protected abstract string AutoIncrementFieldIdentifier { get; }

        public ConnectionBehavior ConnectionBehavior { get; set; }

        public abstract override void CreateStore();
        public abstract override void DeleteStore();
        public abstract override void EnsureCompatibility();

        public abstract override bool StoreExists { get; }


        public abstract override void Insert(object item, bool insertReferences);

        public abstract override T[] Select<T>();
        public abstract override T[] Select<T>(bool fillReferences);
        public abstract override T Select<T>(object primaryKey);
        public abstract override T Select<T>(object primaryKey, bool fillReferences);
        public abstract override T[] Select<T>(string searchFieldName, object matchValue);
        public abstract override T[] Select<T>(string searchFieldName, object matchValue, bool fillReferences);
        public abstract override T[] Select<T>(IEnumerable<FilterCondition> filters);
        public abstract override T[] Select<T>(IEnumerable<FilterCondition> filters, bool fillReferences);
        public abstract override object[] Select(Type entityType);
        public abstract override object[] Select(Type entityType, bool fillReferences);

        public abstract override void Update(object item);
        public abstract override void Update(object item, bool cascadeUpdates, string fieldName);
        public abstract override void Update(object item, string fieldName);

        public abstract override void Delete(object item);
        public abstract override void Delete<T>(object primaryKey);

        public abstract override void FillReferences(object instance);

        public abstract override T[] Fetch<T>(int fetchCount);
        public abstract override T[] Fetch<T>(int fetchCount, int firstRowOffset);
        public abstract override T[] Fetch<T>(int fetchCount, int firstRowOffset, string sortField);
        public abstract override T[] Fetch<T>(int fetchCount, int firstRowOffset, string sortField, FieldSearchOrder sortOrder, FilterCondition filter, bool fillReferences);

        public abstract override int Count<T>();
        public abstract override int Count<T>(IEnumerable<FilterCondition> filters);

        public abstract override void Delete<T>();
        public abstract override void Delete<T>(string fieldName, object matchValue);

        public abstract override bool Contains(object item);

        protected abstract DbCommand GetNewCommandObject();
        protected abstract DbConnection GetNewConnectionObject();

        public SQLStoreBase()
        {
            DefaultStringFieldSize = 200;
            DefaultNumericFieldPrecision = 16;
            DefaultVarBinaryLength = 8000;
        }

        ~SQLStoreBase()
        {
            Dispose();
        }

        public virtual void Dispose()
        {
            if (m_connection != null)
            {
                m_connection.Dispose();
            }

            GC.SuppressFinalize(this);
        }

        protected virtual DbConnection GetConnection(bool maintenance)
        {
            switch (ConnectionBehavior)
            {
                case ConnectionBehavior.AlwaysNew:
                    var connection = GetNewConnectionObject();
                    connection.Open();
                    return connection;
                case ConnectionBehavior.HoldMaintenance:
                    if (m_connection == null)
                    {
                        m_connection = GetNewConnectionObject();
                        m_connection.Open();
                    }
                    if (maintenance) return m_connection;
                    var connection2 = GetNewConnectionObject();
                    connection2.Open();
                    return connection2;
                case ConnectionBehavior.Persistent:
                    if (m_connection == null)
                    {
                        m_connection = GetNewConnectionObject();
                        m_connection.Open();
                    }
                    return m_connection;
                default:
                    throw new NotSupportedException();
            }
        }

        protected virtual void DoneWithConnection(DbConnection connection, bool maintenance)
        {
            switch (ConnectionBehavior)
            {
                case ConnectionBehavior.AlwaysNew:
                    connection.Close();
                    connection.Dispose();
                    break;
                case ConnectionBehavior.HoldMaintenance:
                    if (maintenance) return;
                    connection.Close();
                    connection.Dispose();
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
            var connection = GetConnection(false);
            try
            {
                using (var command = GetNewCommandObject())
                {
                    command.Connection = connection;
                    command.CommandText = sql;
                    return command.ExecuteScalar();
                }
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

        protected virtual void CreateTable(DbConnection connection, EntityInfo entity)
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

                sql.AppendFormat("[{0}] {1} {2}",
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
                int i = command.ExecuteNonQuery();
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

        protected virtual string VerifyIndex(string entityName, string fieldName, FieldSearchOrder searchOrder, DbConnection connection)
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
                        sql = string.Format("CREATE INDEX {0} ON {1}({2} {3})",
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
                if (field.Length >= 8000)
                {
                    return "image";
                }
                // if no length was supplied, default to DefaultVarBinaryLength (8000)
                return string.Format("varbinary({0})", field.Length == 0 ? DefaultVarBinaryLength : field.Length);
            }

            return field.DataType.ToSqlTypeString();
        }

        protected virtual string GetFieldCreationAttributes(EntityAttribute attribute, FieldAttribute field)
        {
            StringBuilder sb = new StringBuilder();

            switch (field.DataType)
            {
                case DbType.String:
                    if (field.Length > 0)
                    {
                        sb.AppendFormat("({0}) ", field.Length);
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

            if (field.Default != null)
            {
                sb.AppendFormat("DEFAULT {0} ", field.Default.GetDefaultValue());
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
    }
}
