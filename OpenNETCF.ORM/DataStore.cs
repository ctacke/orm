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

        public abstract void CreateStore();
        public abstract void DeleteStore();
        public abstract bool StoreExists { get; }
        public abstract void Insert(object item, bool insertReferences);
        public abstract T[] Select<T>() where T : new();
        public abstract T Select<T>(object primaryKey) where T : new();
        public abstract T[] Select<T>(string searchFieldName, object matchValue) where T : new();
        public abstract void Update(object item);
        public abstract void Update(object item, bool cascadeUpdates);
        public abstract void Delete(object item);
        public abstract void Delete<T>(object primaryKey);
        public abstract void FillReferences(object instance);
        public abstract T[] Fetch<T>(int fetchCount) where T : new();
        public abstract T[] Fetch<T>(int fetchCount, int firstRowOffset) where T : new();
        public abstract T[] Fetch<T>(string searchFieldName, int fetchCount, int firstRowOffset) where T : new();
        public abstract int Count<T>();
        public abstract void Delete<T>();
        public abstract void Delete<T>(string fieldName, object matchValue);
        public abstract bool Contains(object item);

        public DataStore()
        {
        }

        public EntityInfoCollection<TEntityInfo> Entities 
        {
            get { return m_entities; }
        }

        public EntityInfo GetEntityInfo(string entityName)
        {
            return Entities[entityName];
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
                        if (!prop.PropertyType.IsArray)
                        {
                            throw new InvalidReferenceTypeException(reference.ReferenceEntityType, reference.ReferenceField,
                                "Reference fields must be arrays");
                        }

                        reference.PropertyInfo = prop;
                        map.References.Add(reference);
                    }
                }
            }

            m_entities.Add(map);
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

        public void Insert(object item)
        {
            // TODO: should this default to true or false?
            // right now it is false since we don't look for duplicate references
            Insert(item, false);
        }
    }
}
