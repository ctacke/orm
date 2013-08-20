using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

#if ANDROID
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
    partial class SQLiteDataStore
    {
        private void OnUpdateDynamicEntity(DynamicEntity item)
        {
            bool changeDetected = false;
            var updateCommand = GetNewCommandObject();
            var connection = GetConnection(false);

            var entityName = item.EntityName;

            try
            {
                using (var command = GetNewCommandObject())
                {
                    var keyField = Entities[entityName].Fields.FirstOrDefault(f => f.IsPrimaryKey).FieldName;
                    var keyValue = item.Fields[keyField];

                    command.Connection = connection;

                    command.CommandText = string.Format("SELECT * FROM {0} WHERE {1} = ?",
                        entityName,
                        Entities[entityName].Fields.KeyField.FieldName);

                    command.CommandType = CommandType.Text;
                    command.Parameters.Add(CreateParameterObject(ParameterPrefix + "keyparam", keyValue));
                    command.Transaction = CurrentTransaction;

                    var updateSQL = new StringBuilder(string.Format("UPDATE {0} SET ", entityName));

                    using (var reader = command.ExecuteReader() as SQLiteDataReader)
                    {

                        if (!reader.HasRows)
                        {
                            // TODO: the PK value has changed - we need to store the original value in the entity or diallow this kind of change
                            throw new RecordNotFoundException("Cannot locate a record with the provided primary key.  You cannot update a primary key value through the Update method");
                        }

                        reader.Read();

                        // update the values
                        foreach (var field in item.Fields)
                        {
                            // do not update PK fields
                            if ((keyField != null) && (field.Name == keyField))
                            {
                                continue;
                            }

                            var value = field.Value;

                            if (reader[field.Name] != value)
                            {
                                changeDetected = true;

                                if (value == null)
                                {
                                    updateSQL.AppendFormat("{0}=NULL, ", field.Name);
                                }
                                else
                                {
                                    updateSQL.AppendFormat("{0}=?, ", field.Name);
                                    updateCommand.Parameters.Add(CreateParameterObject(field.Name, value));
                                }
                            }
                        }
                    }

                    // only execute if a change occurred
                    if (changeDetected)
                    {
                        // remove the trailing comma and append the filter
                        updateSQL.Length -= 2;
                        updateSQL.AppendFormat(" WHERE {0} = ?", keyField);
                        updateCommand.Parameters.Add(CreateParameterObject("keyparam", keyValue));
                        updateCommand.CommandText = updateSQL.ToString();
                        updateCommand.Connection = connection;
                        updateCommand.Transaction = CurrentTransaction;
                        updateCommand.ExecuteNonQuery();
                    }
                }
            }
            finally
            {
                updateCommand.Dispose();
                DoneWithConnection(connection, false);
            }
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
                    command.CommandText = string.Format("DELETE FROM {0} WHERE {1} = ?", entityName, fieldName);
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

        private void OnInsertDynamicEntity(DynamicEntity item, bool insertReferences)
        {
            var connection = GetConnection(false);

            try
            {
                var entityName = item.EntityName;

                var command = GetInsertCommand(entityName);
                command.Connection = connection as SQLiteConnection;
                command.Transaction = CurrentTransaction as SQLiteTransaction;

                foreach (var field in item.Fields)
                {
                    command.Parameters[ParameterPrefix + field.Name].Value = field.Value;
                }

                command.ExecuteNonQuery();

                // did we have an identity field?  If so, we need to update that value in the item
                var keyField = Entities[entityName].Fields.FirstOrDefault(f => f.IsPrimaryKey);

                if (keyField != null)
                {
                    item.Fields[keyField.FieldName] = GetIdentity(connection);
                }
            }
            finally
            {
                DoneWithConnection(connection, false);
            }
        }

        public override IEnumerable<DynamicEntity> Select(string entityName)
        {
            return Select(entityName, typeof(DynamicEntity), null, -1, -1, false).Cast<DynamicEntity>();
        }

        public override DynamicEntity Select(string entityName, object primaryKey)
        {
            var filter = new SqlFilterCondition
            {
                FieldName = (Entities[entityName] as SqlEntityInfo).PrimaryKeyIndexName,
                Operator = FilterCondition.FilterOperator.Equals,
                Value = primaryKey,
                PrimaryKey = true
            };

            return (DynamicEntity)Select(entityName, typeof(DynamicEntity), new FilterCondition[] { filter }, -1, -1, false).FirstOrDefault();
        }

        public override void DiscoverDynamicEntity(string entityName)
        {
            if (!TableExists(entityName))
            {
                throw new EntityNotFoundException(entityName);
            }

            var connection = GetConnection(true);
            try
            {
                using (var cmd = GetNewCommandObject())
                {
                    cmd.Connection = connection;
                    cmd.Transaction = CurrentTransaction;

                    cmd.CommandText = string.Format("PRAGMA table_info({0})", entityName);

                    // cid, name, type, notnull, dflt_value, pk
                    var fields = new List<FieldAttribute>();

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var cid = reader.GetInt64(0);
                            var name = reader.GetString(1);
                            var type = reader.GetString(2).ParseToDbType();
                            var nullable = reader.GetInt64(3) == 0;
                            // 4 == default value - TODO
                            var isPK = reader.GetInt64(5) == 1;

                            var field = new FieldAttribute()
                            {
                                DataType = type,
                                FieldName = name,
                                AllowsNulls = nullable,
                                IsPrimaryKey = isPK
                            };

                            fields.Add(field);
                        }
                    }

                    // TODO: handle index metadata (ascending/descending, unique, etc)
                    // PRAGMA index_list(TABLENAME)
                    // seq | name | unique
                    // PRAGMA index_info(INDEXNAME)
                    // seqno | cid | name | 


                    var entityDefinition = new DynamicEntityDefinition(entityName, fields);
                    RegisterEntityInfo(entityDefinition);
                }
            }
            finally
            {
                DoneWithConnection(connection, true);
            }
        }

        public IEnumerable<DynamicEntity> Fetch(string entityName, int fetchCount, int firstRowOffset, string sortField, FieldSearchOrder sortOrder, FilterCondition filter, bool fillReferences)
        {
            // yes, this is very limited in scope capability, but it's purpose-built for a specific use-case (and better than no functionality at all)

            if (firstRowOffset > 0) throw new NotSupportedException("non-zero rowOffset not currently supported with this version of Fetch");
            if (fillReferences) throw new NotSupportedException("References not currently supported with this version of Fetch.");
            if (filter != null) throw new NotSupportedException("Filters not currently supported with this version of Fetch.  Try post-filtering with LINQ");

            var sql = string.Format("SELECT TOP {0} * FROM {1} ", fetchCount, entityName);

            if (!string.IsNullOrEmpty(sortField))
            {
                sql += string.Format("ORDER BY {0} {1}", sortField, sortOrder == FieldSearchOrder.Descending ? "DESC" : "ASC");
            }

            var connection = GetConnection(false);
            try
            {
                var entities = new List<DynamicEntity>();

                using (var command = GetNewCommandObject())
                {
                    command.CommandType = CommandType.Text;
                    command.Connection = connection;
                    command.CommandText = sql;

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var e = new DynamicEntity(entityName);
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                e.Fields.Add(reader.GetName(i), reader.GetValue(i));
                            }
                            entities.Add(e);
                        }
                    }
                }

                return entities;
            }
            finally
            {
                DoneWithConnection(connection, false);
            }
        }

        public override IEnumerable<DynamicEntity> Fetch(string entityName, int fetchCount)
        {
            throw new NotSupportedException("Dynamic entities are not currently supported with this Provider.");
        }
    }
}
