using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenNETCF.DreamFactory;
using System.Reflection;

namespace OpenNETCF.ORM
{
    public class DreamFactoryDataStore : DataStore<EntityInfo>, ITableBasedStore
    {
        private Session m_session;

        public DreamFactoryDataStore(string dspRootAddress, string application, string username, string password)
        {
            m_session = new Session(dspRootAddress, application, username, password);

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

            if (!TableExists(name))
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

        private void ValidateTable(IEntityInfo entity)
        {
            // TODO:
        }

        public override void CreateStore()
        {
            // todo: create application? For now, assume it's there
        }


        public void DropTable(string tableName)
        {
            m_session.Data.DeleteTable(tableName);
        }

        public bool TableExists(string tableName)
        {
            return m_session.Data.GetTables().FirstOrDefault(t => string.Compare(t.Name, tableName, true) == 0) != null;
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

        public override void OnDelete(object item)
        {
            var entityType = item.GetType();
            var entityName = Entities.GetNameForType(entityType);
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
            var table = m_session.Data.GetTable(entityName);

            var records = table.GetRecords();

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

        public override void OnInsert(object item, bool insertReferences)
        {
            if (insertReferences)
            {
                throw new NotSupportedException("References not currently supported in this DataStore implementation");
            }

            var entityName = Entities.GetNameForType(item.GetType());

            var table = m_session.Data.GetTable(entityName);

            FieldAttribute idField;

            var values = GetEntityValueDictionary(item, out idField);

            var key = table.InsertRecord(values);

            // push the inserted key back into the item
            if(idField != null)
            {
                SetInstanceValue(idField, item, key);
            }
        }

        public override void OnUpdate(object item, bool cascadeUpdates, string fieldName)
        {
            var entityName = Entities.GetNameForType(item.GetType());

            var table = m_session.Data.GetTable(entityName);

            FieldAttribute idField;
            var values = GetEntityValueDictionary(item, out idField);

            table.UpdateRecord(values);
        }

        public override bool Contains(object item)
        {
            var entityType = item.GetType();
            var entityName = Entities.GetNameForType(entityType);
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
            var entityName = Entities.GetNameForType(item.GetType());

            var values = new Dictionary<string, object>();

            identityField = null;

            foreach (var field in Entities[entityName].Fields)
            {
                if (field.IsPrimaryKey) identityField = field;

                var value = field.PropertyInfo.GetValue(item, null);

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

            return values;
        }

        private T RehydrateObjectFromEntityValueDictionary<T>(string entityName, object[] record)
            where T : class
        {
            return RehydrateObjectFromEntityValueDictionary(typeof(T), entityName, record) as T;
        }

        private object RehydrateObjectFromEntityValueDictionary(Type type, string entityName, object[] record)
        {
            if (string.IsNullOrEmpty(entityName))
            {
                entityName = Entities.GetNameForType(type);
            }

            var instance = Activator.CreateInstance(type);
            var f = 0;
            foreach (var field in Entities[entityName].Fields)
            {
                // DreamFactory has a bug where some numerics are returnd in JSON as strings, so we 
                if(record[f] is string)
                {
                    var dbtype = PropertyTypeToDbType(field.PropertyInfo.PropertyType);

                    if (dbtype == null)
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
                        case  System.Data.DbType.Single:
                            field.PropertyInfo.SetValue(instance, Convert.ToSingle(record[f]), null);
                            break;
                        case  System.Data.DbType.Decimal:
                            field.PropertyInfo.SetValue(instance, Convert.ToDecimal(record[f]), null);
                            break;
                        case  System.Data.DbType.Guid:
                            field.PropertyInfo.SetValue(instance, new Guid(record[f] as string), null);
                            break;
                        case System.Data.DbType.Time:
                            field.PropertyInfo.SetValue(instance,TimeSpan.Parse(record[f] as string), null);
                            break;
                        case System.Data.DbType.Date:
                        case System.Data.DbType.DateTime:
                            field.PropertyInfo.SetValue(instance, DateTime.Parse(record[f] as string), null);
                            break;
                        default:
                            field.PropertyInfo.SetValue(instance, record[f], null);
                            break;
                    }
                }
                f++;
            }

            return instance;
        }

        public override int Count(string entityName)
        {
            var table = m_session.Data.GetTable(entityName);
            return table.GetRecordCount();
        }




        public override IEnumerable<DynamicEntity> Select(string entityName)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<object> Select(Type entityType)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<T> Select<T>(IEnumerable<FilterCondition> filters, bool fillReferences)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<T> Select<T>(IEnumerable<FilterCondition> filters)
        {
            throw new NotImplementedException();
        }







        protected override void OnDynamicEntityRegistration(DynamicEntityDefinition definition, bool ensureCompatibility)
        {
            throw new NotImplementedException();
        }

        public override int Count<T>(IEnumerable<FilterCondition> filters)
        {
            throw new NotImplementedException();
        }

        public override void Delete(string entityName, string fieldName, object matchValue)
        {
            throw new NotImplementedException();
        }

        public override void Delete(string entityName, object primaryKey)
        {
            throw new NotImplementedException();
        }

        public override void Delete<T>(string fieldName, object matchValue)
        {
            throw new NotImplementedException();
        }

        public override void DeleteStore()
        {
            throw new NotImplementedException();
        }

        public override void DiscoverDynamicEntity(string entityName)
        {
            throw new NotImplementedException();
        }

        public override void EnsureCompatibility()
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<T> Fetch<T>(int fetchCount, int firstRowOffset, string sortField, FieldSearchOrder sortOrder, FilterCondition filter, bool fillReferences)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<T> Fetch<T>(int fetchCount, int firstRowOffset, string sortField)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<T> Fetch<T>(int fetchCount, int firstRowOffset)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<T> Fetch<T>(int fetchCount)
        {
            throw new NotImplementedException();
        }

        public override void FillReferences(object instance)
        {
            throw new NotImplementedException();
        }

        public override DynamicEntity Select(string entityName, object primaryKey)
        {
            throw new NotImplementedException();
        }


        public override bool StoreExists
        {
            get { throw new NotImplementedException(); }
        }


        #region ITableBasedStore Members

        public string[] GetTableNames()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
