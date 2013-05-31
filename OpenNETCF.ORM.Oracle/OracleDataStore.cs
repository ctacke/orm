using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Oracle.DataAccess.Client;

namespace OpenNETCF.ORM
{
    public partial class OracleDataStore : SQLStoreBase<SqlEntityInfo>, IDisposable
    {
        private string m_connectionString; 
        private OracleConnectionInfo m_info;

        public OracleDataStore(OracleConnectionInfo info)
        {
            m_info = info;
        }

        private string BuildConnectionString(OracleConnectionInfo info)
        {
            var cs = string.Format(
                "Data Source=(DESCRIPTION=" +
                "(ADDRESS=(PROTOCOL=TCP)(HOST={0})(PORT={1}))" +
                "(CONNECT_DATA=(SERVICE_NAME={2})));" +
                "User Id={3};Password={4};",
                     info.ServerAddress,
                     info.ServerPort,
                     info.ServiceName,
                     info.UserName,
                     info.Password);

            return cs;
        }

        private string ConnectionString
        {
            get
            {
                if (m_connectionString == null)
                {
                    m_connectionString = BuildConnectionString(m_info);

                }
                return m_connectionString;
            }
        }

        protected override int MaxSizedStringLength
        {
            // NOTE: this is a character count, and it depends on the encoding of the DB.  We'll assume UTF16 for safety
            get { return 2000; }
        }

        protected override string ParameterPrefix
        {
            get { return ":"; }
        }

        protected override IDbCommand GetNewCommandObject()
        {
            return new OracleCommand();
        }

        protected override IDbConnection GetNewConnectionObject()
        {
            return new OracleConnection(ConnectionString);
        }

        protected override IDataParameter CreateParameterObject(string parameterName, object parameterValue)
        {
            return new OracleParameter(parameterName, parameterValue);
        }

        // oracle does not support a direct auto-incrementing identifier 
        // (search for SEQUENCE for a workaround - maybe implement this in a future ORM build)
        //create table FOO (
        //    x number primary key
        //);
        //create sequence  FOO_seq;

        //create or replace trigger FOO_trg
        //before insert on FOO
        //for each row
        //begin
        //  select FOO_seq.nextval into :new.x from dual;
        //end;
        protected override string AutoIncrementFieldIdentifier
        {
            get 
            { 
                return string.Empty; 
            }
        }

        public override void CreateStore()
        {
            // NOP
        }

        public override void DeleteStore()
        {
            throw new NotSupportedException();
        }

