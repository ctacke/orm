using OpenNETCF.ORM.SQLite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace OpenNETCF.ORM.SQLite.Integration.Test
{
    [TestClass()]
    public class SQLiteDataStoreTest
    {
        public TestContext TestContext { get; set; }

        [TestMethod()]
        [DeploymentItem("SQLite.Interop.dll")]
        public void SimpleCRUDTest()
        {
            var store = new SQLiteDataStore("test.db");
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

            item = store.Select<TestItem>(3);
            Assert.IsTrue(item.Equals(itemC));

            // FETCH

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

        [TestMethod()]
        [DeploymentItem("SQLite.Interop.dll")]
        public void SimpleReferenceTest()
        {
            var store = new SQLiteDataStore("simpleReferenceTest.db");
            store.AddType<Author>();
            store.AddType<Book>();
            store.CreateOrUpdateStore();

            // insert an author
            var dumas = new Author() { Name = "Alexadre Dumas" };
            store.Insert(dumas);

            // insert a couple books.
            // note that we're inserting the foreign key value
            store.Insert(
                new Book()
                {
                    AuthorID = dumas.ID,
                    Title = "The Count of Monte Cristo"
                });

            store.Insert(
                new Book()
                {
                    AuthorID = dumas.ID,
                    Title = "The Three Musketeers"
                });

            // now get the authors back, telling ORM to fill the references
            var authors = store.Select<Author>(true).ToArray();

            // at this point you will have 1 Author instance, with the Books property hydrated and containing two Book instances
        }

        [TestMethod()]
        [DeploymentItem("SQLite.Interop.dll")]
        public void SimpleReferenceTest2()
        {
            var store = new SQLiteDataStore("simpleReferenceTest.db");
            store.AddType<Location>();
            store.AddType<Position>();
            store.CreateOrUpdateStore();

            var position1 = new Position() { Description = "Position 1" };
            store.Insert(position1);

            store.Insert(
                new Location()
                {
                    Description = "Description A",
                    positionId = position1.positionId
                });

            var positions = store.Select<Position>(true).ToArray();
        }

        [TestMethod()]
        [DeploymentItem("SQLite.Interop.dll")]
        public void LongIDTest()
        {
            var store = new SQLiteDataStore("test2.db");
            store.AddType<BigID>();
            store.CreateStore();

            store.Insert(new BigID("Foo"));
            var bid = store.Select<BigID>();
        }
    }
}
