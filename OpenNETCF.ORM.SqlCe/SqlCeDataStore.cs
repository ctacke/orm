using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using System.Data;
using System.Data.SqlServerCe;
using System.IO;
using System.Runtime.InteropServices;
using System.Data.Common;
using System.Data.SqlTypes;

namespace OpenNETCF.ORM
{
    public partial class SqlCeDataStore : SQLStoreBase<SqlEntityInfo>
    {
        private string m_connectionString;
        private int m_maxSize = 128; // Max Database Size defaults to 128MB

        private string Password { get; set; }

        public string FileName { get; protected set; }

        protected SqlCeDataStore()
            : base()
        {
            UseCommandCache = true;
        }

        public SqlCeDataStore(string fileName)
            : this(fileName, null)
        {
        }

        public SqlCeDataStore(string fileName, string password)
            : this()
        {
            FileName = fileName;
            Password = password;
        }

        public override bool StoreExists
        {
            get
            {
                return File.Exists(FileName);
            }
        }

        public override string Name
        {
            get { return FileName; }
        }

        protected override IDbCommand GetNewCommandObject()
        {
            return new SqlCeCommand();
        }

        protected override string AutoIncrementFieldIdentifier
        {
            get { return "IDENTITY"; }
        }

        /// <summary>
        /// Deletes the underlying DataStore
        /// </summary>
        public override void DeleteStore()
        {
            File.Delete(FileName);
        }

        /// <summary>
        /// Creates the underlying DataStore
        /// </summary>
        public override void CreateStore()
        {
            if (StoreExists)
            {
                throw new StoreAlreadyExistsException();
            }

            // create the file
            using (SqlCeEngine engine = new SqlCeEngine(ConnectionString))
            {
                engine.CreateDatabase();
            }

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
                foreach (var entity in this.Entities)
                {
                    ValidateTable(connection, entity);
                }
            }
            finally
            {
                DoneWithConnection(connection, true);
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
                using (var command = BuildFilterCommand<SqlCeCommand, SqlCeParameter>(entityName, filters, true))
                {
                    command.Transaction = CurrentTransaction as SqlCeTransaction;
                    command.Connection = connection as SqlCeConnection;
                    return (int)command.ExecuteScalar();
                }
            }
            finally
            {
                DoneWithConnection(connection, true);
            }
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
            string entityName;
            var itemType = item.GetType();

            if (item is DynamicEntity)
            {
                entityName = (item as DynamicEntity).EntityName;
            }
            else
            {
                entityName = m_entities.GetNameForType(itemType);
            }

            var keyScheme = Entities[entityName].EntityAttribute.KeyScheme;

            if (entityName == null)
            {
                throw new EntityNotFoundException(item.GetType());
            }

            if (insertReferences)
            {
                DoInsertReferences(item, entityName, keyScheme, true);
            }

            // we'll use table direct for inserts - no point in getting the query parser involved in this
            var connection = GetConnection(false);
            try
            {
                CheckOrdinals(entityName);

                FieldAttribute identity = null;

                using (var command = new SqlCeCommand())
                {
                    command.Connection = connection as SqlCeConnection;
                    command.Transaction = CurrentTransaction as SqlCeTransaction;
                    command.CommandText = entityName;
                    command.CommandType = CommandType.TableDirect;

                    using (var results = command.ExecuteResultSet(ResultSetOptions.Updatable))
                    {
                        var record = results.CreateRecord();

                        FillEntity(record.SetValue, entityName, itemType, item, out identity);

                        results.Insert(record);

                        // did we have an identity field?  If so, we need to update that value in the item
                        if (identity != null)
                        {
                            var id = GetIdentity(connection);
                            SetInstanceValue(identity, item, id);
                        }

                        if (insertReferences)
                        {
                            DoInsertReferences(item, entityName, keyScheme, false);
                        }
                    }

                    command.Dispose();
                }
            }
            finally
            {
                DoneWithConnection(connection, false);
            }
        }

