﻿using OpenNETCF.ORM;
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
            var store = new SqlCeDataStore("test.sdf");
            store.AddType<TestItem>();
            store.CreateStore();

            store.BeforeUpdate += new EventHandler<EntityUpdateArgs>(store_BeforeUpdate);
            store.AfterUpdate += new EventHandler<EntityUpdateArgs>(store_AfterUpdate);
            
            var itemA = new TestItem("ItemA");
            var itemB = new TestItem("ItemB");
            var itemC = new TestItem("ItemC");

            // INSERT
            store.Insert(itemA);
            store.Insert(itemB);
            store.Insert(itemC);

            // COUNT
            var count = store.Count<TestItem>();
            Assert.AreEqual(3, count);

            // SELECT
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

            Assert.IsTrue(m_beforeUpdate, "BeforeUpdate never fired");
            Assert.IsTrue(m_afterUpdate, "AfterUpdate never fired");

            item = store.Select<TestItem>("Name", "ItemC").FirstOrDefault();
            Assert.IsNull(item);
            item = store.Select<TestItem>("Name", itemC.Name).FirstOrDefault();
            Assert.IsTrue(item.Equals(itemC));

            // CONTAINS
            var exists = store.Contains(itemA);
            Assert.IsTrue(exists);

            // DELETE
            store.Delete(itemA);
            item = store.Select<TestItem>("Name", itemA.Name).FirstOrDefault();
            Assert.IsNull(item);

            // CONTAINS
            exists = store.Contains(itemA);
            Assert.IsFalse(exists);

            // COUNT
            count = store.Count<TestItem>();
            Assert.AreEqual(2, count);
        }

        private bool m_beforeUpdate = false;
        private bool m_afterUpdate = false;

        void store_AfterUpdate(object sender, EntityUpdateArgs e)
        {
            m_afterUpdate = true;
        }

        void store_BeforeUpdate(object sender, EntityUpdateArgs e)
        {
            m_beforeUpdate = true;
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
