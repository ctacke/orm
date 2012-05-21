using OpenNETCF.ORM;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System;

namespace OpenNETCF.ORM.SqlCE.Integration.Test
{
    [TestClass()]
    public class SqlCeDataStoreTest
    {
        public TestContext TestContext { get; set; }

        [TestMethod()]
        [DeploymentItem("OpenNETCF.ORM.SqlCe.dll")]
        public void SimpleCRUDTest()
        {
            bool beforeInsert = false;
            bool afterInsert = false;
            bool beforeUpdate = false;
            bool afterUpdate = false;
            bool beforeDelete = false;
            bool afterDelete = false;

            var store = new SqlCeDataStore("test.sdf");
            store.AddType<TestItem>();
            store.CreateStore();

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

            item = store.Select<TestItem>(3);
            Assert.IsTrue(item.Equals(itemC));

            // FETCH

            // UPDATE
            itemC.Name = "NewItem";
            itemC.Address = "Changed Address";
            itemC.TS = new TimeSpan(8, 23, 30);
            store.Update(itemC);

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
        }
    }

    [Entity(KeyScheme = KeyScheme.Identity)]
    public class TestItem : IEquatable<TestItem>
    {
        public TestItem()
        {
        }

        public TestItem(string name)
        {
            Name = name;
        }

        [Field(IsPrimaryKey = true)]
        public int ID { get; set; }

        [Field]
        public string Name { get; set; }

        [Field]
        public string Address { get; set; }

        [Field]
        public TimeSpan TS { get; set; }

        public bool Equals(TestItem other)
        {
            return this.ID == other.ID;
        }
    }
}
