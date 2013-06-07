using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenNETCF.ORM.Tests;
using System.Linq;
using System.Diagnostics;

namespace OpenNETCF.ORM.MySQL.Integration.Test
{
    [TestClass]
    public class UnitTest1
    {
        private MySQLDataStore GetTestStore()
        {
            var info = new MySQLConnectionInfo("192.168.10.246", 3306, "TestDB", "root", "password");

            var store = new MySQLDataStore(info);

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

        [TestMethod()]
        public void SimpleCRUDTest()
        {
            bool beforeInsert = false;
            bool afterInsert = false;
            bool beforeUpdate = false;
            bool afterUpdate = false;
            bool beforeDelete = false;
            bool afterDelete = false;

            var store = GetTestStore();

            store.AddType<TestItem>();

            store.BeforeInsert += delegate
            {
                beforeInsert = true;
            };
            store.AfterInsert += delegate
            {
                afterInsert = true;
            };
            store.BeforeUpdate += delegate
            {
                beforeUpdate = true;
            };
            store.AfterUpdate += delegate
            {
                afterUpdate = true;
            };
            store.BeforeDelete += delegate
            {
                beforeDelete = true;
            };
            store.AfterDelete += delegate
            {
                afterDelete = true;
            };

            store.TruncateTable("TestItem");

            var itemA = new TestItem("ItemA");
            itemA.UUID = Guid.NewGuid();
            itemA.ITest = 5;
            itemA.FTest = 3.14F;
            itemA.DBTest = 1.4D;
            itemA.DETest = 2.678M;

            Debug.WriteLine(itemA.UUID.ToString());

            var itemB = new TestItem("ItemB");
            itemB.ID = 2;
            var itemC = new TestItem("ItemC");
            itemC.ID = 3;

            // INSERT
            store.Insert(itemA);
            Assert.IsTrue(beforeInsert, "BeforeInsert never fired");
            Assert.IsTrue(afterInsert, "AfterInsert never fired");

            store.Insert(itemB);
            store.Insert(itemC);

            // COUNT
            var count = store.Count<TestItem>();
            Assert.AreEqual(3, count);

            // SELECT
            var items = store.Select<TestItem>();
            Assert.AreEqual(3, items.Count());

            var item = store.Select<TestItem>("Name", itemB.Name).FirstOrDefault();
            Assert.IsTrue(item.Equals(itemB));

            item = store.Select<TestItem>(itemC.ID);
            Assert.IsTrue(item.Equals(itemC));

            // FETCH

            // UPDATE
            itemC.Name = "NewItem";
            itemC.Address = "Changed Address";
            itemC.TS = new TimeSpan(8, 23, 30);
            itemC.BigString = "little string";

            // test rollback
            store.BeginTransaction();
            store.Update(itemC);
            item = store.Select<TestItem>(itemC.ID);
            Assert.IsTrue(item.Name == itemC.Name);
            store.Rollback();

            item = store.Select<TestItem>(itemC.ID);
            Assert.IsTrue(item.Name != itemC.Name);

            // test commit
            store.BeginTransaction(System.Data.IsolationLevel.Unspecified);
            store.Update(itemC);
            store.Commit();

            Assert.IsTrue(beforeUpdate, "BeforeUpdate never fired");
            Assert.IsTrue(afterUpdate, "AfterUpdate never fired");

            item = store.Select<TestItem>("Name", "ItemC").FirstOrDefault();
            Assert.IsNull(item);
            item = store.Select<TestItem>("Name", itemC.Name).FirstOrDefault();
            Assert.IsTrue(item.Equals(itemC));

            // CONTAINS
            var exists = store.Contains(itemA);
            Assert.IsTrue(exists);

            // DELETE
            store.Delete(itemA);
            Assert.IsTrue(beforeDelete, "BeforeDelete never fired");
            Assert.IsTrue(afterDelete, "AfterDelete never fired");
            item = store.Select<TestItem>("Name", itemA.Name).FirstOrDefault();
            Assert.IsNull(item);

            // CONTAINS
            exists = store.Contains(itemA);
            Assert.IsFalse(exists);

            // COUNT
            count = store.Count<TestItem>();
            Assert.AreEqual(2, count);

            // this will create the table in newer versions of ORM
            store.AddType<LateAddItem>();

            var newitems = store.Select<LateAddItem>(false);
            Assert.IsNotNull(newitems);

        }
    }
}
