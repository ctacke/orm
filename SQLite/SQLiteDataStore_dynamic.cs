using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

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
    partial class SQLiteDataStore
    {
        public override void RegisterDynamicEntity(DynamicEntityDefinition entityDefinition)
        {
            base.RegisterDynamicEntity(entityDefinition, false);
        }

        private void OnUpdateDynamicEntity(DynamicEntity item)
        {
            bool changeDetected = false;

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

                    using (var updateCommand = GetNewCommandObject())
                    {
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
            }
            finally
            {
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
                    // if it's an ID field, don't set it
                    if(field.Name == item.KeyField) continue;
                    var paramName = ParameterPrefix + field.Name;

                    if (!command.Parameters.Contains(paramName))
                    {
                        // is this a key field that got missed?  This is unlikely (it's actually a bug) but
                        // this check is defensive coding
                        continue;
                    }

                    var @param = command.Parameters[paramName];

                    if (field.Value is TimeSpan)
                    {
                        @param.Value = ((TimeSpan)field.Value).Ticks;
                    }
                    else
                    {
                        switch (@param.DbType)
                        {
                            case DbType.DateTime:
                                @param.Value = Convert.ToDateTime(field.Value);
                                break;
                            default:
                                @param.Value = field.Value;
                                break;
                        }
                    }
                }

                command.ExecuteNonQuery();

                // did we have a PK field?  If so, we need to update that value in the item
                if (Entities[entityName].EntityAttribute.KeyScheme == KeyScheme.Identity)
                {
                    var keyField = Entities[entityName].Fields.FirstOrDefault(f => f.IsPrimaryKey);

                    if (keyField != null)
                    {
                        if (Entities[entityName].EntityAttribute.KeyScheme == KeyScheme.Identity)
                        {
                            item.Fields[keyField.FieldName] = GetIdentity(connection);
                        }
                    }
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

                    cmd.CommandText = string.Format("SELECT 1 FROM sqlite_master WHERE tbl_name='{0}' AND sql LIKE '%AUTOINCREMENT%'", entityName);

                    var autoIncrement = cmd.ExecuteScalar();

                    // TODO: handle index metadata (ascending/descending, unique, etc)
                    // PRAGMA index_list(TABLENAME)
                    // seq | name | unique
                    // PRAGMA index_info(INDEXNAME)
                    // seqno | cid | name | 


                    var entityDefinition = new DynamicEntityDefinition(entityName, fields);

                    try
                    {
                        if (Convert.ToInt64(autoIncrement) == 1L)
                        {
                            entityDefinition.EntityAttribute.KeyScheme = KeyScheme.Identity;
                        }
                    }
                    catch
                    {
                        // not auto-increment
                    }

                    RegisterEntityInfo(entityDefinition);

                    return entityDefinition;
                }
            }
            finally
            {
                DoneWithConnection(connection, true);
            }
        }

        public override IEnumerable<DynamicEntity> Fetch(string entityName, int fetchCount, int firstRowOffset, string sortField, FieldSearchOrder sortOrder, FilterCondition filter, bool fillReferences)
        {
            // yes, this is very limited in scope capability, but it's purpose-built for a specific use-case (and better than no functionality at all)

            if (fillReferences) throw new NotSupportedException("References not currently supported with this version of Fetch.");
            if (filter != null) throw new NotSupportedException("Filters not currently supported with this version of Fetch.  Try post-filtering with LINQ");

            var entities = new List<DynamicEntity>();

            if (!Entities.Contains(entityName))
            {
                // check to see if the underlying table exists
                // if it does, add to the Entities and continue the query
                if (DiscoverDynamicEntity(entityName) == null)
                {
                    return entities;
                }
            }

            var sql = string.Format("SELECT * FROM {0}", entityName);

            if (!string.IsNullOrEmpty(sortField))
            {
                sql += string.Format(" ORDER BY {0} {1}", sortField, sortOrder == FieldSearchOrder.Descending ? "DESC" : "ASC");
            }
            else if (sortOrder != FieldSearchOrder.NotSearchable)
            {
                if (Entities[entityName].Fields.KeyField != null)
                {
                    sql+= string.Format(" ORDER BY {0} {1}", Entities[entityName].Fields.KeyField.FieldName, sortOrder == FieldSearchOrder.Descending ? "DESC" : "ASC");
                }
            }

            sql += string.Format(" LIMIT {0}", fetchCount);

            if (firstRowOffset > 0)
            {
                sql += string.Format(" OFFSET {0}", firstRowOffset);
            }

            var connection = GetConnection(false);
            try
            {
                string keyName = null;

                if((m_entities.Contains(entityName)) && (m_entities[entityName].Fields.KeyField != null))
                {
                    keyName = m_entities[entityName].Fields.KeyField.FieldName;
                }

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

                            if(keyName != null)
                            {
                                e.KeyField = keyName;
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
            return Fetch(entityName, fetchCount, 0, null, FieldSearchOrder.NotSearchable, null, false);
        }
    }
}
