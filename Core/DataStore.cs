﻿using OpenNETCF.ORM.Replication;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace OpenNETCF.ORM
{
    public abstract class DataStore<TEntityInfo> : IDataStore
        where TEntityInfo : EntityInfo, new()
    {
        protected EntityInfoCollection m_entities = new EntityInfoCollection();
        private RecoveryService<TEntityInfo> m_recoveryService;

        public event EventHandler<EntityTypeAddedArgs> EntityTypeAdded;

        public ReplicatorCollection Replicators { get; private set; }

        // TODO: maybe move these to another object since they're more "admin" related?
        public abstract void CreateStore();
        public abstract void DeleteStore();
        public abstract bool StoreExists { get; }
        public abstract void EnsureCompatibility();

        public event EventHandler<EntityInsertArgs> BeforeInsert;
        public event EventHandler<EntityInsertArgs> AfterInsert;

        public abstract void OnInsert(object item, bool insertReferences);

        public abstract IEnumerable<T> Select<T>() where T : new();
        public abstract IEnumerable<T> Select<T>(bool fillReferences) where T : new();
        public abstract T Select<T>(object primaryKey) where T : new();
        public abstract T Select<T>(object primaryKey, bool fillReferences) where T : new();
        public abstract IEnumerable<T> Select<T>(string searchFieldName, object matchValue) where T : new();
        public abstract IEnumerable<T> Select<T>(string searchFieldName, object matchValue, bool fillReferences) where T : new();
        public abstract IEnumerable<T> Select<T>(IEnumerable<FilterCondition> filters) where T : new();
        public abstract IEnumerable<T> Select<T>(params FilterCondition[] filters) where T : new();
        public abstract IEnumerable<T> Select<T>(IEnumerable<FilterCondition> filters, bool fillReferences) where T : new();
        public abstract IEnumerable<object> Select(Type entityType);
        public abstract IEnumerable<object> Select(Type entityType, bool fillReferences);
        public abstract IEnumerable<DynamicEntity> Select(string entityName);
        public abstract IEnumerable<DynamicEntity> Select(string entityName, IEnumerable<FilterCondition> filters);
        public abstract DynamicEntity Select(string entityName, object primaryKey);
        public abstract void Drop(string entityName);

        public event EventHandler<EntityUpdateArgs> BeforeUpdate;
        public event EventHandler<EntityUpdateArgs> AfterUpdate;
        public abstract void OnUpdate(object item, bool cascadeUpdates, string fieldName);

        public event EventHandler<EntityDeleteArgs> BeforeDelete;
        public event EventHandler<EntityDeleteArgs> AfterDelete;
        public abstract void OnDelete(object item);

        /// <summary>
        /// Return <b>true</b> if you want the ORM to retry the operation.  Usefule for server unavailable-type errors
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public virtual bool IsRecoverableError(Exception ex)
        {
            return false;
        }

        public bool TracingEnabled { get; set; }

        protected abstract void OnDynamicEntityRegistration(DynamicEntityDefinition definition, bool ensureCompatibility);
        public abstract DynamicEntityDefinition DiscoverDynamicEntity(string entityName);

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <remarks>This method does <b>not</b> Fire the Before/AfterDelete events</remarks>
        public abstract void Delete<T>() where T : new();

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fieldName"></param>
        /// <param name="matchValue"></param>
        /// <remarks>This method does <b>not</b> Fire the Before/AfterDelete events</remarks>
        public abstract void Delete<T>(string fieldName, object matchValue) where T : new();

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fieldName"></param>
        /// <param name="matchValue"></param>
        /// <remarks>This method does <b>not</b> Fire the Before/AfterDelete events</remarks>
        public abstract int Delete<T>(IEnumerable<FilterCondition> filters);

        public abstract void Delete(string entityName, object primaryKey);
        public abstract void Delete(string entityName, string fieldName, object matchValue);
        public abstract int Delete(string entityName, IEnumerable<FilterCondition> filters);

        public abstract void FillReferences(object instance);
        public abstract IEnumerable<T> Fetch<T>(int fetchCount) where T : new();
        public abstract IEnumerable<T> Fetch<T>(int fetchCount, int firstRowOffset) where T : new();
        public abstract IEnumerable<T> Fetch<T>(int fetchCount, int firstRowOffset, string sortField) where T : new();
        public abstract IEnumerable<T> Fetch<T>(int fetchCount, int firstRowOffset, string sortField, FieldSearchOrder sortOrder, FilterCondition filter, bool fillReferences) where T : new();

        public abstract int Count(string entityName);
        public abstract int Count<T>(IEnumerable<FilterCondition> filters);
        public abstract bool Contains(object item);

        public DataStore()
        {
            TracingEnabled = false;
            Replicators = new ReplicatorCollection(this);
            m_recoveryService = new RecoveryService<TEntityInfo>(this);
            RecoveryEnabled = true;
            ResetRecoveryStats();
        }

        /// <summary>
        /// Returns the number of instances of the given type in the DataStore
        /// </summary>
        /// <typeparam name="T">Entity type to count</typeparam>
        /// <returns>The number of instances in the store</returns>
        public int Count<T>()
        {
            var t = typeof(T);
            string entityName = m_entities.GetNameForType(t);

            return Count(entityName);
        }

        public virtual string Name
        {
            get { return "Unanamed Data Store"; }
        }

        public void Delete(object item)
        {
            Validate
                .Begin()
                .IsNotNull(item)
                .Check();

            string name;

            if (item is DynamicEntity)
            {
                name = (item as DynamicEntity).EntityName;
            }
            else
            {
                name = Entities.GetNameForType(item.GetType());
            }

            OnBeforeDelete(name, item);
            OnDelete(item);
            OnAfterDelete(name, item);
        }

        /// <summary>
        /// Deletes the specified entity instance from the DataStore
        /// </summary>
        /// <param name="item"></param>
        /// <remarks>
        /// The instance provided must have a valid primary key value
        /// </remarks>
        public void Delete<T>(object primaryKey)
            where T : new()
        {
            var item = Select<T>(primaryKey);

            if (item != null)
            {
                Delete(item);
            }
        }

        public virtual void OnBeforeDelete(string entityName, object item)
        {
            var handler = BeforeDelete;
            if (handler != null)
            {
                handler(this, new EntityDeleteArgs(entityName, item));
            }
        }

        public virtual void OnAfterDelete(string entityName, object item)
        {
            var handler = AfterDelete;
            if (handler != null)
            {
                handler(this, new EntityDeleteArgs(entityName, item));
            }
        }

        /// <summary>
        /// Updates the backing DataStore with the values in the specified entity instance
        /// </summary>
        /// <param name="item"></param>
        /// <remarks>
        /// The instance provided must have a valid primary key value
        /// </remarks>
        public void Update(object item)
        {
            //TODO: is a cascading default of true a good idea?
            Update(item, true, null);
        }

        public void Update(object item, string fieldName)
        {
            Update(item, false, fieldName);
        }

        public void Update(object item, bool cascadeUpdates, string fieldName)
        {
            Validate
                .Begin()
                .IsNotNull(item)
                .Check();

            string name;

            if (item is DynamicEntity)
            {
                name = (item as DynamicEntity).EntityName;
            }
            else
            {
                name = Entities.GetNameForType(item.GetType());
            }

            OnBeforeUpdate(name, item, cascadeUpdates, fieldName);
            OnUpdate(item, cascadeUpdates, fieldName);
            OnAfterUpdate(name, item, cascadeUpdates, fieldName);
        }

        public virtual void OnBeforeUpdate(string entityName, object item, bool cascadeUpdates, string fieldName) 
        {
            var handler = BeforeUpdate;
            if (handler != null)
            {
                handler(this, new EntityUpdateArgs(entityName, item, cascadeUpdates, fieldName));
            }
        }

        public virtual void OnAfterUpdate(string entityName, object item, bool cascadeUpdates, string fieldName)
        {
            var handler = AfterUpdate;
            if (handler != null)
            {
                handler(this, new EntityUpdateArgs(entityName, item, cascadeUpdates, fieldName));
            }
        }


        public void Insert(object item, bool insertReferences)
        {
            Insert(item, insertReferences, false);
        }

        internal void Insert(object item, bool insertReferences, bool recoveryInsert)
        {
            string name;

            Validate
                .Begin()
                .IsNotNull(item)
                .Check();

            if (item is DynamicEntity)
            {
                name = (item as DynamicEntity).EntityName;
            }
            else
            {
                name = Entities.GetNameForType(item.GetType());
            }

            OnBeforeInsert(name, item, insertReferences);
            try
            {
                OnInsert(item, insertReferences);
            }
            catch (Exception ex)
            {
                if (recoveryInsert) throw;
                if (!IsRecoverableError(ex)) throw;

                m_recoveryService.QueueInsertForRecovery(item, insertReferences);
            }

            OnAfterInsert(name, item, insertReferences);
        }

        public virtual void OnBeforeInsert(string entityName, object item, bool insertReferences)
        {
            var handler = BeforeInsert;
            if (handler != null)
            {
                handler(this, new EntityInsertArgs(entityName, item, insertReferences));
            }
        }

        public virtual void OnAfterInsert(string entityName, object item, bool insertReferences)
        {
            var handler = AfterInsert;
            if (handler != null)
            {
                handler(this, new EntityInsertArgs(entityName, item, insertReferences));
            }
        }

        public EntityInfoCollection Entities 
        {
            get { return m_entities; }
        }

        public IEntityInfo GetEntityInfo(string entityName)
        {
            if (!Entities.Contains(entityName))
            {
                DiscoverDynamicEntity(entityName);
            }

            return Entities[entityName];
        }

        public IEntityInfo[] GetEntityInfo()
        {
            return Entities.ToArray();
        }

        public void AddType<T>()
        {
            AddType<T>(true);
        }

        public void AddType<T>(bool ensureCompatibility)
        {
            AddType(typeof(T), true, ensureCompatibility);
        }

        public void AddType(Type entityType)
        {
            AddType(entityType, true);
        }

        public void AddType(Type entityType, bool ensureCompatibility)
        {
            AddType(entityType, true, ensureCompatibility);
        }

        protected void RegisterEntityInfo(IEntityInfo info)
        {
            lock (m_entities)
            {
                m_entities.Add(info);
            }
        }

        public virtual void RegisterDynamicEntity(DynamicEntityDefinition entityDefinition)
        {
            foreach (var f in entityDefinition.Fields)
            {
                if (entityDefinition.EntityAttribute.KeyScheme == KeyScheme.Identity)
                {
                    throw new NotSupportedException("The current provider does not currently support Identity dynamic entities");
                }
            }

            RegisterDynamicEntity(entityDefinition, false);
        }

        public void RegisterDynamicEntity(DynamicEntityDefinition entityDefinition, bool ensureCompatibility)
        {
            if (entityDefinition.Fields.Count == 0)
            {
                throw new ArgumentException("EntityDefinition must contain at least one field");
            }

            // verify this is a unique entity name
            // check the type-defined entities
            lock (m_entities)
            {
                var existing = m_entities.FirstOrDefault(e => e.EntityName == entityDefinition.EntityName);

                if (existing != null)
                {
                    if (!ensureCompatibility)
                    {
                        throw new EntityAlreadyExistsException(entityDefinition.EntityName);
                    }
                    else
                    {
                        m_entities[entityDefinition.EntityName] = entityDefinition;
                    }
                }
                else
                {
                    m_entities.Add(entityDefinition);
                }
            }


            OnDynamicEntityRegistration(entityDefinition, ensureCompatibility);
        }

        private void ValidateProperties()
        {
        }

        private void AddType(Type entityType, bool verifyInterface, bool ensureCompatibility)
        {
            var attr = (from a in entityType.GetCustomAttributes(true)
                        where a.GetType().Equals(typeof(EntityAttribute))
                        select a).FirstOrDefault() as EntityAttribute;

            if (verifyInterface)
            {
                if (attr == null)
                {
                    throw new ArgumentException(
                        string.Format("Type '{0}' does not have an EntityAttribute", entityType.Name));
                }                
            }

            var allprops = entityType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

            // if it's defined with a key scheme, ensure a PK has been attributed
            if (attr.KeyScheme != KeyScheme.None)
            {
                var keyProp = allprops.FirstOrDefault(p => p.GetCustomAttributes(true)
                    .Where(a =>
                        (a.GetType().Equals(typeof(FieldAttribute)))
                        && (a as FieldAttribute).IsPrimaryKey
                        ).FirstOrDefault() as FieldAttribute != null);

                if (keyProp == null)
                {
                    throw new ArgumentException(
                        $"Type '{entityType}' is defined as KeyScheme {attr.KeyScheme.ToString()} but has no field marked as PrimaryKey");
                }
                if (attr.KeyScheme == KeyScheme.GUID)
                {
                    if (keyProp.PropertyType != typeof(Guid))
                    {
                        throw new ArgumentException(
                            $"Type '{entityType}' is defined as KeyScheme.{attr.KeyScheme.ToString()} but PrimaryKey field '{keyProp.Name}' is defined as {keyProp.PropertyType.Name}");
                    }
                }
                else if (attr.KeyScheme == KeyScheme.Identity)
                {
                    if (keyProp.PropertyType != typeof(int) && keyProp.PropertyType != typeof(long))
                    {
                        throw new ArgumentException(
                            $"Type '{entityType}' is defined as KeyScheme.{attr.KeyScheme.ToString()} but PrimaryKey field '{keyProp.Name}' is defined as {keyProp.PropertyType.Name}");
                    }
                }
            }

            var map = new TEntityInfo();

            // store the NameInStore if  not explicitly set
            if (attr.NameInStore == null)
            {
                attr.NameInStore = entityType.Name;
            }

            //TODO: validate NameInStore

            map.Initialize(attr, entityType);

            // see if we have any entity 
            // get all field definitions
            foreach (var prop in allprops)
            {
                var attribute = prop.GetCustomAttributes(true)
                    .Where(a => 
                        (a.GetType().Equals(typeof(FieldAttribute)))
                        ).FirstOrDefault() as FieldAttribute;

                if (attribute != null)
                {

                    attribute.PropertyInfo = prop;

                    // construct the true FieldAttribute by merging the propertyinfo and the fileldattribute overrides
                    if (attribute.FieldName == null)
                    {
                        attribute.FieldName = prop.Name;
                    }

                    if (!attribute.DataTypeIsValid)
                    {
                        // first call any custom type converter
                        var dt = PropertyTypeToDbType(prop.PropertyType);

                        if (dt.HasValue)
                        {
                            attribute.DataType = dt.Value;
                        }
                        else
                        {
                            // no custom override, use the default
                            attribute.DataType = prop.PropertyType.ToDbType();
                        }
                    }

                    if (!map.Fields.ContainsField(attribute.FieldName))
                    {
                        map.Fields.Add(attribute);
                    }

                    if (m_entities.Contains(map.EntityName))
                    {
                        if (m_entities[map.EntityName].Fields.ContainsField(attribute.FieldName))
                        { // make sure the PropertyInfo is set (dynamic discovery can leave this null)
                            if (m_entities[map.EntityName].Fields[attribute.FieldName].PropertyInfo == null)
                            {
                                m_entities[map.EntityName].Fields[attribute.FieldName].PropertyInfo = prop;
                            }
                        }
                        else
                        { // the entity doesn't contain this named field, but should.
                            if (ensureCompatibility)
                            {
                                m_entities[map.EntityName].Fields.Add(attribute);
                            }
                            else
                            {
                                throw new FieldNotFoundException(string.Format("Field '{0}' not found in destination Entity '{1}'. Consider calling with ensureCompatibility parameter set to 'true'.",
                                    attribute.FieldName, map.EntityName));
                            }
                        }
                    }
                }
                else
                {
                    var reference = prop.GetCustomAttributes(true).Where(a => a.GetType().Equals(typeof(ReferenceAttribute))).FirstOrDefault() as ReferenceAttribute;

                    if (reference != null)
                    {
                        reference.PropertyInfo = prop;
                        map.References.Add(reference);
                    }
                }
            }

//            if (m_entities.Contains(map.EntityName))
//            {
//                // this will ensure that the m_entities type to name map is properly filled out
//                m_entities.Add(map);
//                return;
//            }

            if (map.Fields.Count == 0)
            {
                throw new OpenNETCF.ORM.EntityDefinitionException(map.EntityName, string.Format("Entity '{0}' Contains no Field definitions.", map.EntityName));
            }

            // store a creator proxy delegate if the entity supports it (*way* faster for Selects)
            var methodInfo = entityType.GetMethod("ORM_CreateProxy", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            if (methodInfo != null)
            {
                map.CreateProxy = (EntityCreatorDelegate)Delegate.CreateDelegate(typeof(EntityCreatorDelegate), null, methodInfo);
            }

            m_entities.Add(map);

            AfterAddEntityType(entityType, ensureCompatibility);

            var handler = EntityTypeAdded;
            if (handler != null)
            {
                var info = this.Entities[attr.NameInStore];
                var args = new EntityTypeAddedArgs(info);
                handler(this, args);
            }
        }

        /// <summary>
        /// Gets the registered NameInStore for the given type or null it the type is unregsitered
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        public string GetNameInStore<TEntity>()
        {
            var ei = m_entities.FirstOrDefault(e => e.EntityType == typeof(TEntity));

            if (ei == null) return null;

            return ei.EntityName;
        }

        protected virtual DbType? PropertyTypeToDbType(Type propertyType)
        {
            return null;
        }

        protected virtual void AfterAddEntityType(Type entityType, bool ensureCompatibility)
        {
        }

        public void DiscoverTypes(Assembly containingAssembly)
        {
            var entities = from t in containingAssembly.GetTypes()
                           where t.GetCustomAttributes(true).Where(a => a.GetType().Equals(typeof(EntityAttribute))).FirstOrDefault() != null
                           select t;

            foreach (var entity in entities)
            {
                // the interface has already been verified by our LINQ
                AddType(entity, false);
            }
        }

        protected void AddFieldToEntity(EntityInfo entity, FieldAttribute field)
        {
            entity.Fields.Add(field);
        }

        public void Insert(object item)
        {
            // TODO: should this default to true or false?
            // right now it is false since we don't look for duplicate references
            Insert(item, false);
        }

        public IEnumerable<T> Select<T>(Func<T, bool> selector)
            where T : new()
        {
            return (from e in Select<T>(false)
                   where selector(e)
                   select e);
        }

        private Dictionary<Type, ConstructorInfo> m_ctorCache = new Dictionary<Type, ConstructorInfo>();

        protected internal ConstructorInfo GetConstructorForType(Type objectType)
        {
            if (m_ctorCache.ContainsKey(objectType))
            {
                return m_ctorCache[objectType];
            }

            var ctor = objectType.GetConstructor(new Type[] { });
            m_ctorCache.Add(objectType, ctor);
            return ctor;
        }

        protected object CreateEntityInstance(string entityName, Type objectType, FieldAttributeCollection fields, IDataReader results, out bool fieldsSet)
        {
            if (objectType.Equals(typeof(DynamicEntity)))
            {
                var entity = new DynamicEntity(entityName, fields);

                foreach (var field in entity.Fields)
                {
                    // we should probably cache these ordinals
                    field.Value = results[field.Name];
                }

                fieldsSet = true;
                return entity;
            }

            var info = GetEntityInfo(entityName);
            if (info.CreateProxy == null)
            {
                // no create proxy exists, create the item and let the caller know it needs to fill the fields
                fieldsSet = false;
                return Activator.CreateInstance(objectType);
            }

            var item = info.CreateProxy(fields, results);
            fieldsSet = true;
            return item;
        }

        protected void SetInstanceValue(FieldAttribute field, object instance, object value)
        {
            if (instance is DynamicEntity)
            {
                ((DynamicEntity)instance).Fields[field.FieldName] = value ?? DBNull.Value;
            }
            else
            {
                // use Convert where we can to help ensure conversions (uint->int and the like)
                if (field.PropertyInfo.PropertyType.Equals(typeof(int)))
                {
                    field.PropertyInfo.SetValue(instance, Convert.ToInt32(value), null);
                }
                else if (field.PropertyInfo.PropertyType.Equals(typeof(uint)))
                {
                    field.PropertyInfo.SetValue(instance, Convert.ToUInt32(value), null);
                }
                else if (field.PropertyInfo.PropertyType.Equals(typeof(short)))
                {
                    field.PropertyInfo.SetValue(instance, Convert.ToInt16(value), null);
                }
                else if (field.PropertyInfo.PropertyType.Equals(typeof(ushort)))
                {
                    field.PropertyInfo.SetValue(instance, Convert.ToUInt16(value), null);
                }
                else if (field.PropertyInfo.PropertyType.Equals(typeof(long)))
                {
                    field.PropertyInfo.SetValue(instance, Convert.ToInt64(value), null);
                }
                else if (field.PropertyInfo.PropertyType.Equals(typeof(ulong)))
                {
                    field.PropertyInfo.SetValue(instance, Convert.ToUInt64(value), null);
                }
                else if (field.PropertyInfo.PropertyType.Equals(typeof(float)))
                {
                    field.PropertyInfo.SetValue(instance, Convert.ToSingle(value), null);
                }
                else if (field.PropertyInfo.PropertyType.Equals(typeof(double)))
                {
                    field.PropertyInfo.SetValue(instance, Convert.ToDouble(value), null);
                }
                else
                {
                    field.PropertyInfo.SetValue(instance, value, null);
                }
            }
        }

        protected object GetInstanceValue(FieldAttribute field, object instance)
        {
            object value;
            if (instance is DynamicEntity)
            {
                value = ((DynamicEntity)instance).Fields[field.FieldName];
            }
            else
            {
                value = field.PropertyInfo.GetValue(instance, null);
            }

            if (value is TimeSpan)
            {
                return ((TimeSpan)value).Ticks;
            }

            if (value == null) return DBNull.Value;

            return value;
        }

        public virtual void BeginTransaction()
        {            
            BeginTransaction(IsolationLevel.Unspecified);
        }

        public virtual void BeginTransaction(IsolationLevel isolationLevel)
        {
            throw new NotSupportedException("Transactions are not supported by this provider");
        }

        public virtual void Commit()
        {
            throw new NotSupportedException("Transactions are not supported by this provider");
        }

        public virtual void Rollback()
        {
            throw new NotSupportedException("Transactions are not supported by this provider");
        }

        public virtual IEnumerable<DynamicEntity> Fetch(string entityName, int fetchCount)
        {
            return Select(entityName).Take(fetchCount);
        }

        public virtual IEnumerable<DynamicEntity> Fetch(string entityName, int fetchCount, int firstRowOffset, string sortField, FieldSearchOrder sortOrder, FilterCondition filter, bool fillReferences)
        {
            throw new NotSupportedException("This version of Fetch not supported by this provider");

            // I could build a complex LINQ statement here, but I think it would end up being crazy inefficient and should be overridden and implemented with sanity instead

            // this is *way* inefficient and should almost always be overridden
            //var items = from Select(entityName)
            //            where filter.FieldName

        }

        public bool RecoveryEnabled
        {
            get { return m_recoveryService.Enabled; }
            set { m_recoveryService.Enabled = value; }
        }

        public RecoverableInfo GetRecoveryStats()
        {
            return m_recoveryService.GetStats();
        }

        public void ResetRecoveryStats()
        {
            m_recoveryService.ResetStats();
        }
    }
}
