using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenNETCF.ORM.Tests;
using System.Linq;
using OpenNETCF.ORM.SqlServer.Integration.Test.Entities;

namespace OpenNETCF.ORM.SqlServer.Integration.Test
{
    [TestClass]
    public class SqlServerDataStoreTests
    {
        private SqlConnectionInfo GetInfo()
        {
            var connection = new SqlConnectionInfo();
            connection.DatabaseName = "TEST";
            connection.ServerName = "TEST.opennetcf.com";
            connection.ServerPort = 1433;
            connection.UserName = "TEST";
            connection.Password = "TEST";
            return connection;
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

        [TestMethod]
        public void BaseClassTest()
        {
            var connection = new SqlConnectionInfo();
            connection.DatabaseName = "TEST";
            connection.ServerName = "test.opennetcf.com";
            connection.ServerPort = 1433;
            connection.UserName = "TEST";
            connection.Password = "TEST";

            var store = new SqlServerDataStore(connection);

            try
            {
                store.AddType<PublishedTenantBuildingState>();
                store.AddType<PublishedTenantApartmentState>();
            }
            catch (Exception ex)
            {
            }

        }

        [TestMethod]
        public void GuidPKTest()
        {
            var store = new SqlServerDataStore(GetInfo());

            store.AddType<GuidItem>();

            var item = new GuidItem();

            store.Insert(item);


            var results = store.Select<GuidItem>().ToArray();
            results[0].FieldA = 42;
            store.Update(results[0]);
            results = store.Select<GuidItem>().ToArray();
        }

        [TestMethod]
        public void TestCreateStore()
        {
            var store = new SqlServerDataStore(GetInfo());

            store.CreateStore();
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

            var itemA = new TestItem("ItemA");
            itemA.UUID = Guid.NewGuid();
            itemA.ITest = 5;
            itemA.FTest = 3.14F;
            itemA.DBTest = 1.4D;
            itemA.DETest = 2.678M;

            var itemB = new TestItem("ItemB");
            var itemC = new TestItem("ItemC");

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

        [TestMethod()]
        public void VarBinaryNullTest()
        {
            var store = GetTestStore();
            store.AddType<BinaryItem>();

            var item = new BinaryItem();

            store.Insert(item);

            var test = store.Select<BinaryItem>(item.ID);
        }
    }
}
