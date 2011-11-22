using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace OpenNETCF.ORM
{
    public interface IDataStore
    {
        void AddType<T>();
        void AddType(Type entityType);

        void DiscoverTypes(Assembly containingAssembly);

        void CreateStore();
        void DeleteStore();
        bool StoreExists { get; }
        void EnsureCompatibility();

        void Insert(object item);

        EntityInfo GetEntityInfo(string entityName);

        T[] Select<T>() where T : new();
        T[] Select<T>(bool fillReferences) where T : new();
        T Select<T>(object primaryKey) where T : new();
        T Select<T>(object primaryKey, bool fillReferences) where T : new();
        T[] Select<T>(string searchFieldName, object matchValue) where T : new();
        T[] Select<T>(string searchFieldName, object matchValue, bool fillReferences) where T : new();
        T[] Select<T>(IEnumerable<FilterCondition> filters) where T : new();
        T[] Select<T>(IEnumerable<FilterCondition> filters, bool fillReferences) where T : new();
        object[] Select(Type entityType);
        object[] Select(Type entityType, bool fillReferences);
        T[] Select<T>(Func<T, bool> selector) where T : new();

        void Update(object item);
        void Update(object item, bool cascadeUpdates, string fieldName);
        void Update(object item, string fieldName);

        void Delete(object item);
        void Delete<T>(object primaryKey);
        void Delete<T>();
        void Delete<T>(string fieldName, object matchValue);

        T[] Fetch<T>(int fetchCount) where T : new();
        T[] Fetch<T>(int fetchCount, int firstRowOffset) where T : new();
        T[] Fetch<T>(int fetchCount, int firstRowOffset, string sortField) where T : new();
        T[] Fetch<T>(int fetchCount, int firstRowOffset, string sortField, FieldSearchOrder sortOrder, FilterCondition filter, bool fillReferences)
            where T : new();

        int Count<T>();

        bool Contains(object item);

        void FillReferences(object instance);
    }
}
