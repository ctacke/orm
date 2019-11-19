using OpenNETCF.ORM.SQLite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using AFL.OSA.Models;
using System.IO;

namespace OpenNETCF.ORM.SQLite.Integration.Test
{
    [TestClass()]
    public class SQLiteDataStoreTest
    {
        public TestContext TestContext { get; set; }
        const string TestResultsDirectory = "c:\\temp";

        [TestMethod()]
        [DeploymentItem("SQLite.Interop.dll")]
        public void DisposalTest()
        {
            try
            {
                var path = Path.Combine(TestResultsDirectory, "test.sqlite");
                var store = new SQLiteDataStore(path);
                store.CreateStore();
                store.Dispose();
                var fs = new FileStream(path, FileMode.Open);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }

        [TestMethod()]
        [DeploymentItem("SQLite.Interop.dll")]
        public void SimpleCRUDTest()
        {
            File.Delete("test.db");

            using (var store = new SQLiteDataStore("test.db"))
            {
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

        [TestMethod()]
        [DeploymentItem("SQLite.Interop.dll")]
        public void BitTest()
        {
            var store = new SQLiteDataStore(@"E:\d\shared\TFS01\orm\Tests\OpenNETCF.ORM.SQLite.Integration.Test\testdb.sqlite");
            store.AddType<OSATestSettings>();

            var settings = store.Select<OSATestSettings>().ToArray();
        }

        [TestMethod()]
        [DeploymentItem("SQLite.Interop.dll")]
        public void ManyToOneReferenceTest()
        {
            var driver = new Driver()
            {
                Name = "Speed Racer"
            };

            driver.Vehicles.AddRange(
                new Vehicle[]
                {
                    new Vehicle()
                    {
                        Model = "Super Fast"
                    },
                    new Vehicle()
                    {
                        Model = "Sorta Fast"
                    }
                });

            var Order = new Order()
            {
                Number = "1234",
                Driver = driver
            };

            var store = new SQLiteDataStore("test.sqlite");
            store.AddType<Order>();
            store.AddType<Driver>();
            store.AddType<Vehicle>();
            store.CreateStore();
            store.Insert(Order, true);
        }

        [TestMethod()]
        [DeploymentItem("SQLite.Interop.dll")]
        public void SingleEntityFetchTest()
        {
            var drivers = new Driver[]
                {
                    new Driver()
                    {
                        Name = "Speed Racer"
                    },
                    new Driver()
                    {
                        Name = "Shaggy"
                    },
                    new Driver()
                    {
                        Name = "Mario"
                    },
                    new Driver()
                    {
                        Name = "Luigi"
                    },
                };

            var store = new SQLiteDataStore("test.sqlite");
            store.AddType<Driver>();
            store.CreateStore();
            foreach (var driver in drivers)
            {
                store.Insert(driver);
            }

            var items = store.Fetch<Driver>(2).ToArray();
            Assert.AreEqual(2, items.Length);
        }

        [TestMethod()]
        [DeploymentItem("SQLite.Interop.dll")]
        [ExpectedException(typeof(ArgumentException))]
        public void SimpleGuidIDEntityTest()
        {
            File.Delete("test.db");

            using (var store = new SQLiteDataStore("test.db"))
            {
                store.AddType<GuidItem>();
                store.CreateStore();

                var item = new GuidItem();
                store.Insert(item);

                var existing = store.Select<GuidItem>(item.ID);
                Assert.IsNotNull(existing);
                Assert.AreEqual(item.ID, existing.ID);

                store.Delete<GuidItem>(item.ID);
                existing = store.Select<GuidItem>(item.ID);
                Assert.IsNull(existing);
            }
        }

        [TestMethod()]
        [DeploymentItem("SQLite.Interop.dll")]
        [ExpectedException(typeof(ArgumentException))]
        public void PrimaryKeyWrongTypeTest1()
        {
            File.Delete("test.db");

            using (var store = new SQLiteDataStore("test.db"))
            {
                store.AddType<BadKeyTypeAItem>();
            }
        }

        [TestMethod()]
        [DeploymentItem("SQLite.Interop.dll")]
        [ExpectedException(typeof(ArgumentException))]
        public void PrimaryKeyWrongTypeTest2()
        {
            File.Delete("test.db");

            using (var store = new SQLiteDataStore("test.db"))
            {
                store.AddType<BadKeyTypeBItem>();
            }
        }

        [TestMethod()]
        [DeploymentItem("SQLite.Interop.dll")]
        [ExpectedException(typeof(ArgumentException))]
        public void NoPrimaryKeyTest()
        {
            File.Delete("test.db");

            using (var store = new SQLiteDataStore("test.db"))
            {
                store.AddType<NoPKGuidItem>();
            }
        }
    }
}
