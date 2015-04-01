using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using OpenNETCF.ORM.SqlServer;

namespace OpenNETCF.ORM
{
    partial class SqlServerDataStore
    {
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

                    command.CommandText = string.Format("SELECT * FROM {0} WHERE [{1}] = {2)keyparam",
                        entityName,
                        Entities[entityName].Fields.KeyField.FieldName,
                        ParameterPrefix);

                    command.CommandType = CommandType.Text;
                    command.Parameters.Add(CreateParameterObject(ParameterPrefix + "keyparam", keyValue));
                    command.Transaction = CurrentTransaction;

                    var updateSQL = new StringBuilder(string.Format("UPDATE {0} SET ", entityName));

                    using (var reader = command.ExecuteReader() as SqlDataReader)
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
                                        updateSQL.AppendFormat("{0}=@{0}, ", field.Name);
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

        private void OnInsertDynamicEntity(DynamicEntity item, bool insertReferences)
        {
            var connection = GetConnection(false);

            try
            {
                var entityName = item.EntityName;

                var command = GetInsertCommand(entityName);
                command.Connection = connection as SqlConnection;
                command.Transaction = CurrentTransaction as SqlTransaction;

                string keyFieldName = null;

                if (Entities[entityName].EntityAttribute.KeyScheme == KeyScheme.Identity)
                {
                    var keyField = Entities[entityName].Fields.FirstOrDefault(f => f.IsPrimaryKey);

                    if (keyField != null)
                    {
                        keyFieldName = keyField.FieldName;
                    }
                }

                foreach (var field in item.Fields)
                {
                    if (field.Name == keyFieldName) continue;
                    var p = command.Parameters[ParameterPrefix + field.Name] as SqlParameter;
                    
                    var value = ParseToSqlDbType(field.Value, p.SqlDbType);
                    var length = Entities[entityName].Fields[field.Name].Length;

                    if (value is string && length > 0)
                    {
                        value = (value as string).Truncate(length);
                    }

                    p.Value = value;

                }

                command.ExecuteNonQuery();

                // did we have an identity field?  If so, we need to update that value in the item
                if (Entities[entityName].EntityAttribute.KeyScheme == KeyScheme.Identity)
                {
                    if (keyFieldName != null)
                    {
                        item.Fields[keyFieldName] = GetIdentity(connection);
                    }
                }
            }
            finally
            {
                DoneWithConnection(connection, false);
            }
        }

        private object ParseToSqlDbType(object o, SqlDbType t)
        {
            if (o.Equals(DBNull.Value)) return DBNull.Value;
            if (o == null) return DBNull.Value;

            switch (t)
            {
                case SqlDbType.Int:
                    return Convert.ToInt32(o);
                case SqlDbType.BigInt:
                    return Convert.ToInt64(o);
                case SqlDbType.SmallInt:
                    return Convert.ToInt16(o);
                case SqlDbType.Bit:
                    return Convert.ToBoolean(o);
                case SqlDbType.NVarChar:
                    return o.ToString();
                case SqlDbType.Decimal:
                    return Convert.ToDecimal(o);
                case SqlDbType.Float:
                    return Convert.ToDouble(o);
                case SqlDbType.DateTime:
                    return Convert.ToDateTime(o);
                default:
                    throw new NotSupportedException();
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

                    cmd.CommandText = string.Format("SELECT COLUMN_NAME, ORDINAL_POSITION, IS_NULLABLE, DATA_TYPE, NUMERIC_PRECISION, NUMERIC_SCALE, CHARACTER_MAXIMUM_LENGTH "
                                                    + "FROM information_schema.columns WHERE TABLE_NAME = '{0}' ORDER BY ORDINAL_POSITION", entityName);

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

                            if (!reader.IsDBNull(6))
                            {
                                field.Length = Convert.ToInt32(reader.GetValue(6));
                            }

                            fields.Add(field);
                        }
                    }

                    cmd.CommandText = string.Format(
                        "SELECT ac.name, ind.is_primary_key, ind.is_unique, ic.is_descending_key, col.collation_name, idc.name " +
                        "FROM sys.indexes ind " +
                        "INNER JOIN sys.index_columns ic " +
                        "  ON  ind.object_id = ic.object_id and ind.index_id = ic.index_id " +
                        "INNER JOIN sys.columns col  " +
                        "  ON ic.object_id = col.object_id and ic.column_id = col.column_id  " +
                        "INNER JOIN sys.tables t  " +
                        "  ON ind.object_id = t.object_id " +
                        "INNER JOIN sys.identity_columns idc " +
                        "  ON col.object_id = idc.object_id and col.column_id = idc.column_id " +
                        "INNER JOIN sys.columns ac " +
                        "  ON ac.object_id = col.object_id and ac.column_id = col.column_id " +
                        "WHERE t.name = '{0}'", entityName);

                    string identiyColumn = null;

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
                                var isdescending = Convert.ToInt32(reader.GetValue(3));
                                field.SearchOrder = isdescending == 0 ? FieldSearchOrder.Ascending : FieldSearchOrder.Descending;
                            }
                            if (unique)
                            {
                                field.RequireUniqueValue = true;
                            }

