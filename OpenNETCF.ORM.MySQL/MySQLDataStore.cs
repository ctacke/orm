using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using MySql.Data.MySqlClient;

namespace OpenNETCF.ORM
{    
    public partial class MySQLDataStore : SQLStoreBase<SqlEntityInfo>, IDisposable
    {
        private string m_lastEntity;
        private string m_connectionString;
        private MySQLConnectionInfo m_info;

        public MySQLDataStore(MySQLConnectionInfo info)
        {
            m_info = info;
        }

        private string BuildConnectionString(MySQLConnectionInfo info, bool useSystem)
        {

            var cs = string.Format(
                "server={0};" +
                "port={1};" +
                "user id={2};" + 
                "password={3};" + 
                "database={4};" +
                "pooling=false",
                     info.ServerAddress,
                     info.ServerPort,
                     info.UserName,
                     info.Password,
                     useSystem ? "mysql" : info.DatabaseName,
                     info.Password);

            return cs;
        }

        private string ConnectionString
        {
            get
            {
                if (m_connectionString == null)
                {
                    m_connectionString = BuildConnectionString(m_info, false);
                }
                return m_connectionString;
            }
        }

        public override bool StoreExists
        {
            get
            {
                var cs = BuildConnectionString(m_info, true);
                using (var connection = GetNewConnectionObject(cs))
                {
                    connection.Open();

                    using (var command = GetNewCommandObject())
                    {
                        command.Transaction = CurrentTransaction;
                        command.Connection = connection;
                        var sql = "SHOW DATABASES";
                        command.CommandText = sql;
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                if (string.Compare(reader.GetString(0), m_info.DatabaseName, true) == 0)
                                {
                                    return true;
                                }
                            }
                        }

                        return false;
                    }
                }
            }
        }

        protected override string AutoIncrementFieldIdentifier
        {
            get { return "AUTO_INCREMENT"; }
        }

        public override void CreateStore()
        {
            var cs = BuildConnectionString(m_info, true);
            using (var connection = GetNewConnectionObject(cs))
            {
                connection.Open();
                using (var command = GetNewCommandObject())
                {
                    command.Transaction = CurrentTransaction;
                    command.Connection = connection;
                    var sql = string.Format("CREATE DATABASE IF NOT EXISTS {0}", m_info.DatabaseName); ;
                    command.CommandText = sql;
                    command.ExecuteNonQuery();
                }
            }
        }

        public override void DeleteStore()
        {
            var cs = BuildConnectionString(m_info, true);
            using (var connection = GetNewConnectionObject(cs))
            {
                connection.Open();
                using (var command = GetNewCommandObject())
                {
                    command.Transaction = CurrentTransaction;
                    command.Connection = connection;
                    var sql = string.Format("DROP DATABASE {0}", m_info.DatabaseName); ;
                    command.CommandText = sql;
                    command.ExecuteNonQuery();

                    m_connectionString = null;
                }
            }
        }

        protected override string ParameterPrefix
        {
            get { return "@"; }
        }

        private MySqlDbType TranslateDbTypeToMySqlDbType(DbType type)
        {
            switch (type)
            {
                case DbType.String:
                case DbType.StringFixedLength:
                    return MySqlDbType.String;
                case DbType.Int64:
                    return MySqlDbType.Int64;
                case DbType.UInt64:
                    return MySqlDbType.UInt64;
                case DbType.Int32:
                    return MySqlDbType.Int32;
                case DbType.UInt32:
                    return MySqlDbType.UInt64;
                case DbType.Int16:
                    return MySqlDbType.Int16;
                case DbType.UInt16:
                    return MySqlDbType.UInt64;
                case DbType.Decimal:
                    return MySqlDbType.Decimal;
                case DbType.Single:
                    return MySqlDbType.Float;
                case DbType.Double:
                    return MySqlDbType.Double;
                case DbType.DateTime:
                    return MySqlDbType.DateTime;
                case DbType.Binary:
                    return MySqlDbType.Blob;
                case DbType.Guid:
                    return MySqlDbType.Binary;
                default:
                    throw new NotSupportedException(string.Format("Cannot translate DbType '{0}' to OracleDbType", type.ToString()));
            }
        }

        private MySqlCommand GetInsertCommand(string entityName)
        {
            // TODO: support command caching to improve bulk insert speeds
            //       simply use a dictionary keyed by entityname
            var keyScheme = Entities[entityName].EntityAttribute.KeyScheme;
            var insertCommand = new MySqlCommand();

            var sbFields = new StringBuilder(string.Format("INSERT INTO {0} (", entityName));
            var sbParams = new StringBuilder(" VALUES (");

            foreach (var field in Entities[entityName].Fields)
            {
                // skip auto-increments
                if ((field.IsPrimaryKey) && (keyScheme == KeyScheme.Identity))
                {
                    continue;
                }
                sbFields.Append(field.FieldName + ",");
                sbParams.Append(ParameterPrefix + field.FieldName + ",");

                var parameter = new MySqlParameter(ParameterPrefix + field.FieldName, TranslateDbTypeToMySqlDbType(field.DataType));
                if (field.DataType == DbType.Guid)
                {
                    parameter.Size = 16;
                }
                insertCommand.Parameters.Add(parameter);
            }

            // replace trailing commas
            sbFields[sbFields.Length - 1] = ')';
            sbParams[sbParams.Length - 1] = ')';

            insertCommand.CommandText = sbFields.ToString() + sbParams.ToString();

            return insertCommand;
        }

        public override void OnInsert(object item, bool insertReferences)
        {
            if (item is DynamicEntity)
            {
                throw new NotSupportedException();
                //OnInsertDynamicEntity(item as DynamicEntity, insertReferences);
                return;
            }

            string entityName;
            var itemType = item.GetType();
            entityName = m_entities.GetNameForType(itemType);

            if (entityName == null)
            {
                throw new EntityNotFoundException(item.GetType());
            }

            var keyScheme = Entities[entityName].EntityAttribute.KeyScheme;
            // ---------- Handle N:1 References -------------
            if (insertReferences)
            {
                DoInsertReferences(item, entityName, keyScheme, true);
            }

            var connection = GetConnection(false);
            try
            {
                FieldAttribute identity = null;
                keyScheme = Entities[entityName].EntityAttribute.KeyScheme;
                var command = GetInsertCommand(entityName);
                command.Connection = connection as MySqlConnection;
                command.Transaction = CurrentTransaction as MySqlTransaction;

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
                    else if (field.PropertyInfo.PropertyType.UnderlyingTypeIs<TimeSpan>())
                    {
                        var value = field.PropertyInfo.GetValue(item, null);

                        if (value == null)
                        {
                            command.Parameters[ParameterPrefix + field.FieldName].Value = DBNull.Value;
                        }
                        else
                        {
                            var timespanTicks = ((TimeSpan)value).Ticks;
                            command.Parameters[ParameterPrefix + field.FieldName].Value = timespanTicks;
                        }
                    }
                    else if (field.DataType == DbType.Guid)
                    {
                        var value = field.PropertyInfo.GetValue(item, null);

                        if (value == null)
                        {
                            command.Parameters[ParameterPrefix + field.FieldName].Value = DBNull.Value;
                        }
                        else
                        {
                            command.Parameters[ParameterPrefix + field.FieldName].Value = ((Guid)value).ToByteArray();
                        }
                    }
                    else
                    {
                        var value = field.PropertyInfo.GetValue(item, null);
                        if (value == null)
                        {
                            if (field.DefaultValue != null)
                            {
                                command.Parameters[ParameterPrefix + field.FieldName].Value = field.DefaultValue;
                            }
                            else
                            {
                                command.Parameters[ParameterPrefix + field.FieldName].Value = DBNull.Value;
                            }
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
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                if (Debugger.IsAttached) Debugger.Break();
                throw;
            }
            finally
            {
                DoneWithConnection(connection, false);
            }
        }

        private int GetIdentity(IDbConnection connection)
        {
            using (var command = new MySqlCommand("SELECT last_insert_id()", connection as MySqlConnection))
            {
                command.Transaction = CurrentTransaction as MySqlTransaction;
                object id = command.ExecuteScalar();
                return Convert.ToInt32(id);
            }
        }

        protected override void ValidateTable(IDbConnection connection, IEntityInfo entity)
        {
            // prevent cached reads of entitiy fields
            m_lastEntity = null;

            // first make sure the table exists
            if (!TableExists(entity.EntityAttribute.NameInStore))
            {
                CreateTable(connection, entity);
                return;
            }

            using (var command = GetNewCommandObject())
            {
                command.Transaction = CurrentTransaction;
                command.Connection = connection;

                foreach (var field in entity.Fields)
                {
                    if (ReservedWords.Contains(field.FieldName, StringComparer.InvariantCultureIgnoreCase))
                    {
                        throw new ReservedWordException(field.FieldName);
                    }

                    // yes, I realize hard-coded ordinals are not a good practice, but the SQL isn't changing, it's method specific
                    var sql = string.Format("SELECT column_name, "  // 0
                          + "data_type, "                       // 1
                          + "character_maximum_length, "        // 2
                          + "numeric_precision, "               // 3
                          + "numeric_scale, "                   // 4
                          + "is_nullable "
                          + "FROM information_schema.columns "
                          + "WHERE (table_name = '{0}' AND column_name = '{1}')",
                          entity.EntityAttribute.NameInStore, field.FieldName);

                    command.CommandText = sql;

                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            // field doesn't exist - we must create it
                            var alter = new StringBuilder(string.Format("ALTER TABLE {0} ", entity.EntityAttribute.NameInStore));
                            alter.Append(string.Format("ADD [{0}] {1} {2}",
                                field.FieldName,
                                GetFieldDataTypeString(entity.EntityName, field),
                                GetFieldCreationAttributes(entity.EntityAttribute, field)));

                            using (var altercmd = GetNewCommandObject())
                            {
                                altercmd.CommandText = alter.ToString();
                                altercmd.Connection = connection;
                                altercmd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            // TODO: verify field length, etc.
                        }
                    }
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

        private IEnumerable<object> Select(string entityName, Type objectType, IEnumerable<FilterCondition> filters, int fetchCount, int firstRowOffset, bool fillReferences)
        {
            if (entityName == null)
            {
                throw new EntityNotFoundException(objectType);
            }

            UpdateIndexCacheForType(entityName);

            var items = new List<object>();

            var connection = GetConnection(false);
            MySqlCommand command = null;

            try
            {
                CheckOrdinals(entityName);
                bool tableDirect;
                command = GetSelectCommand<MySqlCommand, MySqlParameter>(entityName, filters, out tableDirect);
                command.Connection = connection as MySqlConnection;
                command.Transaction = CurrentTransaction as MySqlTransaction;

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

                            if (!fieldsSet)
                            {
                                foreach (var field in Entities[entityName].Fields)
                                {
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
                                        else if (field.IsTimespan)
                                        {
                                            var valueAsTimeSpan = new TimeSpan((long)value);
                                            field.PropertyInfo.SetValue(item, valueAsTimeSpan, null);
                                        }
                                        else if (field.DataType == DbType.Guid)
                                        {
                                            if (value != null)
                                            {
                                                var guid = new Guid((byte[])value);
                                                field.PropertyInfo.SetValue(item, guid, null);
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
        }

        private void UpdateIndexCacheForType(string entityName)
        {
            // have we already cached this?
            if (((SqlEntityInfo)Entities[entityName]).IndexNames != null) return;

            // get all iindex names for the type
            var connection = GetConnection(true);
            try
            {
                string sql = string.Format(
                    "SELECT INDEX_NAME " +
                    "FROM INFORMATION_SCHEMA.STATISTICS " +
                    "WHERE TABLE_SCHEMA = '{0}' " +
                    "AND TABLE_NAME = '{1}'", 
                    m_info.DatabaseName,
                    entityName);

                using (var command = GetNewCommandObject())
                {
                    command.CommandText = sql;
                    command.Connection = connection;
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


        protected override void GetPrimaryKeyInfo(string entityName, out string indexName, out string columnName)
        {
            var connection = GetConnection(true);
            try
            {
                string sql = string.Format(
                    "SELECT column_name " +
                    "FROM INFORMATION_SCHEMA.STATISTICS " +
                    "WHERE TABLE_SCHEMA = '{0}' " +
                    "AND TABLE_NAME = '{1}' " +
                    "AND INDEX_NAME ='PRIMARY'",
                    m_info.DatabaseName,
                    entityName);

                // TODO: get index name
                indexName = string.Empty;
                columnName = string.Empty;

                using (var command = GetNewCommandObject())
                {
                    command.CommandText = sql;
                    command.Connection = connection;
                    command.Transaction = CurrentTransaction;
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            columnName = reader.GetString(0);
                        }
                    }
                }
            }
            finally
            {
                DoneWithConnection(connection, true);
            }
        }

        public override void OnUpdate(object item, bool cascadeUpdates, string fieldName)
        {
            if (item is DynamicEntity)
            {
                throw new NotSupportedException();
//                OnUpdateDynamicEntity(item as DynamicEntity);
                return;
            }

            object keyValue;
            var changeDetected = false;
            var itemType = item.GetType();
            var entityName = m_entities.GetNameForType(itemType);
            var insertCommand = GetNewCommandObject();

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

                    command.CommandText = string.Format("SELECT * FROM {0} WHERE {1} = {2}keyparam",
                        entityName,
                        Entities[entityName].Fields.KeyField.FieldName,
                        ParameterPrefix);

                    command.CommandType = CommandType.Text;
                    command.Parameters.Add(new MySqlParameter(ParameterPrefix + "keyparam", keyValue));
                    command.Transaction = CurrentTransaction;

                    var updateSQL = new StringBuilder(string.Format("UPDATE {0} SET ", entityName));

                    using (var reader = command.ExecuteReader() as MySqlDataReader)
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
                                    insertCommand.Parameters.Add(new MySqlParameter(ParameterPrefix + field.FieldName, value));
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
                                    insertCommand.Parameters.Add(new MySqlParameter(ParameterPrefix + field.FieldName, ticks));
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
                                        insertCommand.Parameters.Add(new MySqlParameter(ParameterPrefix + field.FieldName, value));
                                    }
                                }
                            }
                        }
                    }

                    // only execute if a change occurred
                    if (changeDetected)
                    {
                        // remove the trailing comma and append the filter
                        updateSQL.Length -= 2;
                        updateSQL.AppendFormat(" WHERE {0} = {1}keyparam", Entities[entityName].Fields.KeyField.FieldName, ParameterPrefix);
                        insertCommand.Parameters.Add(new MySqlParameter(ParameterPrefix + "keyparam", keyValue));
                        insertCommand.CommandText = updateSQL.ToString();
                        insertCommand.Connection = connection;
                        insertCommand.Transaction = CurrentTransaction;
                        insertCommand.ExecuteNonQuery();
                    }
                }
            }
            finally
            {
                insertCommand.Dispose();
                DoneWithConnection(connection, false);
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

        public override string[] GetTableNames()
        {
            var names = new List<string>();

            var connection = GetConnection(true);
            try
            {
                using (var command = GetNewCommandObject())
                {
                    command.Transaction = CurrentTransaction;
                    command.Connection = connection;
                    var sql = "SHOW TABLES";
                    command.CommandText = sql;
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            names.Add(reader.GetString(0));
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

        protected override IDbCommand GetNewCommandObject()
        {
            return new MySqlCommand();
        }

        protected override IDbConnection GetNewConnectionObject()
        {
            return GetNewConnectionObject(ConnectionString);
        }

        private IDbConnection GetNewConnectionObject(string connectionString)
        {
            return new MySqlConnection(connectionString);
        }

        protected override IDataParameter CreateParameterObject(string parameterName, object parameterValue)
        {
            return new MySqlParameter(parameterName, parameterValue);
        }

        protected override string GetFieldDataTypeString(string entityName, FieldAttribute field)
        {
            switch (field.DataType)
            {
                case DbType.Guid:
                    return "binary(16)";
                case DbType.String:
                case DbType.StringFixedLength:
                    if (field.Length > MaxSizedStringLength)
                    {
                        return "text";
                    }
                    else
                    {
                        return "varchar";
                    }
                case DbType.Single:
                    return "float";
                default:
                    return base.GetFieldDataTypeString(entityName, field);
            }
        }

        public override IEnumerable<T> Fetch<T>(int fetchCount, int firstRowOffset, string sortField, FieldSearchOrder sortOrder, FilterCondition filter, bool fillReferences)
        {
            throw new NotImplementedException();
        }

        public override int Count<T>(IEnumerable<FilterCondition> filters)
        {
            throw new NotImplementedException();
        }
    }
}
