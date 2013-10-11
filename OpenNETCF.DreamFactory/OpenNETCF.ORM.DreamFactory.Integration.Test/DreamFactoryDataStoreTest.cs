using OpenNETCF.ORM;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;


namespace OpenNETCF.ORM.DreamFactory.Integration.Test
{
    [TestClass()]
    public class DreamFactoryDataStoreTest
    {
        public TestContext TestContext { get; set; }

        private const string DSP = "https://dsp-opennetcf.cloud.dreamfactory.com/";

        [TestMethod()]
        public void SimpleCRUDTest()        
        {
            var store = new DreamFactoryDataStore(DSP, "ORM", TestCreds.UID, TestCreds.PWD);
            store.AddType<TestItem>();
            store.CreateStore();

            var itemA = new TestItem("ItemA");
            itemA.UUID = Guid.NewGuid();
            itemA.ITest = 5;
            itemA.FTest = 3.14F;
            itemA.DBTest = 1.4D;
            itemA.DETest = 2.678M;

            var itemB = new TestItem("ItemB");
            var itemC = new TestItem("ItemC");

            // TRUNCATE
            store.Delete<TestItem>();

            // INSERT
            store.Insert(itemA);
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

            item = store.Select<TestItem>("ID", itemC.ID).FirstOrDefault();
            Assert.IsTrue(item.Equals(itemC));

            item = store.Select<TestItem>(items.First().ID);
            Assert.IsTrue(item.Equals(items.First()));

            // FETCH
            // TODO:

            // UPDATE
            itemC.Name = "NewItem";
            itemC.Address = "Changed Address";
            itemC.TS = new TimeSpan(8, 23, 30);
            store.Update(itemC);

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
    }
}
