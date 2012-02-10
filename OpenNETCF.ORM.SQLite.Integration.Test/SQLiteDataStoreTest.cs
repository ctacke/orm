using OpenNETCF.ORM.SQLite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace OpenNETCF.ORM.SQLite.Integration.Test
{
    [TestClass()]
    public class SQLiteDataStoreTest
    {
        public TestContext TestContext { get; set; }
        [TestMethod()]
        [DeploymentItem("OpenNETCF.ORM.SqlCe.dll")]
        public void SelectTest()
        {
            var store = new SQLiteDataStore("test.db");
            store.AddType<TestItem>();
            store.CreateStore();

            store.Insert(new TestItem("ItemA"));
            //store.Insert(new TestItem("ItemB"));
            //store.Insert(new TestItem("ItemC"));

            //var item = store.Select<TestItem>("Name", "ItemB").FirstOrDefault();
            //item = store.Select<TestItem>(2);
        }
    }

    [Entity(KeyScheme=KeyScheme.Identity)]
    public class TestItem
    {
        public TestItem()
        {
        }

        public TestItem(string name)
        {
            Name = name;
        }

        [Field(IsPrimaryKey=true)]
        int ID { get; set; }

        [Field]
        string Name { get; set; }
    }
}
