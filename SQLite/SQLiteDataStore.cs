using System;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Linq;
using System.Data;
using System.Data.Common;
using System.Collections.Generic;
using System.Threading;
using System.Reflection;

#if ANDROID || MONO
// note the case difference between the System.Data.SQLite and Mono's implementation
using SQLiteCommand = Mono.Data.Sqlite.SqliteCommand;
using SQLiteConnection = Mono.Data.Sqlite.SqliteConnection;
using SQLiteParameter = Mono.Data.Sqlite.SqliteParameter;
using SQLiteDataReader = Mono.Data.Sqlite.SqliteDataReader;
using SQLiteTransaction = Mono.Data.Sqlite.SqliteTransaction;
#elif WINDOWS_PHONE
// ah the joys of an open-source project changing cases on us
using SQLiteConnection = Community.CsharpSqlite.SQLiteClient.SqliteConnection;
using SQLiteCommand = Community.CsharpSqlite.SQLiteClient.SqliteCommand;
using SQLiteParameter = Community.CsharpSqlite.SQLiteClient.SqliteParameter;
using SQLiteDataReader = Community.CsharpSqlite.SQLiteClient.SqliteDataReader;
using SQLiteTransaction = Community.CsharpSqlite.SQLiteClient.SqliteTransaction;
#else
using System.Data.SQLite;
#endif

namespace OpenNETCF.ORM
{
    public partial class SQLiteDataStore : SQLStoreBase<SqlEntityInfo>, IDisposable
    {
        private string m_connectionString;

        private string Password { get; set; }
        public string FileName { get; protected set; }
        
        protected SQLiteDataStore()
            : base()
        {
        }

        public SQLiteDataStore(string fileName)
            : this(fileName, null)
        {
        }

        public SQLiteDataStore(string fileName, string password)
            : this()
        {
            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentException();
            }

            FileName = fileName;
            Password = password;
        }

        public override string Name
        {
            get { return FileName; }
        }

        protected override string DefaultDateGenerator
        {
            get { return "CURRENT_TIMESTAMP"; }
        }

        public override string ConnectionString
        {
            get
            {
                if (m_connectionString == null)
                {
                    m_connectionString = string.Format("Data Source={0};", FileName);

                    if (!string.IsNullOrEmpty(Password))
                    {
                        m_connectionString += string.Format("Password={0};", Password);
                    }
                }
                return m_connectionString;
            }
        }

        protected override IDbCommand GetNewCommandObject()
        {
            return new SQLiteCommand();
        }

        protected override IDbConnection GetNewConnectionObject()
        {
            Debug.WriteLineIf(TracingEnabled, "Created new SQLiteConnection");
            return new SQLiteConnection(ConnectionString);
        }

        protected override IDataParameter CreateParameterObject(string parameterName, object parameterValue)
        {
            return new SQLiteParameter(parameterName, parameterValue);
        }

        protected override string AutoIncrementFieldIdentifier
        {
            get { return "AUTOINCREMENT"; }
        }

        public override void CreateStore()
        {
            if (StoreExists)
            {
                throw new StoreAlreadyExistsException();
            }

#if(!WINDOWS_PHONE)
            SQLiteConnection.CreateFile(FileName);
#endif
            var connection = GetConnection(true);
            try
            {
                foreach (var entity in this.Entities)
                {
                    CreateTable(connection, entity);
                }
            }
            finally
            {
                DoneWithConnection(connection, true);
            }
        }

        public override void DeleteStore()
        {
            if (StoreExists)
            {
                File.Delete(FileName);
            }
        }

        public override bool StoreExists
        {
            get { return File.Exists(FileName); }
        }

        protected override string ParameterPrefix
        {
            get { return "@"; }
        }

        private SQLiteCommand GetInsertCommand(string entityName)
        {
            // TODO: support command caching to improve bulk insert speeds
            //       simply use a dictionary keyed by entityname
            var keyScheme = Entities[entityName].EntityAttribute.KeyScheme;
            var insertCommand = new SQLiteCommand();
            
            var sbFields = new StringBuilder(string.Format("INSERT INTO {0} (", entityName));
            var sbParams = new StringBuilder( " VALUES (");

            foreach (var field in Entities[entityName].Fields)
            {
                // skip auto-increments
                if ((field.IsPrimaryKey) && (keyScheme == KeyScheme.Identity))
                {
                    continue;
                }
                sbFields.Append(field.FieldName + ",");
                sbParams.Append(ParameterPrefix + field.FieldName + ",");

                insertCommand.Parameters.Add(new SQLiteParameter(ParameterPrefix + field.FieldName, field.DataType));
            }

            // replace trailing commas
            sbFields[sbFields.Length - 1] = ')';
            sbParams[sbParams.Length - 1] = ')';

            insertCommand.CommandText = sbFields.ToString() + sbParams.ToString();

            return insertCommand;
        }
        