        private void FillEntity(Action<int, object> setter, string entityName, Type itemType, object item, out FieldAttribute identity)
        {
            // The reason for this somewhat convoluted Action parameter is that while the SqlCeUpdateableRecord (from Insert) 
            // and SqlCeResultSet (from Update) both contain a SetValue method, they don't share it on any common
            // interface.  using an Action allows us to share this code anyway.
            identity = null;

            var keyScheme = Entities[entityName].EntityAttribute.KeyScheme;

            foreach (var field in Entities[entityName].Fields)
            {
                if (field.IsPrimaryKey)
                {
                    switch(keyScheme)
                    {
                        case KeyScheme.Identity:
                            identity = field;
                            break;
                        case KeyScheme.GUID:
                            var value = GetInstanceValue(field, item);
                            if (value.Equals(Guid.Empty))
                            {
                                value = Guid.NewGuid();
                                SetInstanceValue(field, item, value);
                            }
                            setter(field.Ordinal, value);
                            break;
                    }
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
                        setter(field.Ordinal, DBNull.Value);
                    }
                    else
                    {
                        setter(field.Ordinal, value);
                    }
                }
                else if (field.DataType == DbType.DateTime)
                {
                    var dtValue = GetInstanceValue(field, item);

                    if (dtValue.Equals(DateTime.MinValue))
                    {
                        if ((!field.AllowsNulls) && (field.DefaultType != DefaultType.CurrentDateTime))
                        {
                            dtValue = SqlDateTime.MinValue;
                            setter(field.Ordinal, dtValue);
                        }
                        else
                        {
                            // let the null pass through
                        }
                    }
                    else
                    {
                        setter(field.Ordinal, dtValue);
                    }

                }
                else if (field.IsRowVersion)
                {
                    // read-only, so do nothing
                }
                else
                {
                    var iv = GetInstanceValue(field, item);
                    
                    if((iv == DBNull.Value) && (field.DefaultValue != null))
                    {
                        iv = field.DefaultValue;
                    }

                    setter(field.Ordinal, iv);
                }
            }
        }

        protected override IDataParameter CreateParameterObject(string parameterName, object parameterValue)
        {
            return new SqlCeParameter(parameterName, parameterValue);
        }

        private int GetIdentity(IDbConnection connection)
        {
            using (var command = new SqlCeCommand("SELECT @@IDENTITY", connection as SqlCeConnection))
            {
                command.Transaction = CurrentTransaction as SqlCeTransaction;
                object id = command.ExecuteScalar();
                return Convert.ToInt32(id);
            }
        }

        protected override void GetPrimaryKeyInfo(string entityName, out string indexName, out string columnName)
        {
            indexName = string.Empty;
            columnName = string.Empty;

            var connection = GetConnection(true);
            try
            {
                string sql = string.Format("SELECT INDEX_NAME FROM information_schema.indexes WHERE (TABLE_NAME = '{0}') AND (PRIMARY_KEY = 1)", entityName);

                using (var command = GetNewCommandObject())
                {
                    command.Transaction = CurrentTransaction as SqlCeTransaction;
                    command.CommandText = sql;
                    command.Connection = connection;
                    indexName = command.ExecuteScalar() as string;
                }
            }
            finally
            {
                DoneWithConnection(connection, true);
            }
        }


        protected override void CheckOrdinals(string entityName)
        {
            if (Entities[entityName].Fields.OrdinalsAreValid) return;

            var connection = GetConnection(true);
            try
            {
                using (var command = new SqlCeCommand())
                {
                    command.Transaction = CurrentTransaction as SqlCeTransaction;
                    command.Connection = connection as SqlCeConnection;
                    command.CommandText = entityName;
                    command.CommandType = CommandType.TableDirect;

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

        public int MaxDatabaseSizeInMB
        {
            get { return m_maxSize; }
            set
            {
                // min of 128MB
                if (value < 128) throw new ArgumentOutOfRangeException();
                // max of 4GB
                if (value > 4096) throw new ArgumentOutOfRangeException();
                m_maxSize = value;
            }
        }

        public override string ConnectionString
        {
            get
            {
                if (m_connectionString == null)
                {
                    m_connectionString = string.Format("Data Source={0};Persist Security Info=False;Max Database Size={1};", FileName, MaxDatabaseSizeInMB);

                    if (!string.IsNullOrEmpty(Password))
                    {
                        m_connectionString += string.Format("Password={0};", Password);
                    }
                }
                return m_connectionString;
            }
        }

        protected override IDbConnection GetNewConnectionObject()
        {
            return new SqlCeConnection(ConnectionString);
        }

        protected void ValidateIndex(IDbConnection connection, string indexName, string tableName, string fieldName, bool ascending)
        {
            var valid = false;

            string sql = string.Format("SELECT INDEX_NAME FROM information_schema.indexes WHERE (TABLE_NAME = '{0}') AND (COLUMN_NAME = '{1}')", tableName, fieldName);

            using (SqlCeCommand command = new SqlCeCommand(sql, connection as SqlCeConnection))
            {
                command.Transaction = CurrentTransaction as SqlCeTransaction;
                var name = command.ExecuteScalar() as string;

                if (string.Compare(name, indexName, true) == 0)
                {
                    valid = true;
                }

                if (!valid)
                {
                    sql = string.Format("CREATE INDEX {0} ON {1}({2} {3})",
                        indexName,
                        tableName,
                        fieldName,
                        ascending ? "ASC" : "DESC");

                    command.CommandText = sql;
                    command.ExecuteNonQuery();
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
                    command.Transaction = CurrentTransaction as SqlCeTransaction;
                    command.Connection = connection;
                    var sql = "SELECT table_name FROM information_schema.tables";
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
                    command.Transaction = CurrentTransaction as SqlCeTransaction;
                    command.Connection = connection;
                    var sql = string.Format("SELECT COUNT(*) FROM information_schema.tables WHERE table_name = '{0}'", tableName);
                    command.CommandText = sql;
                    var count = Convert.ToInt32(command.ExecuteScalar());

                    return (count > 0);
                }
            }
            finally
            {
                DoneWithConnection(connection, true);
            }
        }

        public override DynamicEntityDefinition DiscoverDynamicEntity(string entityName)
        {
            if (!TableExists(entityName))
            {
                return null;
            }

            var connection = GetConnection(true);
            try
            {
                using (var cmd = GetNewCommandObject())
                {
                    cmd.Connection = connection;
                    cmd.Transaction = CurrentTransaction;

                    cmd.CommandText = string.Format("SELECT COLUMN_NAME, ORDINAL_POSITION, IS_NULLABLE, DATA_TYPE, NUMERIC_PRECISION, NUMERIC_SCALE FROM information_schema.columns WHERE TABLE_NAME = '{0}' ORDER BY ORDINAL_POSITION", entityName);

                    var fields = new List<FieldAttribute>();

                    using (var reader = cmd.ExecuteReader())
                    {
                        while(reader.Read())
                        {
                            var name = reader.GetString(0);
                            var nullable = string.Compare(reader.GetString(2), "YES", true) == 0;
                            var type = reader.GetString(3).ParseToDbType();

                            var field = new FieldAttribute()
                            {
                                DataType = type,
                                FieldName = name,
                                AllowsNulls = nullable,
                            };

                            if (!reader.IsDBNull(4))
                            {
                                field.Precision = Convert.ToInt32(reader.GetValue(4));
                            }
                            if (!reader.IsDBNull(5))
                            {
                                field.Scale = Convert.ToInt32(reader.GetValue(5));
                            }

                            fields.Add(field);
                        }
                    }

                    cmd.CommandText = string.Format("SELECT COLUMN_NAME, PRIMARY_KEY, [UNIQUE], COLLATION FROM information_schema.indexes WHERE TABLE_NAME = '{0}'", entityName);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var column = reader.GetString(0);
                            var pk = Convert.ToBoolean(reader.GetValue(1));
                            var unique = Convert.ToBoolean(reader.GetValue(2));

                            var field = fields.FirstOrDefault(f => f.FieldName == column);
                            if (pk)
                            {
                                field.IsPrimaryKey = true;
                            }
                            else
                            {
                                var collation = Convert.ToInt32(reader.GetValue(3));
                                field.SearchOrder = collation == 1 ? FieldSearchOrder.Ascending : FieldSearchOrder.Descending; 
                            }
                            if (unique)
                            {
                                field.RequireUniqueValue = true;
                            }
                        }
                    }


                    var entityDefinition = new DynamicEntityDefinition(entityName, fields);
                    RegisterEntityInfo(entityDefinition);
                    return entityDefinition;
                }
            }
            finally
            {
                DoneWithConnection(connection, true);
            }
        }

        protected override void ValidateTable(IDbConnection connection, IEntityInfo entity)
        {
            // prevent caches reads of entitiy fields
            m_lastEntity = null;

            // first make sure the table exists
            if (!TableExists(entity.EntityAttribute.NameInStore))
            {
                CreateTable(connection, entity);
                return;
            }

            using (var command = new SqlCeCommand())
            {
                command.Transaction = CurrentTransaction as SqlCeTransaction;
                command.Connection = connection as SqlCeConnection;

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

                            using (var altercmd = new SqlCeCommand(alter.ToString(), connection as SqlCeConnection))
                            {
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
    }
}