        public override bool StoreExists
        {
            // Oracle has a single "store" which is the service instance, unlike say SQL Server which may have multiple Databases in an instance
            get { return true; }
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
                    // this gets all tables the current user has access to (not necessarily all tables in the store)
                    var sql = "SELECT table_name FROM all_tables";
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

        public override bool TableExists(string tableName)
        {
            var connection = GetConnection(true);
            try
            {
                using (var command = GetNewCommandObject())
                {
                    command.Connection = connection;
                    // Oracle is case-sensitive.  Joy!
                    var sql = string.Format("SELECT COUNT(*) FROM all_tables WHERE UPPER(table_name) = UPPER('{0}')", tableName);
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

        protected override string GetFieldDataTypeString(string entityName, FieldAttribute field)
        {
            switch (field.DataType)
            {
                case DbType.String:
                    return "NVARCHAR2";
                case DbType.StringFixedLength:
                    return "CHAR";
                case DbType.Int64:
                case DbType.UInt64:
                case DbType.Int32:
                case DbType.UInt32:
                case DbType.Int16:
                case DbType.UInt16:
                case DbType.Decimal:
                    return "NUMBER";
                case DbType.Single:
                    return "BINARY_FLOAT";
                case DbType.Double:
                    return "BINARY_DOUBLE";
                case DbType.DateTime:
                    return "DATE";
                case DbType.Binary:
                    return "BLOB";
                case DbType.Guid:
                    return "RAW";
                default:
                    throw new NotSupportedException(
                        string.Format("Unable to determine convert DbType '{0}' to string", field.DataType.ToString()));
            }
        }

        protected override string GetFieldCreationAttributes(EntityAttribute attribute, FieldAttribute field)
        {
            switch (field.DataType)
            {
                case DbType.Guid:
                    return "(16)"; // guids are "RAW(16)"
                default:
                    return base.GetFieldCreationAttributes(attribute, field);
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
                    var sql = string.Format(
                        "SELECT COLUMN_NAME, "
                        + "DATA_TYPE, "
                        + "DATA_LENGTH, "
                        + "DATA_PRECISION, "
                        + "DATA_SCALE, "
                        + "NULLABLE "
                        + "FROM all_tab_cols "
                        + "WHERE (UPPER(table_name) = UPPER('{0}') AND UPPER(column_name) = UPPER('{1}'))",
                          entity.EntityAttribute.NameInStore, field.FieldName);

                    command.CommandText = sql;

                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            // field doesn't exist - we must create it
                            var alter = new StringBuilder(string.Format("ALTER TABLE {0} ", entity.EntityAttribute.NameInStore));
                            alter.Append(string.Format("ADD {0} {1} {2}",
                                field.FieldName,
                                GetFieldDataTypeString(entity.EntityName, field),
                                GetFieldCreationAttributes(entity.EntityAttribute, field)));

                            using (var altercmd = GetNewCommandObject()) 
                            {
                                altercmd.CommandText = alter.ToString();
                                altercmd.Connection = connection;
                                var result = altercmd.ExecuteNonQuery();
                                altercmd.Dispose();
                            }
                        }
                        else
                        {
                            // TODO: verify field length, etc.
                        }
                    }
                }
                
///////////////////// TEST SECTION DUE TO ORACLE "BEHAVIOR"

                //if (columnAdded) ValidateTable(connection, entity);

                //(connection as OracleConnection).FlushCache();
                //(connection as OracleConnection).PurgeStatementCache();

                //connection.Close();
                //connection.Open();

                //ConnectionBehavior = ORM.ConnectionBehavior.AlwaysNew;
                //using (var cmd = GetNewCommandObject())
                //{
                //    cmd.Connection = GetConnection(false);
                //    cmd.CommandText = string.Format("SELECT * FROM {0}", entity.EntityAttribute.NameInStore);
                //    cmd.Prepare();
                //    //                    command.CommandText = string.Format("SELECT * FROM all_tab_cols WHERE table_name = '{0}'", entityName.ToUpper());
                //    using (var reader = cmd.ExecuteReader())
                //    {
                //        var c = reader.FieldCount;
                //    }

                //    cmd.Dispose();
                //}


                //CheckOrdinals(entity.EntityAttribute.NameInStore);

//////////////
            }
        }

        protected override void CheckOrdinals(string entityName)
        {
            // a bug in ODAC prevents the base implementation from working
            if (Entities[entityName].Fields.OrdinalsAreValid) return;

            var connection = GetConnection(true);
            try
            {
                using (var command = GetNewCommandObject())
                {
                    command.Connection = connection;
                    command.CommandText = string.Format("SELECT column_name FROM all_tab_cols WHERE UPPER(table_name) = UPPER('{0}') order by column_id", entityName);

                    int ordinal = 0;

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var field = reader.GetString(0);
                            Debug.WriteLine(string.Format("Field {0} ordinal = {1}", field, ordinal));
                            Entities[entityName].Fields[field].Ordinal = ordinal;
                            ordinal++;
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

            base.CheckOrdinals(entityName);
        }

        private OracleCommand GetInsertCommand(string entityName)
        {
            // TODO: support command caching to improve bulk insert speeds
            //       simply use a dictionary keyed by entityname
            var keyScheme = Entities[entityName].EntityAttribute.KeyScheme;
            var insertCommand = GetNewCommandObject() as OracleCommand;

            var sbFields = new StringBuilder(string.Format("INSERT INTO {0} (", entityName));
            var sbParams = new StringBuilder(" VALUES (");

            FieldAttribute identity = null;

            foreach (var field in Entities[entityName].Fields)
            {
                // skip auto-increments
                if ((field.IsPrimaryKey) && (keyScheme == KeyScheme.Identity))
                {
                    identity = field;
                    continue;
                }
                sbFields.Append(field.FieldName + ",");
                sbParams.Append(":" + field.FieldName + ",");

                var parameter = new OracleParameter(field.FieldName, TranslateDbTypeToOracleDbType(field.DataType));
                insertCommand.Parameters.Add(parameter);
            }

            // replace trailing commas
            sbFields[sbFields.Length - 1] = ')';
            sbParams[sbParams.Length - 1] = ')';

            var sql = sbFields.ToString() + sbParams.ToString();

            if (identity != null)
            {
                sql += string.Format(" RETURNING {0} INTO :LASTID", identity.FieldName);
            }

            insertCommand.CommandText = sql;

            return insertCommand;
        }

        private OracleDbType TranslateDbTypeToOracleDbType(DbType type)
        {
            switch (type)
            {
                case DbType.String:
                    return OracleDbType.NVarchar2;
                case DbType.StringFixedLength:
                    return OracleDbType.Char;
                case DbType.Int64:
                case DbType.UInt64:
                    return OracleDbType.Int64;
                case DbType.Int32:
                case DbType.UInt32:
                    return OracleDbType.Int32;
                case DbType.Int16:
                case DbType.UInt16:
                    return OracleDbType.Int16;
                case DbType.Decimal:
                    return OracleDbType.Decimal;
                case DbType.Single:
                    return OracleDbType.BinaryFloat;
                case DbType.Double:
                    return OracleDbType.BinaryDouble;
                case DbType.DateTime:
                    return OracleDbType.Date;
                case DbType.Binary:
                    return OracleDbType.Blob;
                case DbType.Guid:
                    return OracleDbType.Raw;
                default:
                    throw new NotSupportedException(string.Format("Cannot translate DbType '{0}' to OracleDbType", type.ToString()));
            }
        }

        public override void OnInsert(object item, bool insertReferences)
        {
            if (item is DynamicEntity)
            {
                OnInsertDynamicEntity(item as DynamicEntity, insertReferences);
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
                command.Connection = connection  as OracleConnection;
                command.Transaction = CurrentTransaction as OracleTransaction;

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
                            command.Parameters[field.FieldName].Value = DBNull.Value;
                        }
                        else
                        {
                            command.Parameters[field.FieldName].Value = value;
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
                        command.Parameters[field.FieldName].Value = dtValue;
                    }
                    else if (field.DataType == DbType.Guid)
                    {
                        // read-only, so do nothing
                        var guid = field.PropertyInfo.GetValue(item, null);
                        if (guid == null)
                        {
                            command.Parameters[field.FieldName].Value = DBNull.Value;
                        }
                        else
                        {
                            command.Parameters[field.FieldName].Value = ((Guid)guid).ToByteArray();
                        }
                    }
                    else if (field.PropertyInfo.PropertyType.UnderlyingTypeIs<TimeSpan>())
                    {
                        // SQL Compact doesn't support Time, so we're convert to a DateTime both directions
                        var value = field.PropertyInfo.GetValue(item, null);

                        if (value == null)
                        {
                            command.Parameters[field.FieldName].Value = DBNull.Value;
                        }
                        else
                        {
                            var timespanTicks = ((TimeSpan)value).Ticks;
                            command.Parameters[field.FieldName].Value = timespanTicks;
                        }
                    }
                    else
                    {
                        var value = field.PropertyInfo.GetValue(item, null);
                        if (value == null)
                        {
                            if (field.DefaultValue != null)
                            {
                                command.Parameters[field.FieldName].Value = field.DefaultValue;
                            }
                            else
                            {
                                command.Parameters[field.FieldName].Value = DBNull.Value;
                            }
                        }
                        else
                        {
                            command.Parameters[field.FieldName].Value = value;
                        }
                    }
                }

                // did we have an identity field?  If so, we need to update that value in the item
                if (identity == null)
                {
                    command.ExecuteNonQuery();
                }
                else
                {
                    var idParameter =  new OracleParameter(":LASTID", OracleDbType.Int32);
                    idParameter.Direction = ParameterDirection.Output;
                    command.Parameters.Add(idParameter);

                    command.ExecuteNonQuery();

                    identity.PropertyInfo.SetValue(item, idParameter.Value, null);
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

        protected override IEnumerable<object> Select(Type objectType, IEnumerable<FilterCondition> filters, int fetchCount, int firstRowOffset, bool fillReferences)
        {
            string entityName = m_entities.GetNameForType(objectType);

            if (entityName == null)
            {
                throw new EntityNotFoundException(objectType);
            }

            return Select(entityName, objectType, filters, fetchCount, firstRowOffset, fillReferences);
        }

        private void UpdateIndexCacheForType(string entityName)
        {
            // have we already cached this?
            if (((SqlEntityInfo)Entities[entityName]).IndexNames != null) return;

            // get all index names for the type
            var connection = GetConnection(true);
            try
            {
                string sql = string.Format(
                    "SELECT table_name, index_name, column_name FROM all_ind_columns " +
                    "WHERE UPPER(table_name) = UPPER('{0}')"
                , entityName);

                using (var command = GetNewCommandObject())
                {
                    command.CommandText = sql;
                    command.Connection = connection;
                    using (var reader = command.ExecuteReader())
                    {
                        List<string> nameList = new List<string>();

                        while (reader.Read())
                        {
                            nameList.Add(reader.GetString(1));
                        }

                        ((SqlEntityInfo)Entities[entityName]).IndexNames = nameList;
                    }
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
                DoneWithConnection(connection, true);
            }
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
            OracleCommand command = null;

            try
            {
                CheckOrdinals(entityName);
                bool tableDirect;
                command = GetSelectCommand<OracleCommand, OracleParameter>(entityName, filters, out tableDirect);
                command.Connection = connection as OracleConnection;
                command.Transaction = CurrentTransaction as OracleTransaction;

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
                                        else if (field.DataType == DbType.Guid)
                                        {
                                            // sql stores this an 8-byte array
                                            field.PropertyInfo.SetValue(item, new Guid((byte[])value), null);
                                        }
                                        else if (field.IsTimespan)
                                        {
                                            // SQL Compact doesn't support Time, so we're convert to ticks in both directions
                                            var valueAsTimeSpan = new TimeSpan(Convert.ToInt64(value));
                                            field.PropertyInfo.SetValue(item, valueAsTimeSpan, null);
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
                                        else if (value is decimal)
                                        {
                                            // Oracle numeric fields appear to come back as "decimal"

                                            // When we query those back, we must convert to put them into the property or we crash hard
                                            if (field.PropertyInfo.PropertyType.Equals(typeof(UInt32)))
                                            {
                                                var t = value.GetType();
                                                field.PropertyInfo.SetValue(item, Convert.ToUInt32(value), null);
                                            }
                                            else if ((field.PropertyInfo.PropertyType.Equals(typeof(Int32))) || (field.PropertyInfo.PropertyType.Equals(typeof(Int32?))))
                                            {
                                                var t = value.GetType();
                                                field.PropertyInfo.SetValue(item, Convert.ToInt32(value), null);
                                            }
                                            else if (field.PropertyInfo.PropertyType.Equals(typeof(decimal)))
                                            {
                                                var t = value.GetType();
                                                field.PropertyInfo.SetValue(item, Convert.ToDecimal(value), null);
                                            }
                                            else if (field.PropertyInfo.PropertyType.Equals(typeof(float)))
                                            {
                                                var t = value.GetType();
                                                field.PropertyInfo.SetValue(item, Convert.ToSingle(value), null);
                                            }
                                            else
                                            {
                                                var t = value.GetType();
                                                field.PropertyInfo.SetValue(item, value, null);
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

        protected override void GetPrimaryKeyInfo(string entityName, out string indexName, out string columnName)
        {
            var connection = GetConnection(true);
            try
            {
                string sql = string.Format(
                    "SELECT cons.constraint_name, cols.column_name " +
                    "FROM all_constraints cons, all_cons_columns cols " +
                    "WHERE UPPER(cols.table_name) = UPPER('{0}') " +
                    "AND cons.constraint_type = 'P' " +
                    "AND cons.constraint_name = cols.constraint_name",
                    entityName);

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
                            indexName = reader.GetString(0);
                            columnName = reader.GetString(1);
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
                OnUpdateDynamicEntity(item as DynamicEntity);
                return;
            }

            object keyValue;
            var changeDetected = false;
            var itemType = item.GetType();
            var entityName = m_entities.GetNameForType(itemType);

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
                    command.Parameters.Add(new OracleParameter(ParameterPrefix + "keyparam", keyValue));
                    command.Transaction = CurrentTransaction;

                    var updateSQL = new StringBuilder(string.Format("UPDATE {0} SET ", entityName));

                    using (var reader = command.ExecuteReader() as OracleDataReader)
                    {

                        if (!reader.HasRows)
                        {
                            // TODO: the PK value has changed - we need to store the original value in the entity or diallow this kind of change
                            throw new RecordNotFoundException("Cannot locate a record with the provided primary key.  You cannot update a primary key value through the Update method");
                        }

                        reader.Read();

                        using (var insertCommand = GetNewCommandObject())
                        {
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
                                        insertCommand.Parameters.Add(new OracleParameter(ParameterPrefix + field.FieldName, value));
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
                                        insertCommand.Parameters.Add(new OracleParameter(ParameterPrefix + field.FieldName, ticks));
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
                                            insertCommand.Parameters.Add(new OracleParameter(ParameterPrefix + field.FieldName, value));
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
                                insertCommand.Parameters.Add(new OracleParameter(ParameterPrefix + "keyparam", keyValue));
                                insertCommand.CommandText = updateSQL.ToString();
                                insertCommand.Connection = connection;
                                insertCommand.Transaction = CurrentTransaction;
                                insertCommand.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
            finally
            {
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
