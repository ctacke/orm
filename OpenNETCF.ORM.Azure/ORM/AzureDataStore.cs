using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Globalization;
using System.Net;
using System.IO;
using System.Xml.Linq;
using OpenNETCF.Azure;
using System.Diagnostics;

namespace OpenNETCF.ORM
{
    public class AzureDataStore : DataStore<EntityInfo>, ITableBasedStore
    {
        private Dictionary<string, DynamicEntityDefinition> m_dynamicDefinitions = new Dictionary<string, DynamicEntityDefinition>(StringComparer.InvariantCultureIgnoreCase);

        private TableService TableService { get; set; }
        private string PartitionKey { get; set; }

        public AzureDataStore(TableService service, string partitionKey)
        {
            Validate
                .Begin()
                .IsNotNull(service)
                .IsNotNullOrEmpty(partitionKey)
                .Check();

            PartitionKey = partitionKey;
            TableService = service;
        }

        public override string Name
        {
            get { return PartitionKey; }
        }

        public DateTime EntityLastUpdated(string entityName)
        {
            TableService.Tables[entityName].RefreshStatistics();
            return TableService.Tables[entityName].LastUpdated;
        }

        public override void CreateStore()
        {
            // nothing to do for Azure
        }

        public bool TableExists(string tableName)
        {
            return TableService.Tables.DoesTableExist(tableName);
        }

        public void TruncateTable(string tableName)
        {
            // azure can't truncate, so we'll just delete and re-create
            TableService.Tables.DeleteTableIfExist(tableName);
            TableService.Tables.CreateTable(tableName);
        }

        public void DropTable(string tableName)
        {
            TableService.Tables.DeleteTable(tableName);
        }

        public override bool StoreExists
        {
            // TODO: return "is connected"?
            get { return true; }
        }

        protected override void AfterAddEntityType(Type entityType, bool ensureCompatibility)
        {
            var name = m_entities.GetNameForType(entityType);

            if ((m_entities[name].References != null) && (m_entities[name].References.Count > 0))
            {
                throw new NotSupportedException("References not currently supported for this Provider");
            }

            // make sure the table exists
            TableService.Tables.CreateTableIfNotExist(name);
        }

        public override void OnInsert(object item, bool insertReferences)
        {
            var entityName = GetEntityNameForInstance(item);

            // TODO: this might cause collisions
            var entity = new AzureEntity(PartitionKey, DateTime.UtcNow.Ticks.ToString());

            foreach (var field in Entities[entityName].Fields)
            {
                var value = GetInstanceValue(field, item);

                if (field.IsPrimaryKey)
                {
                    entity.RowKey = value.ToString();
                }
                else
                {
                    // TODO: do we need to special-case types not handled by Azure (TimeSpans, etc)

                    // TODO: do we need to handle null values as a special case?
                    entity.Fields.Add(field.FieldName, value);
                }
            }

            TableService.Tables[entityName].Insert(entity);
        }

        public override IEnumerable<DynamicEntity> Select(string entityName)
        {
            var entities = TableService.Tables[entityName].GetPartitionEntities(PartitionKey);

            if (!m_dynamicDefinitions.ContainsKey(entityName))
            {
                //if (Debugger.IsAttached) Debugger.Break();
                yield break;
            }

            foreach (var e in entities)
            {
                yield return e.AsDynamicEntity(m_dynamicDefinitions[entityName]);
            }
        }

        public override T Select<T>(object primaryKey)
        {
            if (!(primaryKey is string))
            {
                throw new Exception("Azure Table Services require a string primary key (Row ID)");
            }

            var objectType = typeof(T);
            string entityName = Entities.GetNameForType(objectType);

            var entity = TableService.Tables[entityName].GetEntity(PartitionKey, primaryKey.ToString());

            if (entity == null) return default(T);  // row with provided PK not found

            var item = new T();

            foreach (var ef in entity.Fields)
            {
                // if a field exists remotely, but not locally, just skip it
                // TODO: should we Trace it?
                if (!Entities[entityName].Fields.ContainsField(ef.Name)) continue;

                var field = Entities[entityName].Fields[ef.Name];

                // special-cases for types not handled by Azure
                if (field.PropertyInfo.PropertyType.Equals(typeof(TimeSpan)))
                {
                    SetInstanceValue(Entities[entityName].Fields[ef.Name], item, new TimeSpan(Convert.ToInt64(ef.Value)));
                }
                else
                {
                    SetInstanceValue(Entities[entityName].Fields[ef.Name], item, ef.Value);
                }
            }

            // set key field
            var keyName = m_keyFieldDictionary[objectType];
            if (keyName != null)
            {
                SetInstanceValue(Entities[entityName].Fields[keyName], item, entity.RowKey);
            }

            return item;
        }

