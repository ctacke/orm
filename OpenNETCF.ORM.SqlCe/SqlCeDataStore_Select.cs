using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.SqlServerCe;
using System.Threading;

namespace OpenNETCF.ORM
{
    partial class SqlCeDataStore
    {
        private const int CommandCacheMaxLength = 10;
        private Dictionary<string, SqlCeCommand> m_commandCache = new Dictionary<string, SqlCeCommand>();

        /// <summary>
        /// Determines if the ORM engine should be allowed to cache commands of not.  If you frequently use the same FilterConditions on a Select call to a single entity, 
        /// using the command cache can improve performance by preventing the underlying SQL Compact Engine from recomputing statistics.
        /// </summary>
        public bool UseCommandCache { get; set; }

        public void ClearCommandCache()
        {
            lock (m_commandCache)
            {
                foreach (var cmd in m_commandCache)
                {
                    cmd.Value.Dispose();
                }
                m_commandCache.Clear();
            }
        }

        /// <summary>
        /// Retrieves a single entity instance from the DataStore identified by the specified primary key value
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="primaryKey"></param>
        /// <returns></returns>
        public override T Select<T>(object primaryKey)
        {
            return (T)Select(typeof(T), null, primaryKey, -1, -1, true).FirstOrDefault();
        }

        public override T Select<T>(object primaryKey, bool fillReferences)
        {
            return (T)Select(typeof(T), null, primaryKey, -1, -1, fillReferences).FirstOrDefault();
        }

        /// <summary>
        /// Retrieves all entity instances of the specified type from the DataStore
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public override T[] Select<T>()
        {
            var type = typeof(T);
            var items = Select(type, null, null, -1, 0);
            return items.Cast<T>().ToArray();
        }

        /// <summary>
        /// Retrieves all entity instances of the specified type from the DataStore
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public override T[] Select<T>(bool fillReferences)
        {
            var type = typeof(T);
            var items = Select(type, null, null, -1, 0, fillReferences);
            return items.Cast<T>().ToArray();
        }

        /// <summary>
        /// Retrieves all entity instances of the specified type from the DataStore
        /// </summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        public override object[] Select(Type entityType)
        {
            return Select(entityType, true);
        }

        public override object[] Select(Type entityType, bool fillReferences)
        {
            var items = Select(entityType, null, null, -1, 0, fillReferences);
            return items.ToArray();
        }

        public override T[] Select<T>(string searchFieldName, object matchValue)
        {
            return Select<T>(searchFieldName, matchValue, true);
        }

        public override T[] Select<T>(string searchFieldName, object matchValue, bool fillReferences)
        {
            var type = typeof(T);
            var items = Select(type, searchFieldName, matchValue, -1, 0, fillReferences);
            return items.Cast<T>().ToArray();
        }

        public override T[] Select<T>(IEnumerable<FilterCondition> filters)
        {
            return Select<T>(filters, true);
        }

        public override T[] Select<T>(IEnumerable<FilterCondition> filters, bool fillReferences)
        {
            var objectType = typeof(T);
            return Select(objectType, filters, -1, 0, fillReferences).Cast<T>().ToArray();
        }

        private object[] Select(Type objectType, string searchFieldName, object matchValue, int fetchCount, int firstRowOffset)
        {
            return Select(objectType, searchFieldName, matchValue, fetchCount, firstRowOffset, true);
        }

        private object[] Select(Type objectType, string searchFieldName, object matchValue, int fetchCount, int firstRowOffset, bool fillReferences)
        {
            string entityName = m_entities.GetNameForType(objectType);
            FilterCondition filter = null;

            if (searchFieldName == null)
            {
                if (matchValue != null)
                {
                    CheckPrimaryKeyIndex(entityName);

                    // searching on primary key
                    filter = new SqlFilterCondition
                    {
                        FieldName = Entities[entityName].PrimaryKeyIndexName,
                        Operator = FilterCondition.FilterOperator.Equals,
                        Value = matchValue,
                        PrimaryKey = true
                    };
                }
            }
            else
            {
                filter = new FilterCondition
                {
                    FieldName = searchFieldName,
                    Operator = FilterCondition.FilterOperator.Equals,
                    Value = matchValue
                };
            }

            return Select(
                objectType,
                (filter == null) ? null :
                    new FilterCondition[]
                    {
                        filter
                    },
                fetchCount,
                firstRowOffset,
                fillReferences);
        }

