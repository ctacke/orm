using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace OpenNETCF.ORM
{
    public interface IDataStore
    {
        void AddType(Type entityType);
        void DiscoverTypes(Assembly containingAssembly);

        void CreateStore();
        void DeleteStore();
        bool StoreExists { get; }
        void EnsureCompatibility();

        void Insert(object item);

        EntityInfo GetEntityInfo(string entityName);

        T[] Select<T>() where T : new();
        T Select<T>(object primaryKey) where T : new();
        T[] Select<T>(string searchFieldName, object matchValue) where T : new();
        T[] Select<T>(IEnumerable<FilterCondition> filters) where T : new();
        object[] Select(Type entityType);

        void Update(object item);
        void Update(object item, bool cascadeUpdates);

        void Delete(object item);
        void Delete<T>(object primaryKey);
        void Delete<T>();
        void Delete<T>(string fieldName, object matchValue);

        T[] Fetch<T>(int fetchCount) where T : new();
        T[] Fetch<T>(int fetchCount, int firstRowOffset) where T : new();
        T[] Fetch<T>(string searchFieldName, int fetchCount, int firstRowOffset) where T : new();

        int Count<T>();

        bool Contains(object item);

        void FillReferences(object instance);
    }
}