        public override DynamicEntity Select(string entityName, object primaryKey)
        {
            if (!(primaryKey is string))
            {
                throw new Exception("Azure Table Services require a string primary key (Row ID)");
            }

            var e = TableService.Tables[entityName].GetEntity(PartitionKey, primaryKey.ToString());

            return e.AsDynamicEntity(m_dynamicDefinitions[entityName]);
        }

        protected override void OnDynamicEntityRegistration(DynamicEntityDefinition definition, bool ensureCompatibility)
        {
            // create the table if necessary
            TableService.Tables.CreateTableIfNotExist(definition.EntityName);

            // store the definiton.  later we'll use it for detecting nulls, ignoring columns, etc
            if (!m_dynamicDefinitions.ContainsKey(definition.EntityName))
            {
                m_dynamicDefinitions.Add(definition.EntityName, definition);
            }
            else if (ensureCompatibility)
            {
                m_dynamicDefinitions[definition.EntityName] = definition;
            }
            else
            {
                throw new Exception("Dynamic type already registered");
            }
        }

        public override IEnumerable<T> Select<T>(bool fillReferences)
        {
            if (fillReferences)
            {
                throw new NotSupportedException("References not currently supported for this Provider");
            }

            return Select<T>();
        }

        public override T Select<T>(object primaryKey, bool fillReferences)
        {
            if (fillReferences)
            {
                throw new NotSupportedException("References not currently supported for this Provider");
            }

            return Select<T>(primaryKey);
        }

        private Dictionary<Type, string> m_keyFieldDictionary = new Dictionary<Type, string>();

        public override IEnumerable<DynamicEntity> Fetch(string entityName, int fetchCount)
        {
            foreach (var e in TableService.Tables[entityName].GetPartitionEntities(PartitionKey, fetchCount))
            {
                var de = new DynamicEntity();
                de.EntityName = entityName;
                de.Fields.Add("PartitionKey", e.PartitionKey);
                de.Fields.Add("RowKey", e.RowKey);

                foreach (var f in e.Fields)
                {
                    de.Fields.Add(f.Name, f.Value);
                }

                yield return de;
            }
        }

        public override IEnumerable<T> Select<T>()
        {
            var objectType = typeof(T);
            string entityName = Entities.GetNameForType(objectType);

            // maintain a dictionary of RowID (PK) fields, if the entity has one
            if (!(objectType.Equals(typeof(DynamicEntity))) && (!m_keyFieldDictionary.ContainsKey(objectType)))
            {
                var keyField = Entities[entityName].Fields.FirstOrDefault(f => f.IsPrimaryKey == true);
                if (keyField != null)
                {
                    m_keyFieldDictionary.Add(objectType, keyField.FieldName);
                }
                else
                {
                    m_keyFieldDictionary.Add(objectType, null);
                }
            }

            foreach (var e in TableService.Tables[entityName].GetPartitionEntities(PartitionKey))
            {
                var item = new T();

                if (objectType.Equals(typeof(DynamicEntity)))
                {
                    var de = item as DynamicEntity;
                    de.EntityName = entityName;
                    de.Fields.Add("PartitionKey", e.PartitionKey);
                    de.Fields.Add("RowKey", e.RowKey);

                    foreach (var f in e.Fields)
                    {
                        de.Fields.Add(f.Name, f.Value);
                    }
                }
                else
                {
                    foreach (var ef in e.Fields)
                    {
                        // if a field exists remotely, but not locally, just skip it
                        // TODO: should we Trace it?
                        if (!Entities[entityName].Fields.ContainsField(ef.Name)) continue;
                             
                        var field = Entities[entityName].Fields[ef.Name];

                        // special-cases for types not handled by Azure
                        if (field.PropertyInfo.PropertyType.Equals(typeof(TimeSpan)))
                        {
                            SetInstanceValue(Entities[entityName].Fields[ef.Name], item, new TimeSpan(Convert.ToInt64(ef.Value)));
                        }
                        else
                        {
                            SetInstanceValue(Entities[entityName].Fields[ef.Name], item, ef.Value);
                        }
                    }

                    // fill the PK from the RowID
                    var keyName = m_keyFieldDictionary[objectType]; 
                    if (keyName != null)
                    {
                        SetInstanceValue(Entities[entityName].Fields[keyName], item, e.RowKey);
                    }
                }
                yield return item;
            }
        }