        private SqlCeCommand GetSelectCommand(string entityName, IEnumerable<FilterCondition> filters, out bool tableDirect)
        {
            tableDirect = true;
            var buildFilter = false;
            string indexName = null;

            if (filters != null)
            {
                if (filters.Count() == 1)
                {
                    var filter = filters.First();

                    if (!(filter is SqlFilterCondition))
                    {
                        var field = Entities[entityName].Fields[filter.FieldName];

                        if (!field.IsPrimaryKey)
                        {
                            if (field.SearchOrder == FieldSearchOrder.NotSearchable)
                            {
                                buildFilter = true;
                            }
                            else
                            {
                                indexName = string.Format("ORM_IDX_{0}_{1}_{2}", entityName, filter.FieldName,
                                    field.SearchOrder == FieldSearchOrder.Descending ? "DESC" : "ASC");

                                // build the index if it's not there
                                VerifyIndex(entityName, filter.FieldName, field.SearchOrder, null);
                            }
                        }
                    }
                }
                else if (filters.Count() >= 1)
                {
                    var filter = filters.First() as SqlFilterCondition;

                    if (filter == null || !filter.PrimaryKey)
                    {
                        buildFilter = true;
                    }
                }
            }

            if (buildFilter)
            {
                tableDirect = false;
                return BuildFilterCommand(entityName, filters);
            }

            return new SqlCeCommand()
            {
                CommandText = entityName,
                CommandType = CommandType.TableDirect,
                IndexName = indexName ?? Entities[entityName].PrimaryKeyIndexName
            };
        }

        private SqlCeCommand BuildFilterCommand(string entityName, IEnumerable<FilterCondition> filters)
        {
            return BuildFilterCommand(entityName, filters, false);
        }

        private SqlCeCommand BuildFilterCommand(string entityName, IEnumerable<FilterCondition> filters, bool isCount)
        {
            var command = new SqlCeCommand();
            command.CommandType = CommandType.Text;
            var @params = new List<SqlCeParameter>();

            StringBuilder sb;

            if (isCount)
            {
                sb = new StringBuilder(string.Format("SELECT COUNT(*) FROM {0}", entityName));
            }
            else
            {
                sb = new StringBuilder(string.Format("SELECT * FROM {0}", entityName));
            }

            for (int i = 0; i < filters.Count(); i++)
            {
                sb.Append(i == 0 ? " WHERE " : " AND ");

                var filter = filters.ElementAt(i);
                sb.Append("[" + filter.FieldName + "]");

                switch (filters.ElementAt(i).Operator)
                {
                    case FilterCondition.FilterOperator.Equals:
                        if ((filter.Value == null) || (filter.Value == DBNull.Value))
                        {
                            sb.Append(" IS NULL ");
                            continue;
                        }
                        sb.Append(" = ");
                        break;
                    case FilterCondition.FilterOperator.Like:
                        sb.Append(" LIKE ");
                        break;
                    case FilterCondition.FilterOperator.LessThan:
                        sb.Append(" < ");
                        break;
                    case FilterCondition.FilterOperator.GreaterThan:
                        sb.Append(" > ");
                        break;
                    default:
                        throw new NotSupportedException();
                }

                string paramName = string.Format("@p{0}", i);
                sb.Append(paramName);

                var param = new SqlCeParameter(paramName, filter.Value ?? DBNull.Value);
                @params.Add(param);
            }
            var sql = sb.ToString();
            command.CommandText = sql;
            command.Parameters.AddRange(@params.ToArray());

            if (UseCommandCache)
            {
                lock (m_commandCache)
                {
                    if (m_commandCache.ContainsKey(sql))
                    {
                        command.Dispose();
                        command = m_commandCache[sb.ToString()];

                        // use the cached command object, but we must copy over the new command parameter values
                        // or it will use the old ones
                        for(int p = 0 ; p < command.Parameters.Count ; p++)
                        {
                            command.Parameters[p].Value = @params[p].Value;
                        }
                    }
                    else
                    {
                        m_commandCache.Add(sql, command);

                        // trim the cache so it doesn't grow infinitely
                        if (m_commandCache.Count > CommandCacheMaxLength)
                        {
                            m_commandCache.Remove(m_commandCache.First().Key);
                        }
                    }
                }
            }

            return command;
        }

        private object[] Select(Type objectType, IEnumerable<FilterCondition> filters, int fetchCount, int firstRowOffset, bool fillReferences)
        {
            string entityName = m_entities.GetNameForType(objectType);

