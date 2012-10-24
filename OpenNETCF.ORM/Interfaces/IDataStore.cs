using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Data;

namespace OpenNETCF.ORM
{
    public interface IDataStore
    {
        event EventHandler<EntityTypeAddedArgs> EntityTypeAdded;
        event EventHandler<EntityInsertArgs> BeforeInsert;
        event EventHandler<EntityInsertArgs> AfterInsert;
        event EventHandler<EntityUpdateArgs> BeforeUpdate;
        event EventHandler<EntityUpdateArgs> AfterUpdate;

        string Name { get; }
        EntityInfoCollection Entities { get; }

        void AddType<T>();
        void AddType<T>(bool ensureCompatibility);
        void AddType(Type entityType);
        void AddType(Type entityType, bool ensureCompatibility);

        void RegisterDynamicEntity(DynamicEntityDefinition entityDefinition);
        void RegisterDynamicEntity(DynamicEntityDefinition entityDefinition, bool ensureCompatibility);
        void DiscoverDynamicEntity(string entityName);

        void DiscoverTypes(Assembly containingAssembly);

        void CreateStore();
        void DeleteStore();
        bool StoreExists { get; }
        void EnsureCompatibility();

        void Insert(object item);
        void Insert(object item, bool insertReferences);

        IEntityInfo GetEntityInfo(string entityName);
        IEntityInfo[] GetEntityInfo();

        IEnumerable<T> Select<T>() where T : new();
        IEnumerable<T> Select<T>(bool fillReferences) where T : new();
        T Select<T>(object primaryKey) where T : new();
        T Select<T>(object primaryKey, bool fillReferences) where T : new();
        IEnumerable<T> Select<T>(string searchFieldName, object matchValue) where T : new();
        IEnumerable<T> Select<T>(string searchFieldName, object matchValue, bool fillReferences) where T : new();
        IEnumerable<T> Select<T>(IEnumerable<FilterCondition> filters) where T : new();
        IEnumerable<T> Select<T>(IEnumerable<FilterCondition> filters, bool fillReferences) where T : new();
        IEnumerable<object> Select(Type entityType);
        IEnumerable<object> Select(Type entityType, bool fillReferences);
        IEnumerable<T> Select<T>(Func<T, bool> selector) where T : new();
        IEnumerable<DynamicEntity> Select(string entityName);
        DynamicEntity Select(string entityName, object primaryKey);

        void Update(object item);
        void Update(object item, bool cascadeUpdates, string fieldName);
        void Update(object item, string fieldName);

        void Delete(object item);
        void Delete(string entityName, object primaryKey);
        void Delete(string entityName, string fieldName, object matchValue);
        void Delete<T>(object primaryKey) where T : new();
        void Delete<T>() where T : new();
        void Delete<T>(string fieldName, object matchValue) where T : new();

        IEnumerable<T> Fetch<T>(int fetchCount) where T : new();
        IEnumerable<T> Fetch<T>(int fetchCount, int firstRowOffset) where T : new();
        IEnumerable<T> Fetch<T>(int fetchCount, int firstRowOffset, string sortField) where T : new();
        IEnumerable<T> Fetch<T>(int fetchCount, int firstRowOffset, string sortField, FieldSearchOrder sortOrder, FilterCondition filter, bool fillReferences)
            where T : new();

        int Count<T>();
        int Count(string entityName);

        bool Contains(object item);

        void FillReferences(object instance);

        void BeginTransaction(IsolationLevel isolationLevel);
        void BeginTransaction();
        void Commit();
        void Rollback();
    }
}
