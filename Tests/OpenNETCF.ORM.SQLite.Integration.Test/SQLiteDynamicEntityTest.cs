using OpenNETCF.ORM;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace OpenNETCF.ORM.SQLite.Integration.Test
{
    [TestClass()]
    public class SQLiteDynamicEntityTest
    {
        public TestContext TestContext { get; set; }

        private SQLiteDataStore GetTestStore()
        {
            var store = new SQLiteDataStore("test.db");

            if (store.StoreExists)
            {
                store.DeleteStore();
            }

            if (!store.StoreExists)
            {
                store.CreateStore();
            }
            else
            {
                store.EnsureCompatibility();
            }

            return store;
        }

        [TestInitialize]
        public void Setup()
        {

        }

        [TestMethod()]
        [DeploymentItem("SQLite.Interop.dll")]
        public void BasicDynamicCRUDTest()
        {
            var tableName = "TestChars";
            var field0 = "ID";
            var field1 = "first_name";
            var field2 = "last.name";
            var field3 = "middle_name";

            var fieldList = new List<FieldAttribute>();
            fieldList.Add(new FieldAttribute()
            {
                FieldName = field0,
                IsPrimaryKey = true,
                DataType = System.Data.DbType.Int32
            });

            fieldList.Add(new FieldAttribute()
            {
                FieldName = field1,
                DataType = System.Data.DbType.String
            });

            fieldList.Add(new FieldAttribute()
            {
                FieldName = field2,
                DataType = System.Data.DbType.String,
                AllowsNulls = false
            });

            var definition = new DynamicEntityDefinition(tableName, fieldList, KeyScheme.None);

            var store = GetTestStore();

            var exists = store.TableExists(definition.EntityName);
            if (exists)
            {
                store.DropTable(definition.EntityName);
            }
            else
            {
                store.RegisterDynamicEntity(definition);
            }

            Assert.IsTrue(store.TableExists(definition.EntityName));

            var entity = new DynamicEntity(tableName);
            entity.Fields[field0] = 1;
            entity.Fields[field1] = "John";
            entity.Fields[field2] = "Doe";
            store.Insert(entity);

            entity = new DynamicEntity(tableName);
            entity.Fields[field0] = 2;
            entity.Fields[field1] = "Jim";
            entity.Fields[field2] = "Smith";
            store.Insert(entity);

            entity = new DynamicEntity(tableName);
            entity.Fields[field0] = 3;
            entity.Fields[field1] = "Sam";
            entity.Fields[field2] = "Adams";
            store.Insert(entity);

            var items = store.Select(tableName);
            DumpData(items);

            store.Delete(tableName, items.First().Fields[field0]);

            items = store.Select(tableName);
            DumpData(items);

            store.Delete(tableName, field2, "Smith");

            items = store.Select(tableName);
            DumpData(items);

            var person = items.First();
            person.Fields[field1] = "Joe";
            person.Fields[field2] = "Satriani";
            store.Update(person);

            items = store.Select(tableName);
            DumpData(items);

            // now let's change the structure and see what happens
            fieldList.Add(new FieldAttribute()
            {
                FieldName = field3,
                DataType = System.Data.DbType.Double,
                AllowsNulls = true // this has to be true to add a column
            });

            var newDefinition = new DynamicEntityDefinition(tableName, fieldList, KeyScheme.Identity);
            store.RegisterDynamicEntity(newDefinition, true);

            items = store.Select(tableName);

            DumpData(items);

            store.Dispose();
        }

        void DumpData(IEnumerable<DynamicEntity> items)
        {
            var first = true;

            foreach (var p in items)
            {
                if (first)
                {
                    Debug.WriteLine(p.EntityName);
                    Debug.WriteLine(new string('=', p.EntityName.Length));

                    foreach (var f in p.Fields)
                    {
                        Debug.Write(string.Format("| {0} ", f.Name));
                    }

                    Debug.WriteLine("|");

                    first = false;
                }

                foreach (var f in p.Fields)
                {
                    Debug.Write(string.Format("| {0} ", f.Value));
                }
                Debug.WriteLine("|");
            }
        }
    }
}