        /// <summary>
        /// Inserts the provided entity instance into the underlying data store.
        /// </summary>
        /// <param name="item"></param>
        /// <remarks>
        /// If the entity has an identity field, calling Insert will populate that field with the identity vale vefore returning
        /// </remarks>
        public override void OnInsert(object item, bool insertReferences)
        {
            if (item is DynamicEntity)
            {
                OnInsertDynamicEntity(item as DynamicEntity, insertReferences);
                return;
            }

            var itemType = item.GetType();
            string entityName = m_entities.GetNameForType(itemType);
            var keyScheme = Entities[entityName].EntityAttribute.KeyScheme;

            if (entityName == null)
            {
                throw new EntityNotFoundException(item.GetType());
            }

            // ---------- Handle N:1 References -------------
            if (insertReferences)
            {
                DoInsertReferences(item, entityName, keyScheme, true);
            }

            var connection = GetConnection(false);
            try
            {
                FieldAttribute identity = null;
                var command = GetInsertCommand(entityName);
                command.Connection = connection as SQLiteConnection;
                command.Transaction = CurrentTransaction as SQLiteTransaction;

                // TODO: fill the parameters
                foreach (var field in Entities[entityName].Fields)
                {
                    if ((field.IsPrimaryKey) && (keyScheme == KeyScheme.Identity))
                    {
                        identity = field;
                        continue;
                    }
                    else if (field.DataType == DbType.Object)
                    {
                        // get serializer
                        var serializer = GetSerializer(itemType);

                        if (serializer == null)
                        {
                            throw new MissingMethodException(
                                string.Format("The field '{0}' requires a custom serializer/deserializer method pair in the '{1}' Entity",
                                field.FieldName, entityName));
                        }
                        var value = serializer.Invoke(item, new object[] { field.FieldName });
                        if (value == null)
                        {
                            command.Parameters[ParameterPrefix + field.FieldName].Value = DBNull.Value;
                        }
                        else
                        {
                            command.Parameters[ParameterPrefix + field.FieldName].Value = value;
                        }
                    }
                    else if (field.DataType == DbType.DateTime)
                    {
                        var dtValue = GetInstanceValue(field, item);

                        if (dtValue.Equals(DateTime.MinValue) &&
                            ((field.AllowsNulls) || (field.DefaultType == DefaultType.CurrentDateTime)))
                        {
                            // testing of just letting the null fall through is setting the field to null, not using the default
                            // so we'll set it manually
                            dtValue = DateTime.Now;
                        }
                        command.Parameters[ParameterPrefix + field.FieldName].Value = dtValue;
                    }
                    else if (field.IsRowVersion)
                    {
                        // read-only, so do nothing
                    }
                    else if (field.PropertyInfo.PropertyType.UnderlyingTypeIs<TimeSpan>())
                    {
                        // SQLite doesn't support "timespan" - and date/time must be stored as text, real or 64-bit integer (see the SQLite docs for more details)
                        // here we'll store TimeSpans (since they can be negative) as an offset from a base date
                        var value = field.PropertyInfo.GetValue(item, null);

                        if (value == null)
                        {
                            command.Parameters[ParameterPrefix + field.FieldName].Value = DBNull.Value;
                        }
                        else
                        {
                            var storeTime = new DateTime(1980, 1, 1) + (TimeSpan)value;
                            command.Parameters[ParameterPrefix + field.FieldName].Value = storeTime;
                        }
                    }
                    else
                    {
                        var value = field.PropertyInfo.GetValue(item, null);
                        if ((value == null) && (field.DefaultValue != null))
                        {
                            command.Parameters[ParameterPrefix + field.FieldName].Value = field.DefaultValue;
                        }
                        else
                        {
                            command.Parameters[ParameterPrefix + field.FieldName].Value = value;
                        }
                    }
                }

                command.ExecuteNonQuery();

                // did we have an identity field?  If so, we need to update that value in the item
                if (identity != null)
                {
                    var id = GetIdentity(connection);
                    identity.PropertyInfo.SetValue(item, id, null);
                }

                if (insertReferences)
                {
                    // ---------- Handle 1:N References -------------
                    DoInsertReferences(item, entityName, keyScheme, false);
                }
            }
            finally
            {
                DoneWithConnection(connection, false);
            }
        }

