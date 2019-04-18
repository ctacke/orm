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

        [TestMethod()]
        [DeploymentItem("SQLite.Interop.dll")]
        public void DisposalTest()
        {
            try
            {
                var path = Path.Combine("test.sqlite");
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
            var now = DateTime.Now;

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
                itemB.TS = now.TimeOfDay;

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

                // make sure the timespan worked, since we have to special-case that thing in the depths of the ORM
                Assert.AreEqual(now.TimeOfDay, item.TS);

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
            using (var store = new SQLiteDataStore("simpleReferenceTest.db"))
            {
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
        }

        [TestMethod()]
        [DeploymentItem("SQLite.Interop.dll")]
        public void SimpleReferenceTest2()
        {
            using (var store = new SQLiteDataStore("simpleReferenceTest.db"))
            {
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
        }

        [TestMethod()]
        [DeploymentItem("SQLite.Interop.dll")]
        public void LongIDTest()
        {
            using (var store = new SQLiteDataStore("test2.db"))
            {
                store.AddType<BigID>();
                store.CreateStore();

                store.Insert(new BigID("Foo"));
                var bid = store.Select<BigID>();
            }
        }

        [TestMethod()]
        [DeploymentItem("SQLite.Interop.dll")]
        public void BitTest()
        {
            using (var store = new SQLiteDataStore("test2.db"))
            {
                store.AddType<OSATestSettings>();

                var settings = store.Select<OSATestSettings>().ToArray();
            }
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

            using (var store = new SQLiteDataStore("ref.db"))
            {
                store.AddType<Order>();
                store.AddType<Driver>();
                store.AddType<Vehicle>();
                store.CreateStore();
                store.Insert(Order, true);
            }
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

            using (var store = new SQLiteDataStore("ref1.db"))
            {
                store.AddType<Driver>();
                store.CreateStore();
                foreach (var driver in drivers)
                {
                    store.Insert(driver);
                }

                var items = store.Fetch<Driver>(2).ToArray();
                Assert.AreEqual(2, items.Length);
            }
        }

    }
}
