﻿using OpenNETCF.ORM;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Linq;

namespace OpenNETCF.ORM.SqlServer.Integration.Test
{
    [TestClass()]
    public class SqlServerDynamicEntityTest
    {
        public TestContext TestContext { get; set; }

        private SqlConnectionInfo GetInfo()
        {
            return new SqlConnectionInfo("192.168.10.47,1433", "TestDB")
            {
                UserName = "testuser",
                Password = "pwd123!sql"
            };
        }

        private SqlServerDataStore GetTestStore()
        {
            var store = new SqlServerDataStore(GetInfo());

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

        [TestMethod()]
        public void BasicDynamicCRUDTest()
        {
            var fieldList = new List<FieldAttribute>();
            fieldList.Add(new FieldAttribute()
            {
                FieldName = "ID",
                IsPrimaryKey = true,
                DataType = System.Data.DbType.Int32
            });

            fieldList.Add(new FieldAttribute()
            {
                FieldName = "FirstName",
                DataType = System.Data.DbType.String
            });

            fieldList.Add(new FieldAttribute()
            {
                FieldName = "LastName",
                DataType = System.Data.DbType.String,
                AllowsNulls = false
            });

            var definition = new DynamicEntityDefinition("People", fieldList, KeyScheme.Identity);

            var store = GetTestStore();

            var exists = store.TableExists(definition.EntityName);
            if (exists)
            {
                store.DropTable(definition.EntityName);
            }

            store.RegisterDynamicEntity(definition);

            Assert.IsTrue(store.TableExists(definition.EntityName));

            var entity = new DynamicEntity("People");
            entity.Fields["FirstName"] = "John";
            entity.Fields["LastName"] = "Doe";
            store.Insert(entity);

            entity = new DynamicEntity("People");
            entity.Fields["FirstName"] = "Jim";
            entity.Fields["LastName"] = "Smith";
            store.Insert(entity);

            entity = new DynamicEntity("People");
            entity.Fields["FirstName"] = "Sam";
            entity.Fields["LastName"] = "Adams";
            store.Insert(entity);

            var items = store.Select("People");
            DumpData(items);

            store.Delete("People", items.First().Fields["ID"]);

            items = store.Select("People");
            DumpData(items);

            store.Delete("People", "LastName", "Smith");

            items = store.Select("People");
            DumpData(items);

            var person = items.First();
            person.Fields["FirstName"] = "Joe";
            person.Fields["LastName"] = "Satriani";
            store.Update(person);

            items = store.Select("People");
            DumpData(items);

            // now let's change the structure and see what happens
            fieldList.Add(new FieldAttribute()
            {
                FieldName = "Middle_Name",
                DataType = System.Data.DbType.Double,
                AllowsNulls = true // this has to be true to add a column
            });

            var newDefinition = new DynamicEntityDefinition("People", fieldList, KeyScheme.Identity);

            store.RegisterDynamicEntity(newDefinition, true);

            items = store.Select("People");
            DumpData(items);
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