        public override void CompactDatabase()
        {
            var connection = GetConnection(true);
            try
            {
                string sql = "VACUUM";

                using (var command = GetNewCommandObject())
                {
                    command.CommandText = sql;
                    command.Connection = connection;
                    command.Transaction = CurrentTransaction;
                    command.ExecuteNonQuery();
                }
            }
            finally
            {
                DoneWithConnection(connection, true);
            }
        }

        private int GetIdentity(IDbConnection connection)
        {
            using (var command = new SQLiteCommand("SELECT last_insert_rowid()", connection as SQLiteConnection))
            {
                object id = command.ExecuteScalar();
                return Convert.ToInt32(id);
            }
        }

        protected override void GetPrimaryKeyInfo(string entityName, out string indexName, out string columnName)
        {
            var connection = GetConnection(true);
            try
            {
                indexName = string.Empty;
                columnName = string.Empty;

                string sql = string.Format("PRAGMA table_info({0})", entityName);

                using (var command = GetNewCommandObject())
                {
                    command.CommandText = sql;
                    command.Connection = connection;
                    command.Transaction = CurrentTransaction;
                    using (var reader = command.ExecuteReader() as SQLiteDataReader)
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                // pk column is #5
                                if (Convert.ToInt32(reader[5]) != 0)
                                {
                                    columnName = reader[1] as string;
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                DoneWithConnection(connection, true);
            }
        }

        private void UpdateIndexCacheForType(string entityName)
        {
            if (!TableExists(entityName)) return;

            if (Entities.Contains(entityName))
            {
                // have we already cached this?
                if (((SqlEntityInfo)Entities[entityName]).IndexNames != null) return;
            }
            else
            {
                return;
            }

            // get all iindex names for the type
            var connection = GetConnection(true);
            try
            {
                string sql = string.Format("SELECT name FROM sqlite_master WHERE (tbl_name = '{0}')", entityName);

                using (var command = GetNewCommandObject())
                {
                    command.Connection = connection;
                    command.CommandText = sql;
                    command.Transaction = CurrentTransaction;
                    using (var reader = command.ExecuteReader())
                    {
                        List<string> nameList = new List<string>();

                        while (reader.Read())
                        {
                            nameList.Add(reader.GetString(0));
                        }

                        ((SqlEntityInfo)Entities[entityName]).IndexNames = nameList;
                    }
                }
            }
            finally
            {
                DoneWithConnection(connection, true);
            }
        }

        public override string[] GetTableNames()
        {
            var names = new List<string>();

            var connection = GetConnection(true);
            try
            {
                using (var command = GetNewCommandObject())
                {
                    if(this.CurrentTransaction != null)
                    {
                        command.Transaction = this.CurrentTransaction;
                    }
                    command.Connection = connection;
                    var sql = "SELECT name FROM sqlite_master WHERE type = 'table'";
                    command.CommandText = sql;
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var n = reader.GetString(0);
                            if (n == "sqlite_sequence") continue;
                            names.Add(n);
                        }
                    }

                    return names.ToArray();
                }
            }
            finally
            {
                DoneWithConnection(connection, true);
            }
        }

        public override bool TableExists(string tableName)
        {
            var connection = GetConnection(true);
            try
            {
                using (var command = GetNewCommandObject())
                {
                    command.Connection = connection;
                    var sql = string.Format("SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = '{0}'", tableName);
                    command.CommandText = sql;
                    command.Transaction = CurrentTransaction;
                    var count = Convert.ToInt32(command.ExecuteScalar());

                    return (count > 0);
                }
            }
            finally
            {
                DoneWithConnection(connection, true);
            }
        }

