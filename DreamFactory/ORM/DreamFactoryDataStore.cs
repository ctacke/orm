using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenNETCF.DreamFactory;
using System.Reflection;
using System.Diagnostics;
using System.Data;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace OpenNETCF.ORM
{
    public class DreamFactoryDataStore : DataStore<EntityInfo>, ITableBasedStore
    {
        private Session m_session;

        public DreamFactoryDataStore(string dspRootAddress, string application, string username, string password, RemoteCertificateValidationCallback certificateCallback, bool useCompression)
        {
            m_session = new Session(dspRootAddress, application, username, password, certificateCallback, useCompression);

            m_session.Initialize();
        }

        public void Dispose()
        {
        }

        public override string Name
        {
            get { return "DreamFactory." + m_session.ApplicationName; }
        }

        protected override System.Data.DbType? PropertyTypeToDbType(Type propertyType)
        {
            if(propertyType.Equals(typeof(TimeSpan)))
            {
                return System.Data.DbType.Time;
            }

            return null;
        }

        protected override void AfterAddEntityType(Type entityType, bool ensureCompatibility)
        {
            var name = m_entities.GetNameForType(entityType);

            if ((m_entities[name].References != null) && (m_entities[name].References.Count > 0))
            {
                throw new NotSupportedException("References not currently supported for this Provider");
            }

            // this will exist because the caller inserted it
            var entity = Entities[name];

            if (!TableExists(entity.EntityAttribute.NameInStore))
            {
                CreateTable(entity);
            }
            else if (ensureCompatibility)
            {
                ValidateTable(entity);
            }
        }

        private static string[] ReservedWords = new string[]
        {
            "IDENTITY" ,"ENCRYPTION" ,"ORDER" ,"ADD" ,"END" ,"OUTER" ,"ALL" ,"ERRLVL" ,"OVER" ,"ALTER" ,"ESCAPE" ,"PERCENT" ,"AND" ,"EXCEPT" ,"PLAN" ,"ANY" ,"EXEC" ,"PRECISION" ,"AS" ,"EXECUTE" ,"PRIMARY" ,"ASC",
            "EXISTS" ,"PRINT" ,"AUTHORIZATION" ,"EXIT" ,"PROC" ,"AVG" ,"EXPRESSION" ,"PROCEDURE" ,"BACKUP" ,"FETCH" ,"PUBLIC" ,"BEGIN" ,"FILE" ,"RAISERROR" ,"BETWEEN" ,"FILLFACTOR" ,"READ" ,"BREAK" ,"FOR" ,"READTEXT",
            "BROWSE" ,"FOREIGN" ,"RECONFIGURE" ,"BULK" ,"FREETEXT" ,"REFERENCES" ,"BY" ,"FREETEXTTABLE" ,"REPLICATION" ,"CASCADE" ,"FROM" ,"RESTORE" ,"CASE" ,"FULL" ,"RESTRICT" ,"CHECK" ,"FUNCTION" ,"RETURN" ,"CHECKPOINT",
            "GOTO" ,"REVOKE" ,"CLOSE" ,"GRANT" ,"RIGHT" ,"CLUSTERED" ,"GROUP" ,"ROLLBACK" ,"COALESCE" ,"HAVING" ,"ROWCOUNT" ,"COLLATE" ,"HOLDLOCK" ,"ROWGUIDCOL" ,"COLUMN" ,"IDENTITY" ,"RULE",
            "COMMIT" ,"IDENTITY_INSERT" ,"SAVE" ,"COMPUTE" ,"IDENTITYCOL" ,"SCHEMA" ,"CONSTRAINT" ,"IF" ,"SELECT" ,"CONTAINS" ,"IN" ,"SESSION_USER" ,"CONTAINSTABLE" ,"INDEX" ,"SET" ,"CONTINUE" ,"INNER" ,"SETUSER",
            "CONVERT" ,"INSERT" ,"SHUTDOWN" ,"COUNT" ,"INTERSECT" ,"SOME" ,"CREATE" ,"INTO" ,"STATISTICS" ,"CROSS" ,"IS" ,"SUM" ,"CURRENT" ,"JOIN" ,"SYSTEM_USER" ,"CURRENT_DATE" ,"KEY" ,"TABLE" ,"CURRENT_TIME" ,"KILL",
            "TEXTSIZE" ,"CURRENT_TIMESTAMP" ,"LEFT" ,"THEN" ,"CURRENT_USER" ,"LIKE" ,"TO" ,"CURSOR" ,"LINENO" ,"TOP" ,"DATABASE" ,"LOAD" ,"TRAN" ,"DATABASEPASSWORD" ,"MAX" ,"TRANSACTION" ,"DATEADD" ,"MIN" ,"TRIGGER",
            "DATEDIFF" ,"NATIONAL" ,"TRUNCATE" ,"DATENAME" ,"NOCHECK" ,"TSEQUAL" ,"DATEPART" ,"NONCLUSTERED" ,"UNION" ,"DBCC" ,"NOT" ,"UNIQUE" ,"DEALLOCATE", "NULL", "UPDATE", "DECLARE", "NULLIF", "UPDATETEXT",
            "DEFAULT", "OF", "USE", "DELETE", "OFF", "USER", "DENY", "OFFSETS", "VALUES", "DESC", "ON", "VARYING", "DISK", "OPEN", "VIEW", "DISTINCT", "OPENDATASOURCE", "WAITFOR", "DISTRIBUTED", "OPENQUERY", "WHEN", 
            "DOUBLE", "OPENROWSET", "WHERE", "DROP", "OPENXML", "WHILE", "DUMP", "OPTION", "WITH", "ELSE", "OR", "WRITETEXT" 
        };

        private void CreateTable(IEntityInfo entity)
        {
            var fields = new List<Field>();

            foreach (var field in entity.Fields)
            {
                if (ReservedWords.Contains(field.FieldName, StringComparer.InvariantCultureIgnoreCase))
                {
                    throw new ReservedWordException(field.FieldName);
                }

                fields.Add(FieldFactory.GetFieldForAttribute(field, entity.EntityAttribute.KeyScheme));
            }

            var name = entity.EntityAttribute.NameInStore ?? entity.EntityName;
            m_session.Data.CreateTable(name, name, fields); 
        }

        public override void Drop(string entityName)
        {
            DropTable(entityName);
        }

        public void DropTable(string tableName)
        {
            m_session.Data.DeleteTable(tableName);
        }

        public bool TableExists(string tableName)
        {
            return m_session.Data.GetTable(tableName) != null;
        }

        public void TruncateTable(string tableName)
        {
            m_session.Data.GetTable(tableName).DeleteRecords();
        }

        public override void Delete<T>()
        {
            var name = Entities.GetNameForType(typeof(T));
            TruncateTable(name);
        }

        public override void Delete(string entityName, string fieldName, object matchValue)
        {
            var filter = string.Format("{0}='{1}'", fieldName, matchValue);
            var table = m_session.Data.GetTable(entityName);
            table.DeleteFilteredRecords(filter);
        }

        public override void Delete(string entityName, object primaryKey)
        {
            var table = m_session.Data.GetTable(entityName);
            table.DeleteRecords(primaryKey);
        }

        public override void Delete<T>(string fieldName, object matchValue)
        {
            var entityName = Entities.GetNameForType(typeof(T));
            Delete(entityName, fieldName, matchValue);
        }

        public override void OnDelete(object item)
        {
            var entityType = item.GetType();
            var entityName = GetEntityNameForInstance(item);
            var table = m_session.Data.GetTable(entityName);
            FieldAttribute idField;
            var key = GetEntityKeyValue(entityName, item, out idField);
            table.DeleteRecords(key);
        }

        public override T Select<T>(object primaryKey, bool fillReferences)
        {
            if (fillReferences)
            {
                throw new NotSupportedException("References not currently supported in this DataStore implementation");
            }

            return Select<T>(primaryKey);
        }

        public override T Select<T>(object primaryKey)
        {
            var entityType = typeof(T);
            var entityName = Entities.GetNameForType(entityType);
            var table = m_session.Data.GetTable(entityName);

            var record = table.GetRecords(primaryKey).FirstOrDefault();
            if (record == null) return default(T);

            return (T)RehydrateObjectFromEntityValueDictionary(entityType, entityName, record);
        }

        public override IEnumerable<object> Select(Type entityType, bool fillReferences)
        {
            if (fillReferences)
            {
                throw new NotSupportedException("References not currently supported in this DataStore implementation");
            }

            var entityName = Entities.GetNameForType(entityType);

            // see if the underlying schema exists
            if (entityName == null)
            {
                AddType(entityType);
            }

            // if it's still null, it's not there
            entityName = Entities.GetNameForType(entityType);
            if (entityName == null)
            {
                throw new EntityNotFoundException(string.Format("Type '{0}' is not registered with this store.", entityType.Name));
            }
            Table table;
            IEnumerable<object[]> records;

            try
            {
                table = m_session.Data.GetTable(entityName);
            }
            catch (Exception)
            {
                throw;
            }

            if (table == null) return null;

            try
            {
                records = table.GetRecords();
            }
            catch (Exception)
            {
                throw;
            }

            var items = new List<object>();

            foreach (var record in records)
            {
                var instance = RehydrateObjectFromEntityValueDictionary(entityType, entityName, record);
                items.Add(instance);
            }

            return items;
        }

        public override IEnumerable<T> Select<T>(string searchFieldName, object matchValue, bool fillReferences)
        {
            if (fillReferences)
            {
                throw new NotSupportedException("References not currently supported in this DataStore implementation");
            }

            var entityType = typeof(T);
            var entityName = Entities.GetNameForType(entityType);
            var table = m_session.Data.GetTable(entityName);

            var filter = string.Format("{0}='{1}'", searchFieldName, matchValue);
            var records = table.GetRecords(filter);

            var items = new List<object>();

            foreach (var record in records)
            {
                var instance = RehydrateObjectFromEntityValueDictionary(entityType, entityName, record);
                items.Add(instance);
            }

            return items.Cast<T>();
        }

        public override IEnumerable<object> Select(Type entityType)
        {
            return Select(entityType, false);
        }

        public override IEnumerable<T> Select<T>(string searchFieldName, object matchValue)
        {
            return Select<T>(searchFieldName, matchValue, false);
        }

        public override IEnumerable<T> Select<T>(bool fillReferences)
        {
            var type = typeof(T);
            var objects = Select(type, fillReferences);
            return objects.Cast<T>();
        }

        public override IEnumerable<T> Select<T>()
        {
            return Select<T>(false);
        }

        public override IEnumerable<T> Select<T>(IEnumerable<FilterCondition> filters, bool fillReferences)
        {
            if (fillReferences)
            {
                throw new NotSupportedException("References not currently supported in this DataStore implementation");
            }

            var entityType = typeof(T);
            var entityName = Entities.GetNameForType(entityType);
            var table = m_session.Data.GetTable(entityName);

            IEnumerable<object[]> records;

            if ((filters != null) && (filters.Count() > 0))
            {
                var filter = string.Join(" AND ", (from f in filters
                                                   select FilterToString(f)).ToArray());

                records = table.GetRecords(filter);
            }
            else
            {
                records = table.GetRecords();
            }

            var items = new List<object>();

            foreach (var record in records)
            {
                var instance = RehydrateObjectFromEntityValueDictionary(entityType, entityName, record);
                items.Add(instance);
            }

            return items.Cast<T>();
        }

        private string FilterToString(FilterCondition f)
        {
            var filter = new StringBuilder(f.FieldName);

            // These assume a SQLite-backed DSP
            // SQLite has some specific date and time requirements becasue it doesn't actually have a DateTime data type
            string formattedValue;

            if (f.Value is DateTime)
            {
                // YYYY-MM-DD HH:MM:SS
                formattedValue = string.Format("{0:yyyy-MM-dd HH:mm:ss}", f.Value);
            }
            else if (f.Value is TimeSpan)
            {
                // HH:MM:SS
                formattedValue = string.Format("{0:HH:mm:ss}", f.Value);
            }
            else
            {
                formattedValue = f.Value.ToString();
            }


            switch(f.Operator)
            {
                case FilterCondition.FilterOperator.Equals:
                    filter.AppendFormat(" = '{0}'", formattedValue);
                    break;
                case FilterCondition.FilterOperator.GreaterThan:
                    filter.AppendFormat(" > '{0}'", formattedValue);
                    break;
                case FilterCondition.FilterOperator.LessThan:
                    filter.AppendFormat(" < '{0}'", formattedValue);
                    break;
                case FilterCondition.FilterOperator.Like:
                    filter.AppendFormat(" LIKE '{0}'", formattedValue);
                    break;
            }

            return filter.ToString();
        }

        public override IEnumerable<T> Select<T>(IEnumerable<FilterCondition> filters)
        {
            return Select<T>(filters, false);
        }

        private string GetEntityNameForInstance(object item)
        {
            if (item is DynamicEntity)
            {
                return (item as DynamicEntity).EntityName;
            }

            var itemType = item.GetType();
            return m_entities.GetNameForType(itemType);
        }

        public override void OnInsert(object item, bool insertReferences)
        {
            if (insertReferences)
            {
                throw new NotSupportedException("References not currently supported in this DataStore implementation");
            }

            var entityName = GetEntityNameForInstance(item);

            if (entityName == null)
            {
                AddType(item.GetType());
            }

            entityName = GetEntityNameForInstance(item);

            if (entityName == null)
            {
                throw new Exception("Unknown and unregistered Entity type: " + item.GetType().Name);
            }

            var table = m_session.Data.GetTable(entityName);

            FieldAttribute idField;

            var values = GetEntityValueDictionary(item, out idField);

            object key;

            try
            {
                key = table.InsertRecord(values);
            }
            catch (InvalidSessionException)
            {
                m_session.Reconnect();
                key = table.InsertRecord(values);
            }

            if((key != null) && (idField != null))
            {
                // push the inserted key back into the item
                SetInstanceValue(idField, item, key);
            }
        }

        public override void OnUpdate(object item, bool cascadeUpdates, string fieldName)
        {
            var entityName = GetEntityNameForInstance(item);

            var table = m_session.Data.GetTable(entityName);

            FieldAttribute idField;
            var values = GetEntityValueDictionary(item, out idField);

            try
            {
                table.UpdateRecord(values);
            }
            catch (InvalidSessionException)
            {
                m_session.Reconnect();
                table.UpdateRecord(values);
            }
        }

        public override bool Contains(object item)
        {
            var entityType = item.GetType();
            var entityName = GetEntityNameForInstance(item);
            FieldAttribute idField;
            var key = GetEntityKeyValue(entityName, item, out idField);

            var table = m_session.Data.GetTable(entityName);

            var record = table.GetRecords(key).FirstOrDefault();

            return record != null;
        }

        private object GetEntityKeyValue(string entityName, object item, out FieldAttribute identityField)
        {
            identityField = null;

            foreach (var field in Entities[entityName].Fields)
            {
                if (field.IsPrimaryKey)
                {
                    identityField = field;
                    return field.PropertyInfo.GetValue(item, null);
                }
            }

            return null;
        }

        private Dictionary<string, object> GetEntityValueDictionary(object item, out FieldAttribute identityField)
        {
            var entityName = GetEntityNameForInstance(item);

            var values = new Dictionary<string, object>();

            identityField = null;

            if (item is DynamicEntity)
            {
                var de = item as DynamicEntity;

                foreach (var field in Entities[entityName].Fields)
                {
                    if ((field.IsPrimaryKey) && (Entities[entityName].EntityAttribute.KeyScheme == KeyScheme.Identity)) identityField = field;

                    var value = de.Fields[field.FieldName];

                    if ((field.AllowsNulls) && (value == null))
                    {
                        values.Add(field.FieldName, null);
                    }
                    else
                    {
                        switch (field.DataType)
                        {
                            case System.Data.DbType.Time:
                                values.Add(field.FieldName, ((TimeSpan)value).ToString());
                                break;
                            case System.Data.DbType.Date:
                            case System.Data.DbType.DateTime:
                            case System.Data.DbType.DateTime2:
                                values.Add(field.FieldName, Convert.ToDateTime(value).ToString("s"));
                                break;
                            default:
                                values.Add(field.FieldName, value);
                                break;
                        }
                    }
                }
            }
            else
            {
                foreach (var field in Entities[entityName].Fields)
                {
                    if (field.IsPrimaryKey) identityField = field;

                    var value = field.PropertyInfo.GetValue(item, null);

                    if ((field.AllowsNulls) && (value == null))
                    {
                        values.Add(field.FieldName, null);
                    }
                    else
                    {
                        switch (field.DataType)
                        {
                            case System.Data.DbType.Time:
                                values.Add(field.FieldName, ((TimeSpan)value).ToString());
                                break;
                            case System.Data.DbType.Date:
                            case System.Data.DbType.DateTime:
                            case System.Data.DbType.DateTime2:
                                values.Add(field.FieldName, Convert.ToDateTime(value).ToString("s"));
                                break;
                            default:
                                values.Add(field.FieldName, value);
                                break;
                        }
                    }
                }
            }
            return values;
        }

        private T RehydrateObjectFromEntityValueDictionary<T>(string entityName, object[] record)
            where T : class
        {
            return RehydrateObjectFromEntityValueDictionary(typeof(T), entityName, record) as T;
        }

        private DynamicEntity DynamicEntityFromEntityValueDictionary(string entityName, Field[] sourceFieldOrder, object[] record)
        {
            var instance = new DynamicEntity(entityName);

            if (!Entities.Contains(entityName))
            {
                DiscoverDynamicEntity(entityName);
            }

            var f = 0;
            foreach (var field in sourceFieldOrder)
            {
                try
                {
                    if (record[f] is string)
                    {
                        switch (field.TypeName.ParseToDbType())
                        {
                            case System.Data.DbType.Int16:
                            case System.Data.DbType.UInt16:
                                instance.Fields[field.Name] = Convert.ToInt16(record[f]);
                                break;
                            case System.Data.DbType.Int32:
                            case System.Data.DbType.UInt32:
                                instance.Fields[field.Name] = Convert.ToInt32(record[f]);
                                break;
                            case System.Data.DbType.Int64:
                            case System.Data.DbType.UInt64:
                                instance.Fields[field.Name] = Convert.ToInt64(record[f]);
                                break;
                            case System.Data.DbType.Double:
                                instance.Fields[field.Name] = Convert.ToDouble(record[f]);
                                break;
                            case System.Data.DbType.Single:
                                instance.Fields[field.Name] = Convert.ToSingle(record[f]);
                                break;
                            case System.Data.DbType.Decimal:
                                instance.Fields[field.Name] = Convert.ToDecimal(record[f]);
                                break;
                            case System.Data.DbType.Guid:
                                instance.Fields[field.Name] = new Guid(record[f] as string);
                                break;
                            case System.Data.DbType.Time:
                                instance.Fields[field.Name] = TimeSpan.Parse(record[f] as string);
                                break;
                            case System.Data.DbType.Date:
                            case System.Data.DbType.DateTime:
                                DateTime dt;
                                if (DateTime.TryParse(record[f] as string, out dt))
                                {
                                    instance.Fields[field.Name] = dt;
                                }
                                else
                                {
                                    instance.Fields[field.Name] = null;
                                }

                                break;
                            default:
                                instance.Fields[field.Name] = record[f];
                                break;
                        }
                    }
                    else
                    {
                        instance.Fields[field.Name] = record[f];
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("** Unable to convert field {0} value {1} to {2}: {3}", field.Name, record[f], field.TypeName, ex.Message));
                    throw ex;
                }
                f++;
            }

            return instance;
        }

        private object RehydrateObjectFromEntityValueDictionary(Type type, string entityName, object[] record)
        {
            try
            {
                if (string.IsNullOrEmpty(entityName))
                {
                    entityName = Entities.GetNameForType(type);
                }

                var instance = Activator.CreateInstance(type);
                var f = 0;
                foreach (var field in Entities[entityName].Fields)
                {
                    DbType? dbtype = null;
                    // DreamFactory has a bug where some numerics are returnd in JSON as strings, so we need to handle that here
                    if (record[f] is string)
                    {
                        dbtype = PropertyTypeToDbType(field.PropertyInfo.PropertyType);
                    }

                    if (dbtype == null) // this will catch non-string and string conversion failures
                    {
                        dbtype = field.PropertyInfo.PropertyType.ToDbType();
                    }

                    switch (dbtype.Value)
                    {
                        case System.Data.DbType.Int16:
                        case System.Data.DbType.UInt16:
                            field.PropertyInfo.SetValue(instance, Convert.ToInt16(record[f]), null);
                            break;
                        case System.Data.DbType.Int32:
                        case System.Data.DbType.UInt32:
                            field.PropertyInfo.SetValue(instance, Convert.ToInt32(record[f]), null);
                            break;
                        case System.Data.DbType.Int64:
                        case System.Data.DbType.UInt64:
                            field.PropertyInfo.SetValue(instance, Convert.ToInt64(record[f]), null);
                            break;
                        case System.Data.DbType.Double:
                            field.PropertyInfo.SetValue(instance, Convert.ToDouble(record[f]), null);
                            break;
                        case System.Data.DbType.Single:
                            field.PropertyInfo.SetValue(instance, Convert.ToSingle(record[f]), null);
                            break;
                        case System.Data.DbType.Decimal:
                            field.PropertyInfo.SetValue(instance, Convert.ToDecimal(record[f]), null);
                            break;
                        case System.Data.DbType.Guid:
                            field.PropertyInfo.SetValue(instance, new Guid(record[f] as string), null);
                            break;
                        case System.Data.DbType.Time:
                            field.PropertyInfo.SetValue(instance, TimeSpan.Parse(record[f] as string), null);
                            break;
                        case System.Data.DbType.Date:
                        case System.Data.DbType.DateTime:
                            field.PropertyInfo.SetValue(instance, DateTime.Parse(record[f] as string), null);
                            break;
                        default:
                            field.PropertyInfo.SetValue(instance, record[f], null);
                            break;
                    }

                    f++;
                }

                return instance;
            }
            catch (Exception ex)
            {
                if (Debugger.IsAttached) Debugger.Break();
                throw ex;
            }
        }

        public string[] GetTableNames()
        {
            try
            {
                var tables = m_session.Data.GetTables();

                if (tables == null || tables.Length == 0) return null;

                return tables.Select(t => t.Name).ToArray();

            }
            catch
            {
                return null;
            }
        }

        public override void RegisterDynamicEntity(DynamicEntityDefinition entityDefinition)
        {
            base.RegisterDynamicEntity(entityDefinition, true);
        }

        protected override void OnDynamicEntityRegistration(DynamicEntityDefinition definition, bool ensureCompatibility)
        {
            string name = definition.EntityName;

            if (!TableExists(name))
            {
                var fields = new List<Field>();

                foreach (var field in definition.Fields)
                {
                    if (ReservedWords.Contains(field.FieldName, StringComparer.InvariantCultureIgnoreCase))
                    {
                        throw new ReservedWordException(field.FieldName);
                    }

                    fields.Add(FieldFactory.GetFieldForAttribute(field, definition.EntityAttribute.KeyScheme));
                }

                m_session.Data.CreateTable(name, name, fields);
            }
            else
            {
                ValidateTable(definition);
            }
        }

        public override IEnumerable<DynamicEntity> Select(string entityName)
        {
            var table = m_session.Data.GetTable(entityName);

            if (table == null) return null;

            var records = table.GetRecords();

            var items = new List<DynamicEntity>();

            foreach (var record in records)
            {
                var instance = DynamicEntityFromEntityValueDictionary(entityName, table.Fields, record);
                items.Add(instance);
            }

            return items;
        }

        public override DynamicEntity Select(string entityName, object primaryKey)
        {
            var table = m_session.Data.GetTable(entityName);
            var record = table.GetRecords(primaryKey).FirstOrDefault();

            if (record == null) return null;

            return DynamicEntityFromEntityValueDictionary(entityName, table.Fields, record);
        }

        public override IEnumerable<DynamicEntity> Select(string entityName, IEnumerable<FilterCondition> filters)
        {
            var table = m_session.Data.GetTable(entityName);
            IEnumerable<object[]> records;

            if ((filters != null) && (filters.Count() > 0))
            {
                var filter = string.Join(" AND ", (from f in filters
                                                   select FilterToString(f)).ToArray());

                records = table.GetRecords(filter);
            }
            else
            {
                records = table.GetRecords();
            }

            var items = new List<DynamicEntity>();

            foreach (var record in records)
            {
                var instance = DynamicEntityFromEntityValueDictionary(entityName, table.Fields, record);
                items.Add(instance);
            }

            return items;
        }

        public override DynamicEntityDefinition DiscoverDynamicEntity(string entityName)
        {
            var table = m_session.Data.GetTable(entityName);
            if (table == null) return null;

            var fields = new List<FieldAttribute>();
            foreach (var field in table.Fields)
            {
                fields.Add(new FieldAttribute()
                {
                    FieldName = field.Name,
                    IsPrimaryKey = field.IsPrimaryKey.HasValue ? field.IsPrimaryKey.Value : false,
                    DataType = field.TypeName.ParseToDbType()
                });
            }

            var entityDefinition = new DynamicEntityDefinition(entityName, fields);
            RegisterEntityInfo(entityDefinition);
            return entityDefinition;
        }

        public override IEnumerable<T> Fetch<T>(int fetchCount, int firstRowOffset, string sortField, FieldSearchOrder sortOrder, FilterCondition filter, bool fillReferences)
        {
            if (fillReferences)
            {
                throw new NotSupportedException("References not currently supported in this DataStore implementation");
            }

            var entityType = typeof(T);
            var entityName = Entities.GetNameForType(entityType);
            var table = m_session.Data.GetTable(entityName);

            string orderString = null;
            string filterString = null;

            if (!string.IsNullOrEmpty(sortField))
            {
                switch(sortOrder)
                {
                    case FieldSearchOrder.Ascending:
                        orderString = string.Format("{0} ASC", sortField);
                        break;
                    case FieldSearchOrder.Descending:
                        orderString = string.Format("{0} DESC", sortField);
                        break;
                }                
            }

            if (filter != null)
            {
                filterString = FilterToString(filter);
            }

            return table.GetRecords(fetchCount, firstRowOffset, filterString, orderString).Cast<T>();
        }

        public override IEnumerable<T> Fetch<T>(int fetchCount, int firstRowOffset, string sortField)
        {
            return Fetch<T>(fetchCount, firstRowOffset, sortField, FieldSearchOrder.Ascending, null, false);
        }

        public override IEnumerable<T> Fetch<T>(int fetchCount, int firstRowOffset)
        {
            return Fetch<T>(fetchCount, firstRowOffset, null, FieldSearchOrder.NotSearchable, null, false);
        }

        public override IEnumerable<DynamicEntity> Fetch(string entityName, int fetchCount)
        {
            var table = m_session.Data.GetTable(entityName);
            
            if (table == null) return null;

            var records = table.GetRecords(fetchCount);

            var items = new List<DynamicEntity>();

            foreach (var record in records)
            {
                var instance = DynamicEntityFromEntityValueDictionary(entityName, table.Fields, record);
                items.Add(instance);
            }

            return items;
        }

        public override IEnumerable<DynamicEntity> Fetch(string entityName, int fetchCount, int firstRowOffset, string sortField, FieldSearchOrder sortOrder, FilterCondition filter, bool fillReferences)
        {
            if (fillReferences)
            {
                throw new NotSupportedException("References not currently supported in this DataStore implementation");
            }

            string filterString = null;
            string orderString = null;

            if (filter != null)
            {
                filterString = FilterToString(filter);
            }

            if (!string.IsNullOrEmpty(sortField))
            {
                switch (sortOrder)
                {
                    case FieldSearchOrder.Ascending:
                        orderString = string.Format("{0} ASC", sortField);
                        break;
                    case FieldSearchOrder.Descending:
                        orderString = string.Format("{0} DESC", sortField);
                        break;
                }
            }

            var table = m_session.Data.GetTable(entityName);

            if (table == null) return null;

            try
            {
                var records = table.GetRecords(fetchCount, firstRowOffset, filterString, orderString);

                if (records == null) return null;

                var items = new List<DynamicEntity>();

                foreach (var record in records)
                {
                    var instance = DynamicEntityFromEntityValueDictionary(entityName, table.Fields, record);
                    items.Add(instance);
                }

                return items;
            }
            catch
            {
                return null;
            }
        }

        public override IEnumerable<T> Fetch<T>(int fetchCount)
        {
            var entityType = typeof(T);
            var entityName = Entities.GetNameForType(entityType);
            var table = m_session.Data.GetTable(entityName);

            return table.GetRecords(fetchCount).Cast<T>();
        }

        public override int Count(string entityName)
        {
            var table = m_session.Data.GetTable(entityName);
            return table.GetRecordCount();
        }

        public override int Count<T>(IEnumerable<FilterCondition> filters)
        {
            var entityType = typeof(T);
            var entityName = Entities.GetNameForType(entityType);

            var table = m_session.Data.GetTable(entityName);

            if ((filters != null) && (filters.Count() > 0))
            {
                var filter = string.Join(" AND ", (from f in filters
                                                   select FilterToString(f)).ToArray());

                return table.GetRecordCount(filter);
            }
            else
            {
                return table.GetRecordCount();
            }            
        }

        public override void EnsureCompatibility()
        {
            if (!StoreExists)
            {
                CreateStore();
                return;
            }

            foreach(var e in Entities)
            {
                try
                {
                    ValidateTable(e);
                }
                catch (Exception ex)
                {
                    if (Debugger.IsAttached) Debugger.Break();
                    throw ex;
                }
            }
        }

        private void ValidateTable(IEntityInfo entity)
        {
            if (!TableExists(entity.EntityAttribute.NameInStore))
            {
                CreateTable(entity);
                return;
            }

            var fields = new List<Field>();

            foreach (var field in entity.Fields)
            {
                if (ReservedWords.Contains(field.FieldName, StringComparer.InvariantCultureIgnoreCase))
                {
                    throw new ReservedWordException(field.FieldName);
                }

                fields.Add(FieldFactory.GetFieldForAttribute(field, entity.EntityAttribute.KeyScheme));
            }

            try
            {
                m_session.Data.UpdateTable(entity.EntityName, fields);
            }
            catch (InvalidSessionException)
            {
                m_session.Reconnect();
                m_session.Data.UpdateTable(entity.EntityName, fields);
            }
        }

        public override void CreateStore()
        {
            if(StoreExists)
            {
                throw new StoreAlreadyExistsException();
            }

            m_session.Applications.Create(m_session.ApplicationName);
        }

        public override bool StoreExists
        {
            get 
            {
                var existingContainer = m_session.Applications.Find(m_session.ApplicationName);
                return existingContainer != null; 
            }
        }

        public override void DeleteStore()
        {
            m_session.Applications.Delete(m_session.ApplicationName);
        }

        public override void FillReferences(object instance)
        {
            throw new NotSupportedException("References not currently supported in this DataStore implementation");
        }
    }
}
