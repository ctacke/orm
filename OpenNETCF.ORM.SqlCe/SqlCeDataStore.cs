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
            var itemType = item.GetType();
            string entityName = m_entities.GetNameForType(itemType);

            if (entityName == null)
            {
                throw new EntityNotFoundException(item.GetType());
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
                    command.CommandText = entityName;
                    command.CommandType = CommandType.TableDirect;

                    using (var results = command.ExecuteResultSet(ResultSetOptions.Updatable))
                    {
                        var record = results.CreateRecord();

                        var keyScheme = Entities[entityName].EntityAttribute.KeyScheme;

                        foreach (var field in Entities[entityName].Fields)
                        {
                            if((keyScheme == KeyScheme.Identity) && field.IsPrimaryKey)
                            {
                                identity = field;
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
                                    record.SetValue(field.Ordinal, DBNull.Value);
                                }
                                else
                                {
                                    record.SetValue(field.Ordinal, value);
                                }
                            }
                            else if (field.IsRowVersion)
                            {
                                // read-only, so do nothing
                            }
                            else if (field.PropertyInfo.PropertyType.UnderlyingTypeIs<TimeSpan>())
                            {
                                // SQL Compact doesn't support Time, so we're convert to a DateTime both directions
                                var value = field.PropertyInfo.GetValue(item, null);

                                if (value == null)
                                {
                                    record.SetValue(field.Ordinal, DBNull.Value);
                                }
                                else
                                {
                                    var timespanTicks = ((TimeSpan)value).Ticks;
                                    record.SetValue(field.Ordinal, timespanTicks);
                                }
                            }
                            else
                            {
                                var value = field.PropertyInfo.GetValue(item, null);
                                record.SetValue(field.Ordinal, value);
                            }
                        }

                        results.Insert(record);

                        // did we have an identity field?  If so, we need to update that value in the item
                        if (identity != null)
                        {
                            var id = GetIdentity(connection);
                            identity.PropertyInfo.SetValue(item, id, null);
                        }

                        if (insertReferences)
                        {
                            // cascade insert any References
                            // do this last because we need the PK from above
                            foreach (var reference in Entities[entityName].References)
                            {
                                var valueArray = reference.PropertyInfo.GetValue(item, null);
                                if (valueArray == null) continue;

                                var fk = Entities[entityName].Fields[reference.ReferenceField].PropertyInfo.GetValue(item, null);

                                string et = null;

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
                                            // TODO: see if PK field value == -1
                                            isNew = keyValue.Equals(-1);
                                            break;
                                        case KeyScheme.GUID:
                                            // TODO: see if PK field value == null
                                            isNew = keyValue.Equals(null);
                                            break;
                                    }

                                    if (isNew)
                                    {
                                        Entities[et].Fields[reference.ReferenceField].PropertyInfo.SetValue(element, fk, null);
                                        Insert(element);
                                    }
                                }
                            }
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

        protected override IDataParameter CreateParameterObject(string parameterName, object parameterValue)
        {
            return new SqlCeParameter(parameterName, parameterValue);
        }

        private int GetIdentity(IDbConnection connection)
        {
            using (var command = new SqlCeCommand("SELECT @@IDENTITY", connection as SqlCeConnection))
            {
                object id = command.ExecuteScalar();
                return Convert.ToInt32(id);
            }
        }

        protected override string GetPrimaryKeyIndexName(string entityName)
        {
            var connection = GetConnection(true);
            try
            {
                string sql = string.Format("SELECT INDEX_NAME FROM information_schema.indexes WHERE (TABLE_NAME = '{0}') AND (PRIMARY_KEY = 1)", entityName);

                using (var command = GetNewCommandObject())
                {
                    command.CommandText = sql;
                    command.Connection = connection;
                    return command.ExecuteScalar() as string;
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

        private string ConnectionString
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

        protected void ValidateTable(IDbConnection connection, EntityInfo entity)
        {
            using (var command = new SqlCeCommand())
            {
                command.Connection = connection as SqlCeConnection;

                // first make sure the table exists
                var sql = string.Format("SELECT COUNT(*) FROM information_schema.tables WHERE table_name = '{0}'", entity.EntityAttribute.NameInStore);

                command.CommandText = sql;

                var count = Convert.ToInt32(command.ExecuteScalar());

                if (count == 0)
                {
                    CreateTable(connection, entity);
                }
                else
                {
                    foreach (var field in entity.Fields)
                    {
                        if (ReservedWords.Contains(field.FieldName, StringComparer.InvariantCultureIgnoreCase))
                        {
                            throw new ReservedWordException(field.FieldName);
                        }

                        // yes, I realize hard-coded ordinals are not a good practice, but the SQL isn't changing, it's method specific
                        sql = string.Format("SELECT column_name, "  // 0
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
}