        public override void EnsureCompatibility()
        {
            // nothing to do in Azure
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

        public override void OnDelete(object item)
        {
            var entityName = GetEntityNameForInstance(item);

            var keyField = Entities[entityName].Fields.FirstOrDefault(f=>f.IsPrimaryKey);
            var keyValue = GetInstanceValue(keyField, item);

            TableService.Tables[entityName].Delete(this.PartitionKey, keyValue.ToString());
        }

        public override void OnUpdate(object item, bool cascadeUpdates, string fieldName)
        {
            var entityName = GetEntityNameForInstance(item);

            // TODO: this might cause collisions
            var entity = new AzureEntity(PartitionKey, DateTime.UtcNow.Ticks.ToString());

            foreach (var field in Entities[entityName].Fields)
            {
                var value = GetInstanceValue(field, item);

                if (field.IsPrimaryKey)
                {
                    entity.RowKey = value.ToString();
                }
                else
                {
                    // TODO: do we need to special-case types not handled by Azure (TimeSpans, etc)

                    // TODO: do we need to handle null values as a special case?
                    entity.Fields.Add(field.FieldName, value);
                }
            }

            // TODO: should we use Update instaed?
            TableService.Tables[entityName].InsertOrReplace(entity);
        }




        public override bool Contains(object item)
        {
            throw new NotImplementedException();
        }

        public override int Count<T>(IEnumerable<FilterCondition> filters)
        {
            throw new NotImplementedException();
        }

        public override int Count(string entityName)
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

        public override void Delete<T>()
        {
            throw new NotImplementedException();
        }

        public override void DeleteStore()
        {
            throw new NotImplementedException();
        }

        public override DynamicEntityDefinition DiscoverDynamicEntity(string entityName)
        {
            // TODO:
            return null;
        }

        public override IEnumerable<T> Fetch<T>(int fetchCount, int firstRowOffset, string sortField, FieldSearchOrder sortOrder, FilterCondition filter, bool fillReferences)
        {
            if (fillReferences)
            {
                throw new NotSupportedException("References not currently supported for this Provider");
            }

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

        public override IEnumerable<object> Select(Type entityType, bool fillReferences)
        {
            if (fillReferences)
            {
                throw new NotSupportedException("References not currently supported for this Provider");
            }

            throw new NotImplementedException();
        }

        public override IEnumerable<object> Select(Type entityType)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<T> Select<T>(IEnumerable<FilterCondition> filters, bool fillReferences)
        {
            if (fillReferences)
            {
                throw new NotSupportedException("References not currently supported for this Provider");
            }
            
            throw new NotImplementedException();
        }

        public override IEnumerable<T> Select<T>(IEnumerable<FilterCondition> filters)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<T> Select<T>(string searchFieldName, object matchValue, bool fillReferences)
        {
            if (fillReferences)
            {
                throw new NotSupportedException("References not currently supported for this Provider");
            }

            throw new NotImplementedException();
        }

        public override IEnumerable<T> Select<T>(string searchFieldName, object matchValue)
        {
            throw new NotImplementedException();
        }

        string[] ITableBasedStore.GetTableNames()
        {
            throw new NotImplementedException();
        }
    }
}