            if (entityName == null)
            {
                throw new EntityNotFoundException(objectType);
            }

            UpdateIndexCacheForType(entityName);

            var items = new List<object>();
            bool tableDirect;

            var connection = GetConnection(false);
            SqlCeCommand command = null;

            if (UseCommandCache)
            {
                Monitor.Enter(m_commandCache);
            }

            try
            {
                CheckOrdinals(entityName);

                command = GetSelectCommand(entityName, filters, out tableDirect);
                command.Connection = connection as SqlCeConnection;

                int searchOrdinal = -1;
                ResultSetOptions options = ResultSetOptions.Scrollable;

                object matchValue = null;
                string matchField = null;

                if (tableDirect) // use index
                {
                    if ((filters != null) && (filters.Count() > 0))
                    {
                        var filter = filters.First();

                        matchValue = filter.Value ?? DBNull.Value;
                        matchField = filter.FieldName;

                        var sqlfilter = filter as SqlFilterCondition;
                        if ((sqlfilter != null) && (sqlfilter.PrimaryKey))
                        {
                            searchOrdinal = Entities[entityName].PrimaryKeyOrdinal;
                        }
                    }

                    // we need to ensure that the search value does not exceed the length of the indexed
                    // field, else we'll get an exception on the Seek call below
                    var indexInfo = GetIndexInfo(command.IndexName);
                    if (indexInfo != null)
                    {
                        if (indexInfo.MaxCharLength > 0)
                        {
                            var value = (string)matchValue;
                            if (value.Length > indexInfo.MaxCharLength)
                            {
                                matchValue = value.Substring(0, indexInfo.MaxCharLength);
                            }
                        }
                    }
                }

                using (var results = command.ExecuteResultSet(options))
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

                            if (tableDirect)
                            {
                                results.Seek(DbSeekOptions.FirstEqual, new object[] { matchValue });
                            }
                        }

                        while (results.Read())
                        {
                            if (currentOffset < firstRowOffset)
                            {
                                currentOffset++;
                                continue;
                            }

                            if (tableDirect && (matchValue != null))
                            {
                                // if we have a match value, we'll have seeked to the first match above
                                // then at this point the first non-match means we have no more matches, so
                                // we can exit out once we hit the first non-match.

                                // For string we want a case-insensitive search, so it's special-cased here
                                if (matchValue is string)
                                {
                                    if (string.Compare((string)results[searchOrdinal], (string)matchValue, true) != 0)
                                    {
                                        break;
                                    }
                                }
                                else
                                {
                                    if (!results[searchOrdinal].Equals(matchValue))
                                    {
                                        break;
                                    }
                                }
                            }

                            object item = Activator.CreateInstance(objectType);
                            object rowPK = null;

                            // autofill references if desired
                            if (referenceFields == null)
                            {
                                referenceFields = Entities[entityName].References.ToArray();
                            }


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
                                    else if (field.IsRowVersion)
                                    {
                                        // sql stores this an 8-byte array
                                        field.PropertyInfo.SetValue(item, BitConverter.ToInt64((byte[])value, 0), null);
                                    }
                                    else if (field.PropertyInfo.PropertyType.UnderlyingTypeIs<TimeSpan>())
                                    {
                                        // SQL Compact doesn't support Time, so we're convert to ticks in both directions
                                        var valueAsTimeSpan = new TimeSpan((long)value);
                                        field.PropertyInfo.SetValue(item, valueAsTimeSpan, null);
                                    }
                                    else
                                    {
                                        field.PropertyInfo.SetValue(item, value, null);
                                    }
                                }
                                //Check if it is reference key to set, not primary.
                                ReferenceAttribute attr = referenceFields.Where(
                                    x => x.ReferenceField == field.FieldName).FirstOrDefault();

                                if (attr != null)
                                {
                                    rowPK = value;
                                }
                                if (field.IsPrimaryKey)
                                {
                                    rowPK = value;
                                }
                            }

                            if ((fillReferences) && (referenceFields.Length > 0))
                            {
                                //FillReferences(item, rowPK, referenceFields, true);
                                FillReferences(item, rowPK, referenceFields, false);
                            }

                            items.Add(item);

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
                    Monitor.Exit(m_commandCache);
                }

                FlushReferenceTableCache();
                DoneWithConnection(connection, false);
            }

            return items.ToArray();
        }
    }
}
