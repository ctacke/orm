using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Oracle.DataAccess.Client;

namespace OpenNETCF.ORM
{
    partial class OracleDataStore
    {
        private DbType ParseDbType(string dbTypeName)
        {
            var test = dbTypeName.ToLower();

            switch (test)
            {
                case "number":
                    // todo: what about 64-bit?  Do we need to look at this?
                    return DbType.Int32;
                case "date":
                    return DbType.DateTime;
                case "nvarchar2":
                    return DbType.String;
                case "binary_float":
                    return DbType.Single;
                case "binary_double":
                    return DbType.Double;
                default:
                    throw new NotSupportedException(string.Format("Unknown/unsupported type name '{0}'", dbTypeName));
            }

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

                    cmd.CommandText = string.Format(
                        "SELECT COLUMN_NAME, NULLABLE, DATA_TYPE, DATA_PRECISION, DATA_SCALE FROM all_tab_cols "
                        + " WHERE UPPER(TABLE_NAME) = UPPER('{0}') ORDER BY COLUMN_ID", entityName);

                    var fields = new List<FieldAttribute>();

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var name = reader.GetString(0);
                            var nullable = string.Compare(reader.GetString(1), "Y", true) == 0;
                            var type = ParseDbType(reader.GetString(2));

                            var field = new FieldAttribute()
                            {
                                DataType = type,
                                FieldName = name,
                                AllowsNulls = nullable,
                            };

                            if (!reader.IsDBNull(3))
                            {
                                field.Precision = Convert.ToInt32(reader.GetValue(3));
                            }
                            if (!reader.IsDBNull(4))
                            {
                                field.Scale = Convert.ToInt32(reader.GetValue(4));
                            }

                            fields.Add(field);
                        }
                    }

                    //CONSTRAINT_TYPE (from 11gR2 docs)
                    //C - Check constraint on a table
                    //P - Primary key
                    //U - Unique key
                    //R - Referential integrity
                    //V - With check option, on a view
                    //O - With read only, on a view
                    //H - Hash expression
                    //F - Constraint that involves a REF column
                    //S - Supplemental logging
                    
                    // INDEX_TYPE
                    // ASC == NORMAL
                    // DESC == FUNCTION-BASED NORMAL

                    cmd.CommandText = string.Format(
                        "SELECT cons.constraint_type, cons.constraint_name, cols.column_name, i.index_type " +
                        "FROM all_constraints cons " +
                        "LEFT OUTER JOIN all_cons_columns cols " +
                        "ON cons.table_name = cols.table_name " +
                        "LEFT OUTER JOIN all_indexes i " +
                        "ON cons.index_name = i.index_name " +
                        "WHERE UPPER(cols.table_name) = UPPER('{0}')"
                        , entityName);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var column = reader.GetString(2);
                            var ct = reader.GetString(0).TrimEnd();
                            var pk = false;
                            var unique = false;
 
                            switch (ct)
                            {
                                case "P":
                                    pk = true;
                                    unique = true;
                                    break;
                                case "U":
                                    unique = true;
                                    break;
                            }

                            var field = fields.FirstOrDefault(f => f.FieldName == column);
                            if (pk)
                            {
                                field.IsPrimaryKey = true;
                            }
                            else
                            {
                                var isdescending = reader.GetString(3).IndexOf("FUNTION-BASED", StringComparison.InvariantCultureIgnoreCase) >= 0;
                                field.SearchOrder = isdescending ? FieldSearchOrder.Descending : FieldSearchOrder.Ascending;
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

        private void OnInsertDynamicEntity(DynamicEntity item, bool insertReferences)
        {
            var connection = GetConnection(false);

            try
            {
                var entityName = item.EntityName;

                var command = GetInsertCommand(entityName);
                command.Connection = connection as OracleConnection;
                command.Transaction = CurrentTransaction as OracleTransaction;

                foreach (var field in item.Fields)
                {
                    if (field.Value == null)
                    {
                        command.Parameters[field.Name].Value = DBNull.Value;
                    }
                    else
                    {
                        command.Parameters[field.Name].Value = field.Value;
                    }
                }

                // did we have an identity field?  If so, we need to update that value in the item
                var keyField = Entities[entityName].Fields.FirstOrDefault(f => f.IsPrimaryKey);
                var keyScheme = Entities[entityName].EntityAttribute.KeyScheme;

                if (keyScheme != KeyScheme.Identity)
                {
                    command.ExecuteNonQuery();
                }
                else
                {
                    var idParameter = new OracleParameter(":LASTID", OracleDbType.Int32);
                    idParameter.Direction = ParameterDirection.Output;
                    command.Parameters.Add(idParameter);

                    command.ExecuteNonQuery();

                    item.Fields[keyField.FieldName] = idParameter.Value;
                }
            }
            finally
            {
                DoneWithConnection(connection, false);
            }
        }

        private void OnUpdateDynamicEntity(DynamicEntity item)
        {
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
                            bool changeDetected = false;

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
                                        insertCommand.Parameters.Add(CreateParameterObject(ParameterPrefix + field.Name, value));
                                    }
                                }
                            }

                            // only execute if a change occurred
                            if (changeDetected)
                            {
                                // remove the trailing comma and append the filter
                                updateSQL.Length -= 2;
                                updateSQL.AppendFormat(" WHERE {0} = {1}keyparam", keyField, ParameterPrefix);
                                insertCommand.Parameters.Add(CreateParameterObject(ParameterPrefix + "keyparam", keyValue));
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

        public override IEnumerable<DynamicEntity> Fetch(string entityName, int fetchCount)
        {
            throw new NotImplementedException();
        }
    }
}
