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
            var store = new SqlCeDataStore("test.sdf");
            store.AddType<TestItem>();
            store.CreateStore();

            var itemA = new TestItem("ItemA");
            var itemB = new TestItem("ItemB");
            var itemC = new TestItem("ItemC");
            
            // INSERT
            store.Insert(itemA);
            store.Insert(itemB);
            store.Insert(itemC);

            // SELECT
            var item = store.Select<TestItem>("Name", itemB.Name).FirstOrDefault();
            Assert.AreEqual(item, itemB);

            item = store.Select<TestItem>(3);
            Assert.AreEqual(item, itemC);

            // FETCH

            // UPDATE
            itemC.Name = "NewItem";
            store.Update(itemC);

            item = store.Select<TestItem>("Name", "ItemC").FirstOrDefault();
            Assert.IsNull(item);
            item = store.Select<TestItem>("Name", itemC.Name).FirstOrDefault();
            Assert.AreEqual(item, itemC);

            // DELETE
        }
    }

    [Entity(KeyScheme=KeyScheme.Identity)]
    public class TestItem : IEquatable<TestItem>
    {
        public TestItem()
        {
        }

        public TestItem(string name)
        {
            Name = name;
        }

        [Field(IsPrimaryKey=true)]
        public int ID { get; set; }

        [Field]
        public string Name { get; set; }

        public bool Equals(TestItem other)
        {
            return this.ID == other.ID;
        }
    }
}
