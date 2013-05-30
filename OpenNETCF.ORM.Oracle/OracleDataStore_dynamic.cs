using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenNETCF.ORM
{
    partial class OracleDataStore
    {
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

                    cmd.CommandText = string.Format(
                        "SELECT ac.name, ind.is_primary_key, ind.is_unique, ic.is_descending_key, col.collation_name " +
                        "FROM sys.indexes ind " +
                        "INNER JOIN sys.index_columns ic " +
                        "  ON  ind.object_id = ic.object_id and ind.index_id = ic.index_id " +
                        "INNER JOIN sys.columns col  " +
                        "  ON ic.object_id = col.object_id and ic.column_id = col.column_id  " +
                        "INNER JOIN sys.tables t  " +
                        "  ON ind.object_id = t.object_id " +
                        "INNER JOIN sys.columns ac " +
                        "  ON ac.object_id = col.object_id and ac.column_id = col.column_id " +
                        "WHERE t.name = '{0}'", entityName);

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
                        }
                    }


                    var entityDefinition = new DynamicEntityDefinition(entityName, fields);
                    RegisterEntityInfo(entityDefinition);
                }
            }
            finally
            {
                DoneWithConnection(connection, true);
            }
        }

        public override IEnumerable<DynamicEntity> Select(string entityName)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<DynamicEntity> Fetch(string entityName, int fetchCount)
        {
            throw new NotImplementedException();
        }

        public override DynamicEntity Select(string entityName, object primaryKey)
        {
            throw new NotImplementedException();
        }
    }
}
