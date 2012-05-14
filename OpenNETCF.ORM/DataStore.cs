using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Diagnostics;

namespace OpenNETCF.ORM
{
    public abstract class DataStore<TEntityInfo> : IDataStore
        where TEntityInfo : EntityInfo, new()
    {
        protected EntityInfoCollection<TEntityInfo> m_entities = new EntityInfoCollection<TEntityInfo>();

        public event EventHandler<EntityTypeAddedArgs> EntityTypeAdded;

        // TODO: maybe move these to another object since they're more "admin" related?
        public abstract void CreateStore();
        public abstract void DeleteStore();
        public abstract bool StoreExists { get; }
        public abstract void EnsureCompatibility();

        public event EventHandler<EntityInsertArgs> BeforeInsert;
        public event EventHandler<EntityInsertArgs> AfterInsert;
        public abstract void OnInsert(object item, bool insertReferences);

        public abstract T[] Select<T>() where T : new();
        public abstract T[] Select<T>(bool fillReferences) where T : new();
        public abstract T Select<T>(object primaryKey) where T : new();
        public abstract T Select<T>(object primaryKey, bool fillReferences) where T : new();
        public abstract T[] Select<T>(string searchFieldName, object matchValue) where T : new();
        public abstract T[] Select<T>(string searchFieldName, object matchValue, bool fillReferences) where T : new();
        public abstract T[] Select<T>(IEnumerable<FilterCondition> filters) where T : new();
        public abstract T[] Select<T>(IEnumerable<FilterCondition> filters, bool fillReferences) where T : new();
        public abstract object[] Select(Type entityType);
        public abstract object[] Select(Type entityType, bool fillReferences);

        public event EventHandler<EntityUpdateArgs> BeforeUpdate;
        public event EventHandler<EntityUpdateArgs> AfterUpdate;
        public abstract void OnUpdate(object item, bool cascadeUpdates, string fieldName);

        public event EventHandler<EntityDeleteArgs> BeforeDelete;
        public event EventHandler<EntityDeleteArgs> AfterDelete;
        public abstract void OnDelete(object item);

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
        
        public abstract void FillReferences(object instance);
        public abstract T[] Fetch<T>(int fetchCount) where T : new();
        public abstract T[] Fetch<T>(int fetchCount, int firstRowOffset) where T : new();
        public abstract T[] Fetch<T>(int fetchCount, int firstRowOffset, string sortField) where T : new();
        public abstract T[] Fetch<T>(int fetchCount, int firstRowOffset, string sortField, FieldSearchOrder sortOrder, FilterCondition filter, bool fillReferences) where T : new();

        public abstract int Count<T>();
        public abstract int Count<T>(IEnumerable<FilterCondition> filters);
        public abstract bool Contains(object item);

        public DataStore()
        {
        }

        public void Delete(object item)
        {
            OnBeforeDelete(item);
            OnDelete(item);
            OnAfterDelete(item);
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

        public virtual void OnBeforeDelete(object item)
        {
            var handler = BeforeDelete;
            if (handler != null)
            {
                handler(this, new EntityDeleteArgs(item));
            }
        }

        public virtual void OnAfterDelete(object item)
        {
            var handler = AfterDelete;
            if (handler != null)
            {
                handler(this, new EntityDeleteArgs(item));
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
            OnBeforeUpdate(item, cascadeUpdates, fieldName);
            OnUpdate(item, cascadeUpdates, fieldName);
            OnAfterUpdate(item, cascadeUpdates, fieldName);
        }

        public virtual void OnBeforeUpdate(object item, bool cascadeUpdates, string fieldName) 
        {
            var handler = BeforeUpdate;
            if (handler != null)
            {
                handler(this, new EntityUpdateArgs(item, cascadeUpdates, fieldName));
            }
        }

        public virtual void OnAfterUpdate(object item, bool cascadeUpdates, string fieldName)
        {
            var handler = AfterUpdate;
            if (handler != null)
            {
                handler(this, new EntityUpdateArgs(item, cascadeUpdates, fieldName));
            }
        }

        public void Insert(object item, bool insertReferences)
        {
            OnBeforeInsert(item, insertReferences);
            OnInsert(item, insertReferences);
            OnAfterInsert(item, insertReferences);
        }

        public virtual void OnBeforeInsert(object item, bool insertReferences)
        {
            var handler = BeforeInsert;
            if (handler != null)
            {
                handler(this, new EntityInsertArgs(item, insertReferences));
            }
        }

        public virtual void OnAfterInsert(object item, bool insertReferences)
        {
            var handler = AfterInsert;
            if (handler != null)
            {
                handler(this, new EntityInsertArgs(item, insertReferences));
            }
        }

        public EntityInfoCollection<TEntityInfo> Entities 
        {
            get { return m_entities; }
        }

        public IEntityInfo GetEntityInfo(string entityName)
        {
            return Entities[entityName];
        }

        public IEntityInfo[] GetEntityInfo()
        {
            return Entities.ToArray();
        }

        public void AddType<T>()
        {
            AddType(typeof(T), true);
        }

        public void AddType(Type entityType)
        {
            AddType(entityType, true);
        }

        private void AddType(Type entityType, bool verifyInterface)
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
            foreach (var prop in entityType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
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
                        // TODO: add custom converter support here
                        attribute.DataType = prop.PropertyType.ToDbType();
                    }

                    map.Fields.Add(attribute);
                }
                else
                {
                    var reference = prop.GetCustomAttributes(true).Where(a => a.GetType().Equals(typeof(ReferenceAttribute))).FirstOrDefault() as ReferenceAttribute;

                    if (reference != null)
                    {
                        //if (!prop.PropertyType.IsArray)
                        //{
                        //    throw new InvalidReferenceTypeException(reference.ReferenceEntityType, reference.ReferenceField,
                        //        "Reference fields must be arrays");
                        //}

                        reference.PropertyInfo = prop;
                        map.References.Add(reference);
                    }
                }
            }

            if (map.Fields.Count == 0)
            {
                throw new OpenNETCF.ORM.EntityDefinitionException(map.EntityName, string.Format("Entity '{0}' Contains no Field definitions.", map.EntityName));
            }

            m_entities.Add(map);

            var handler = EntityTypeAdded;
            if (handler != null)
            {
                var info = this.Entities[attr.NameInStore];
                var args = new EntityTypeAddedArgs(info);
                handler(this, args);
            }
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

        public T[] Select<T>(Func<T, bool> selector)
            where T : new()
        {
            return (from e in Select<T>(false)
                   where selector(e)
                   select e).ToArray();
        }
    }
}