        protected override void ValidateTable(IDbConnection connection, IEntityInfo entity)
        {
            // first make sure the table exists
            if (!TableExists(entity.EntityAttribute.NameInStore))
            {
                CreateTable(connection, entity);
                return;
            }

            var fieldData = new List< object[]>();

            using (var command = new SQLiteCommand())
            {
                command.Connection = connection as SQLiteConnection;
                command.CommandText = string.Format("PRAGMA table_info({0})", entity.EntityName);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var values = new object[6];
                        reader.GetValues(values);
                        fieldData.Add(values);
                    }
                }
            }

            foreach(var field in entity.Fields)
            {
                // 0 = cid (column id)
                // 1 = name
                // 2 = type
                // 3 = notnull
                // 4 = dflt_value
                // 5 = pk

                var existing = fieldData.FirstOrDefault(f => string.Compare(f[1].ToString(), field.FieldName, true) == 0);

                if (existing == null)
                {
                    // field doesn't exist - we must create it
                    var alter = new StringBuilder(string.Format("ALTER TABLE {0} ", entity.EntityAttribute.NameInStore));
                    alter.Append(string.Format("ADD [{0}] {1} {2}",
                        field.FieldName,
                        GetFieldDataTypeString(entity.EntityName, field),
                        GetFieldCreationAttributes(entity.EntityAttribute, field)));

                    using (var command = new SQLiteCommand(alter.ToString(), connection as SQLiteConnection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
                else
                {
                    // TODO: verify field length, etc.
                }
            }

            return;

        }

        protected override string VerifyIndex(string entityName, string fieldName, FieldSearchOrder searchOrder, IDbConnection connection)
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

                    var sql = string.Format("SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name = '{0}'", indexName);
                    command.CommandText = sql;

                    var i = (long)command.ExecuteScalar();

                    if (i == 0)
                    {
                        sql = string.Format("CREATE INDEX {0} ON {1}({2} {3})",
                            indexName,
                            entityName,
                            fieldName,
                            searchOrder == FieldSearchOrder.Descending ? "DESC" : string.Empty);

                        Debug.WriteLine(sql);

                        command.CommandText = sql;
                        command.Transaction = CurrentTransaction;
                        command.ExecuteNonQuery();
                    }

                    var indexinfo = new IndexInfo
                    {
                        Name = indexName,
                        MaxCharLength = -1
                    };

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

        protected override IEnumerable<object> Select(Type objectType, IEnumerable<FilterCondition> filters, int fetchCount, int firstRowOffset, bool fillReferences)
        {
            string entityName = m_entities.GetNameForType(objectType);

            if (entityName == null)
            {
                throw new EntityNotFoundException(objectType);
            }

            return Select(entityName, objectType, filters, fetchCount, firstRowOffset, fillReferences);
        }

        protected override string GetLimitSubCommand(int fetchCount)
        {
            if (fetchCount > 0)
            {
                return string.Format("LIMIT " + fetchCount);
            }

            return string.Empty;
        }

        private IEnumerable<object> Select(string entityName, Type objectType, IEnumerable<FilterCondition> filters, int fetchCount, int firstRowOffset, bool fillReferences)
        {
            Debug.WriteLineIf(TracingEnabled, "+Select");
            if (entityName == null)
            {
                throw new EntityNotFoundException(objectType);
            }

            if (!Entities.Contains(entityName))
            {
                if(DiscoverDynamicEntity(entityName) == null) yield return null;
            }

            UpdateIndexCacheForType(entityName);

            var items = new List<object>();

            var connection = GetConnection(false);
            SQLiteCommand command = null;

            try
            {
                CheckOrdinals(entityName);
                command = BuildFilterCommand<SQLiteCommand, SQLiteParameter>(entityName, filters, fetchCount, false);
                command.Connection = connection as SQLiteConnection;
                command.Transaction = CurrentTransaction as SQLiteTransaction;

                int searchOrdinal = -1;
            //    ResultSetOptions options = ResultSetOptions.Scrollable;

                object matchValue = null;
                string matchField = null;

            // TODO: we need to ensure that the search value does not exceed the length of the indexed
            // field, else we'll get an exception on the Seek call below (see the SQL CE implementation)

                using (var results = command.ExecuteReader(CommandBehavior.SingleResult))
                {
                    if (results.HasRows)
                    {
                        ReferenceAttribute[] referenceFields = null;

                        int currentOffset = 0;

                        if (matchValue != null)
                        {
                            // convert enums to an int, else the .Equals later check will fail
                            // this feels a bit kludgey, but for now it's all I can think of
                            if (matchValue.GetType().IsEnum)
                            {
                                matchValue = (int)matchValue;
                            }

                            if (searchOrdinal < 0)
                            {
                                searchOrdinal = results.GetOrdinal(matchField);
                            }
                        }

                        while (results.Read())
                        {
                            if (currentOffset < firstRowOffset)
                            {
                                currentOffset++;
                                continue;
                            }

                            // autofill references if desired
                            if (referenceFields == null)
                            {
                                referenceFields = Entities[entityName].References.ToArray();
                            }

                            bool fieldsSet;
                            object item = CreateEntityInstance(entityName, objectType, Entities[entityName].Fields, results, out fieldsSet);

                            object rowPK = null;

                            if(!fieldsSet)
                            {
                                foreach (var field in Entities[entityName].Fields)
                                {
                                    MethodInfo mi = null;

                                    if (!field.PropertyInfo.CanWrite)
                                    {
                                        // get a private accessor?
                                        mi = field.PropertyInfo.GetSetMethod(true);

                                        // not settable, so skip
                                        if (mi == null) continue;
                                    }

                                    var value = results[field.Ordinal];
                                    if (value != DBNull.Value)
                                    {
                                        if (field.DataType == DbType.Object)
                                        {
                                            if (fillReferences)
                                            {
                                                // get serializer
                                                var itemType = item.GetType();
                                                var deserializer = GetDeserializer(itemType);

                                                if (deserializer == null)
                                                {
                                                    throw new MissingMethodException(
                                                        string.Format("The field '{0}' requires a custom serializer/deserializer method pair in the '{1}' Entity",
                                                        field.FieldName, entityName));
                                                }

                                                var @object = deserializer.Invoke(item, new object[] { field.FieldName, value });
                                                field.PropertyInfo.SetValue(item, @object, null);
                                            }
                                        }
                                        else if (field.IsRowVersion)
                                        {
                                            // sql stores this an 8-byte array
                                            field.PropertyInfo.SetValue(item, BitConverter.ToInt64((byte[])value, 0), null);
                                        }
                                        else if (field.IsTimespan)
                                        {
                                            // SQLite doesn't support "timespan" - and date/time must be stored as text, real or 64-bit integer (see the SQLite docs for more details)
                                            // here we'll pull TimeSpans (since they can be negative) as an offset from a base date
                                            var storeDate = (DateTime)value;
                                            var storeTime = storeDate - new DateTime(1980, 1, 1);
                                            field.PropertyInfo.SetValue(item, storeTime, null);
                                        }
                                        else if (field.DataType == DbType.DateTime)
                                        {
                                            field.PropertyInfo.SetValue(item, Convert.ToDateTime(value), null);
                                        }
                                        else if ((field.IsPrimaryKey) && (value is Int64))
                                        {
                                            if (field.PropertyInfo.PropertyType.Equals(typeof(int)))
                                            {
                                                // SQLite automatically makes auto-increment fields 64-bit, so this works around that behavior
                                                field.PropertyInfo.SetValue(item, Convert.ToInt32(value), null);
                                            }
                                            else
                                            {
                                                field.PropertyInfo.SetValue(item, Convert.ToInt64(value), null);
                                            }
                                        }
                                        else if ((value is Int64) || (value is double))
                                        {
                                            // SQLite is "interesting" in that its 'integer' has a strong affinity toward 64-bit, so int and uint properties
                                            // end up as 64-bit fields.  Decimals have a strong affinity toward 'double', so float properties
                                            // end up as 'double'. Even more fun is that a decimal value '0' will come back as an int64

                                            // When we query those back, we must convert to put them into the property or we crash hard
                                            object setval;

                                            if (field.PropertyInfo.PropertyType.Equals(typeof(UInt32)))
                                            {
                                                setval = Convert.ToUInt32(value);
                                            }
                                            else if ((field.PropertyInfo.PropertyType.Equals(typeof(Int32))) || (field.PropertyInfo.PropertyType.Equals(typeof(Int32?))))
                                            {
                                                setval = Convert.ToInt32(value);
                                            }
                                            else if (field.PropertyInfo.PropertyType.Equals(typeof(decimal)))
                                            {
                                                setval = Convert.ToDecimal(value);
                                            }
                                            else if (field.PropertyInfo.PropertyType.Equals(typeof(float)))
                                            {
                                                setval = Convert.ToSingle(value);
                                            }
                                            else if (field.PropertyInfo.PropertyType.IsEnum)
                                            {
                                                setval = Enum.ToObject(field.PropertyInfo.PropertyType, value);
                                            }
                                            else
                                            {
                                                setval = value;
                                            }

                                            if (mi == null)
                                            {
                                                field.PropertyInfo.SetValue(item, setval, null);
                                            }
                                            else
                                            {
                                                mi.Invoke(setval, null);
                                            }
                                        }
                                        else
                                        {
                                            var t = value.GetType();
                                            field.PropertyInfo.SetValue(item, value, null);
                                        }
                                    }

                                    if (field.IsPrimaryKey)
                                    {
                                        rowPK = value;
                                    }
                                }
                            }

                            if ((fillReferences) && (referenceFields.Length > 0))
                            {
                                //FillReferences(item, rowPK, referenceFields, true);
                                FillReferences(item, rowPK, referenceFields, false);
                            }

                            // changed from
                            // items.Add(item);
                            yield return item;

                            if ((fetchCount > 0) && (items.Count >= fetchCount))
                            {
                                break;
                            }
                        }
                    }
                }
            }
            finally
            {
                if ((!UseCommandCache) && (command != null))
                {
                    command.Dispose();
                }

                if (UseCommandCache)
                {
                    Monitor.Exit(CommandCache);
                }

                FlushReferenceTableCache();
                DoneWithConnection(connection, false);
            }

            Debug.WriteLineIf(TracingEnabled, "-Select");
        }
        
        public override void OnUpdate(object item, bool cascadeUpdates, string fieldName)
        {
            if (item is DynamicEntity)
            {
                OnUpdateDynamicEntity(item as DynamicEntity);
                return;
            }

            object keyValue;
            var changeDetected = false;
            var itemType = item.GetType();
            string entityName = m_entities.GetNameForType(itemType);
            var insertCommand = GetNewCommandObject();
            var updateSQL = new StringBuilder(string.Format("UPDATE {0} SET ", entityName));

            if (entityName == null)
            {
                throw new EntityNotFoundException(itemType);
            }

            if (Entities[entityName].Fields.KeyField == null)
            {
                throw new PrimaryKeyRequiredException("A primary key is required on an Entity in order to perform Updates");
            }

            var connection = GetConnection(false);
            try
            {
                CheckOrdinals(entityName);
                CheckPrimaryKeyIndex(entityName);

                using (var command = GetNewCommandObject())
                {
                    keyValue = Entities[entityName].Fields.KeyField.PropertyInfo.GetValue(item, null);

                    command.Connection = connection;

                    command.CommandText = string.Format("SELECT * FROM {0} WHERE [{1}] = {2}keyparam",
                        entityName,
                        Entities[entityName].Fields.KeyField.FieldName,
                        ParameterPrefix);

                    command.CommandType = CommandType.Text;
                    command.Parameters.Add(new SQLiteParameter(ParameterPrefix + "keyparam", keyValue));
                    command.Transaction = CurrentTransaction;

                    using (var reader = command.ExecuteReader() as SQLiteDataReader)
                    {

                        if (!reader.HasRows)
                        {
                            // TODO: the PK value has changed - we need to store the original value in the entity or diallow this kind of change
                            throw new RecordNotFoundException("Cannot locate a record with the provided primary key.  You cannot update a primary key value through the Update method");
                        }

                        reader.Read();

                        // update the values
                        foreach (var field in Entities[entityName].Fields)
                        {
                            // do not update PK fields
                            if (field.IsPrimaryKey)
                            {
                                continue;
                            }
                            else if (fieldName != null && field.FieldName != fieldName)
                            {
                                continue; // if we pass in a field name, skip over any fields that don't match
                            }
                            else if (field.IsRowVersion)
                            {
                                // read-only, so do nothing
                            }
                            else if (field.DataType == DbType.Object)
                            {
                                changeDetected = true;
                                // get serializer
                                var serializer = GetSerializer(itemType);

                                if (serializer == null)
                                {
                                    throw new MissingMethodException(
                                        string.Format("The field '{0}' requires a custom serializer/deserializer method pair in the '{1}' Entity",
                                        field.FieldName, entityName));
                                }
                                var value = serializer.Invoke(item, new object[] { field.FieldName });

                                if (value == null)
                                {
                                    updateSQL.AppendFormat("{0}=NULL, ", field.FieldName);
                                }
                                else
                                {
                                    updateSQL.AppendFormat("{0}={1}{0}, ", field.FieldName, ParameterPrefix);
                                    insertCommand.Parameters.Add(new SQLiteParameter(ParameterPrefix + field.FieldName, value));
                                }
                            }
                            else if (field.PropertyInfo.PropertyType.UnderlyingTypeIs<TimeSpan>())
                            {
                                changeDetected = true;
                                // SQL Compact doesn't support Time, so we're convert to ticks in both directions
                                var value = field.PropertyInfo.GetValue(item, null);
                                if (value == null)
                                {
                                    updateSQL.AppendFormat("{0}=NULL, ", field.FieldName);
                                }
                                else
                                {
                                    var ticks = ((TimeSpan)value).Ticks;
                                    updateSQL.AppendFormat("{0}={1}{0}, ", field.FieldName, ParameterPrefix);
                                    insertCommand.Parameters.Add(new SQLiteParameter(ParameterPrefix + field.FieldName, ticks));
                                }
                            }
                            else
                            {
                                var value = field.PropertyInfo.GetValue(item, null);

                                if (reader[field.FieldName] != value)
                                {
                                    changeDetected = true;

                                    if (value == null)
                                    {
                                        updateSQL.AppendFormat("{0}=NULL, ", field.FieldName);
                                    }
                                    else
                                    {
                                        updateSQL.AppendFormat("{0}={1}{0}, ", field.FieldName, ParameterPrefix);
                                        insertCommand.Parameters.Add(new SQLiteParameter(ParameterPrefix + field.FieldName, value));
                                    }
                                }
                            }
                        } // foreach
                    } // execute reader
                } // using command
            } // try
            finally
            {
                DoneWithConnection(connection, false);
            }

            // only execute if a change occurred
            if (changeDetected)
            {
                connection = GetConnection(false);

                try
                {
                    // remove the trailing comma and append the filter
                    updateSQL.Length -= 2;
                    updateSQL.AppendFormat(" WHERE {0} = {1}keyparam", Entities[entityName].Fields.KeyField.FieldName, ParameterPrefix);
                    insertCommand.Parameters.Add(new SQLiteParameter(ParameterPrefix + "keyparam", keyValue));
                    insertCommand.CommandText = updateSQL.ToString();
                    insertCommand.Connection = connection;
                    insertCommand.Transaction = CurrentTransaction;
                    insertCommand.ExecuteNonQuery();
                }
                finally
                {
                    DoneWithConnection(connection, false);
                }
            }

            if (cascadeUpdates)
            {
                // TODO: move this into the base DataStore class as it's not SqlCe-specific
                foreach (var reference in Entities[entityName].References)
                {
                    var itemList = reference.PropertyInfo.GetValue(item, null) as Array;
                    if (itemList != null)
                    {
                        foreach (var refItem in itemList)
                        {
                            if (!this.Contains(refItem))
                            {
                                var foreignKey = refItem.GetType().GetProperty(reference.ForeignReferenceField, BindingFlags.Instance | BindingFlags.Public);
                                foreignKey.SetValue(refItem, keyValue, null);
                                Insert(refItem, false);
                            }
                            else
                            {
                                Update(refItem, true, fieldName);
                            }
                        }
                    }
                }
            }
        }

        public override int Count<T>(IEnumerable<FilterCondition> filters)
        {
            var t = typeof(T);
            string entityName = m_entities.GetNameForType(t);

            if (entityName == null)
            {
                throw new EntityNotFoundException(t);
            }

            var connection = GetConnection(true);
            try
            {
                using (var command = BuildFilterCommand<SQLiteCommand, SQLiteParameter>(entityName, filters, true))
                {
                    command.Connection = connection as SQLiteConnection;
                    return (int)command.ExecuteScalar();
                }
            }
            finally
            {
                DoneWithConnection(connection, true);
            }
        }

        protected override string GetFieldDataTypeString(string entityName, FieldAttribute field)
        {
            // a SQLite Int64 auto-increment key requires being called "INTEGER", not "BIGINT"
            if(field.IsPrimaryKey && (field.DataType == DbType.Int64))
            {
                if (GetEntityInfo(entityName).EntityAttribute.KeyScheme == KeyScheme.Identity)
                {
                    return "INTEGER";
                }
            }

            return base.GetFieldDataTypeString(entityName, field);
        }
    }
}