                            identiyColumn = reader.GetString(5);
                        }
                    }


                    var entityDefinition = new DynamicEntityDefinition(
                        entityName, 
                        fields,
                        string.IsNullOrEmpty(identiyColumn) ? KeyScheme.None : KeyScheme.Identity);

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
            if (fillReferences) throw new NotSupportedException("References not currently supported with this version of Fetch.");
            if (filter != null) throw new NotSupportedException("Filters not currently supported with this version of Fetch.  Try post-filtering with LINQ");

            var v = ServerVersion;

            string sql;

            // TODO: get this working then revert the version number to 11 in the "if" startement below
            //       For now, all paths lead through the else condition
            if (v.Major >= 99) // sql server 2012 or later support OFFSET and FETCH
            {
                if (firstRowOffset <= 0)
                {
                    sql = string.Format("SELECT * FROM {0} ", entityName);

                    if (!string.IsNullOrEmpty(sortField))
                    {
                        sql += string.Format("ORDER BY {0} {1} ", sortField, sortOrder == FieldSearchOrder.Descending ? "DESC" : "ASC");
                    }

                    if (fetchCount > 0)
                    {
                        sql = sql.Replace("*", string.Format("TOP ({0}) *", fetchCount));
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(sortField))
                    {
                        sql = string.Format("DECLARE @orderrow varchar(500); "
                            + "SELECT @orderrow = column_name "
                            + "FROM information_schema.columns "
                            + "WHERE table_name = '{0}' "
                            + " AND ordinal_position = 1; "
                            + "SELECT * FROM {0} "
                            + "ORDER BY @orderrow "
                            + "OFFSET {1} ROWS "
                            , entityName
                            , firstRowOffset);

                        if (fetchCount > 0)
                        {
                            sql += string.Format("FETCH FIRST {0} ROWS ONLY", fetchCount);
                        }
                    }
                    else
                    {
                        sql = string.Format("SELECT * FROM {0} ", entityName);
                        sql += string.Format("ORDER BY {0} {1} ", sortField, sortOrder == FieldSearchOrder.Descending ? "DESC" : "ASC");
                        sql += string.Format("OFFSET {0} ROWS ", firstRowOffset);

                        if (fetchCount > 0)
                        {
                            sql += string.Format("FETCH FIRST {0} ROWS ONLY", fetchCount);
                        }
                    }
                }
            }
            else // pre-2012
            {
                if (firstRowOffset <= 0)
                {
                    sql = string.Format("SELECT * FROM {0} ", entityName);

                    if (!string.IsNullOrEmpty(sortField))
                    {
                        sql += string.Format("ORDER BY {0} {1} ", sortField, sortOrder == FieldSearchOrder.Descending ? "DESC" : "ASC");
                    }

                    if (fetchCount > 0)
                    {
                        sql = sql.Replace("*", string.Format("TOP ({0}) *", fetchCount));
                    }
                }
                else
                {
                    if(string.IsNullOrEmpty(sortField))
                    {
                        //DECLARE @orderrow varchar(500);

                        //SELECT @orderrow = column_name
                        //FROM information_schema.columns
                        //WHERE table_name = 'C00261854A503_Collector017'
                        //AND ordinal_position = 1


                        //;WITH results AS
                        //(
                        //    SELECT
                        //        *,
                        //        ROW_NUMBER() OVER (
                        //        ORDER BY 

                        //        @orderrow

                        //        ) AS orm_row_num
                        //    FROM C00261854A503_Collector017
                        //)
                        //SELECT TOP (100) *
                        //FROM results
                        //WHERE orm_row_num >= 100

                        sql = string.Format("DECLARE @orderrow varchar(500); "
                            + "SELECT @orderrow = column_name "
                            + "FROM information_schema.columns "
                            + "WHERE table_name = '{0}' "
                            + " AND ordinal_position = 1 "

                            + ";WITH results AS ("
                            + "SELECT *, "
                            + "ROW_NUMBER() OVER (ORDER BY @orderrow) AS orm_row_num "
                            + "FROM {0} ) "

                            + "{1} * "
                            + "FROM results "
                            + "WHERE orm_row_num >= {2}",

                            entityName,
                            fetchCount > 0 ? string.Format("SELECT TOP ({0}) ", fetchCount) : string.Empty,
                            firstRowOffset);
                    }
                    else
                    {
                        sql = string.Format(";WITH results AS ("
                            + "SELECT *, "
                            + "ROW_NUMBER() OVER (ORDER BY {0} {1}) AS orm_row_num "
                            + "FROM {2} ) "

                            + "{3} * "
                            + "FROM results "
                            + "WHERE orm_row_num >= {4}",

                            sortField,
                            sortOrder == FieldSearchOrder.Descending ? "DESC" : "ASC",
                            entityName,
                            fetchCount > 0 ? string.Format("SELECT TOP ({0}) ", fetchCount) : string.Empty,
                            firstRowOffset);
                    }                    
                }
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

                            // TODO: cache ordinals to improve perf
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                var name = reader.GetName(i);

                                // this is a temp column variable - don't return it
                                if (name == "orm_row_num") continue;
                                e.Fields.Add(name, reader.GetValue(i));
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
    }
}
