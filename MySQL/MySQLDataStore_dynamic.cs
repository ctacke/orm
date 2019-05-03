using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using MySql.Data.MySqlClient;

namespace OpenNETCF.ORM
{
    partial class MySQLDataStore
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

                    command.CommandText = string.Format("SELECT * FROM {0} WHERE {1} = {2}keyparam",
                        entityName,
                        Entities[entityName].Fields.KeyField.FieldName,
                        ParameterPrefix);

                    command.CommandType = CommandType.Text;
                    command.Parameters.Add(CreateParameterObject(ParameterPrefix + "keyparam", keyValue));
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
                                    updateSQL.AppendFormat("{0}={1}{0}, ", field.Name, ParameterPrefix);
                                    updateCommand.Parameters.Add(CreateParameterObject(ParameterPrefix + field.Name, value));
                                }
                            }
                        }
                    }

                    // only execute if a change occurred
                    if (changeDetected)
                    {
                        // remove the trailing comma and append the filter
                        updateSQL.Length -= 2;
                        updateSQL.AppendFormat(" WHERE {0} = {1}keyparam", keyField, ParameterPrefix);
                        updateCommand.Parameters.Add(CreateParameterObject(ParameterPrefix + "keyparam", keyValue));
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

        private void OnInsertDynamicEntity(DynamicEntity item, bool insertReferences)
        {
            var connection = GetConnection(false);

            try
            {
                var entityName = item.EntityName;

                var command = GetInsertCommand(entityName);
                command.Connection = connection as MySqlConnection;
                command.Transaction = CurrentTransaction as MySqlTransaction;

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

        public override DynamicEntityDefinition DiscoverDynamicEntity(string entityName)
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

                    cmd.CommandText = string.Format("SELECT COLUMN_NAME, ORDINAL_POSITION, IS_NULLABLE, DATA_TYPE, NUMERIC_PRECISION, NUMERIC_SCALE FROM information_schema.columns WHERE TABLE_NAME = '{0}' ORDER BY ORDINAL_POSITION", entityName);

                    var fields = new List<FieldAttribute>();

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
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

                    // get PK info
                    cmd.CommandText = string.Format(
                        "SELECT COLUMN_NAME, CONSTRAINT_TYPE " +
                        "FROM information_schema.table_constraints t " +
                        "LEFT JOIN information_schema.key_column_usage k " +
                        "USING(constraint_name,table_schema,table_name) " +
                        "WHERE TABLE_NAME = '{0}'", entityName);

                    // result will be PRIMARY_KEY or UNIQUE

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var column = reader.GetString(0);
                            var t = reader.GetString(1);

                            var pk = t == "PRIMARY_KEY";
                            var unique = t == "UNIQUE";

                            var field = fields.FirstOrDefault(f => f.FieldName == column);
                            if (pk)
                            {
                                field.IsPrimaryKey = true;
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

        public override IEnumerable<DynamicEntity> Fetch(string entityName, int fetchCount, int firstRowOffset, string sortField, FieldSearchOrder sortOrder, FilterCondition filter, bool fillReferences)
        {
            // yes, this is very limited in scope capability, but it's purpose-built for a specific use-case (and better than no functionality at all)

            if (fillReferences) throw new NotSupportedException("References not currently supported with this version of Fetch.");
            if (filter != null) throw new NotSupportedException("Filters not currently supported with this version of Fetch.  Try post-filtering with LINQ");

            var sql = $"SELECT * FROM {entityName} ";

            if (!string.IsNullOrEmpty(sortField))
            {
                sql += string.Format("ORDER BY {0} {1} ", sortField, sortOrder == FieldSearchOrder.Descending ? "DESC" : "ASC");
            }

            sql += $"LIMIT {fetchCount} ";

            if (firstRowOffset > 0)
            {
                sql += $"OFFSET {firstRowOffset} ";
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
            return Fetch(entityName, fetchCount, 0, null, FieldSearchOrder.NotSearchable, null, false);
        }
    }
}
