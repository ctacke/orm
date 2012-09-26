using OpenNETCF.ORM;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System;
using OpenNETCF.ORM.Test;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using OpenNETCF.ORM.Test.Entities;

namespace OpenNETCF.ORM.SqlCE.Integration.Test
{
    [TestClass()]
    public class SqlCeDataStoreTest
    {
        public TestContext TestContext { get; set; }

        [TestMethod()]
        public void SimpleTransactionTest()
        {
            var store = new SqlCeDataStore("test_trans.sdf");
            store.AddType<LateAddItem>();

            if (store.StoreExists)
            {
                store.DeleteStore();
            }
            store.CreateStore();

            var testEntity = new LateAddItem() { ID = -1 };
            store.Insert(testEntity);

            var id = testEntity.ID;

            store.BeginTransaction();
            store.Delete<LateAddItem>(id);
            // timeout here, at insert
            store.Insert(new LateAddItem() { ID = -1 });
            store.Commit();
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

            item = store.Select<TestItem>(3);
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
            item = store.Select<TestItem>(3);
            Assert.IsTrue(item.Name == itemC.Name);
            store.Rollback();

            item = store.Select<TestItem>(3);
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

            // this will *not* create the table
            store.AddType<LateAddItem>();

            Exception expected = null;
            try
            {
                var newitems = store.Select<LateAddItem>(false);
            }
            catch (Exception ex)
            {
                expected = ex;
            }

            Assert.IsNotNull(expected);

        }

        [TestMethod()]
        public void PerfTest()
        {
            var store = Initialize();
//            TestEnumCRUD(store);

//            TestCascadingInsert(store);
//            TestCascadingUpdates(store);

//            TestGetEntityCount(store, 100);
            TestGetAllBooks(store, 10);

            var lastID = store.Count<Book>();
            var searchID = new Random().Next(lastID);
            TestGetBookById(store, 100, searchID);
        }

        private SqlCeDataStore Initialize()
        {

            var store = new SqlCeDataStore("pubs.sdf");
            if (store.StoreExists)
            {
                store.DeleteStore();
            }

            store.ConnectionBehavior = ConnectionBehavior.Persistent;

            Stopwatch sw = new Stopwatch();

            sw.Start();
            store.DiscoverTypes(Assembly.GetExecutingAssembly());
            sw.Stop();
            Debug.WriteLine(string.Format("SQL CE Store Discover:\t{0}s", sw.Elapsed.TotalSeconds));

            sw.Start();
            store.CreateStore();
            sw.Stop();
            Debug.WriteLine(string.Format("SQL CE Store Create:\t{0}", sw.Elapsed.TotalSeconds));

            CreateTestData(store);

            return store;
        }

        private void CreateTestData(SqlCeDataStore store)
        {
            DataGenerator generator = new DataGenerator();

            var authors = generator.GenerateAuthors(100);

            int authorID = 0;
            int bookID = 0;
            List<long> authorEts = new List<long>();
            List<long> bookEts = new List<long>();

            var r = new Random(Environment.TickCount);

            foreach (var author in authors)
            {
                author.AuthorID = authorID++;

                // each author will have 0 to 5 books
                var books = generator.GenerateBooks(r.Next(5));

                foreach (var book in books)
                {
                    book.BookID = bookID++;
                    book.AuthorID = author.AuthorID;

                    store.Insert(book);
//                    LastBookID = book.BookID;
                }

                store.Insert(author);
//                LastAuthorID = author.AuthorID;
            }
        }

        public void TestEnumCRUD(SqlCeDataStore store)
        {
            // truncate the table for this test
            store.Delete<TestTable>();

            var testRow = new TestTable
            {
                EnumField = TestEnum.ValueB
            };

            store.Insert(testRow);

            var existing = store.Select<TestTable>().First();

            Assert.AreEqual(existing.EnumField, testRow.EnumField);

            existing.EnumField = TestEnum.ValueC;
            store.Update(existing);
            var secondPull = store.Select<TestTable>().First();

            Assert.AreEqual(existing.EnumField, secondPull.EnumField);
        }

        private void TestCascadingUpdates(SqlCeDataStore store)
        {
            var author = new Author
            {
                Name = "Theodore Geisel"
            };

                store.Insert(author);

                var book = new Book
                {
                    BookType = BookType.Fiction,
                    Title = "Fox in Sox"
                };

                author.Books = new Book[] { book };

                store.Update(author);

                var existing = store.Select<Author>(author.AuthorID, true);
                Assert.AreEqual(1, existing.Books.Length);
                Assert.AreEqual("Fox in Sox", existing.Books[0].Title);

                // replace the book title in the author's collection
                existing.Books[0].Title = "Green Eggs and Ham";

                // now Update the Author - this should cascade update the book title
                store.Update(existing);

                existing = store.Select<Author>(author.AuthorID, true);
                Assert.AreEqual("Green Eggs and Ham", existing.Books[0].Title);
            
        }

        private void TestCascadingInsert(SqlCeDataStore store)
        {
            var testBooks = new Book[]
                {
                    new Book
                    {
                      Title = "CSS: The Missing Manual",
                      BookType = BookType.NonFiction
                    },

                    new Book
                    {
                        Title = "JavaScript: The Missing Manual",
                        BookType = BookType.NonFiction
                    },

                    new Book
                    {
                        Title = "Dreamweaver: The Missing Manual",
                        BookType = BookType.NonFiction
                    },
                };

            // ensures that the entity *and its references* get inserted
            Author a = new Author
            {
                Name = "David McFarland",

                Books = testBooks
            };


            var initialCount = store.Count<Book>();

            // insert, telling ORM to insert references (cascade)
            store.Insert(a, true);

            // pull back to verify
            var author = store.Select<Author>(a.AuthorID, true);
            var count = store.Count<Book>();

            // we should have inserted 3 new books
            var diff = count - initialCount;
            Assert.IsTrue(diff == 3);


            // create a new author with the same books - the books should *not* get re-inserted - plus one new book
            List<Book> newList = new List<Book>(testBooks);
            newList.Add(
                new Book
                {
                    Title = "My Coauthors Book",
                    BookType = BookType.NonFiction
                }
                    );

            Author a2 = new Author
            {
                Name = "Test CoAuthor",

                Books = newList.ToArray()
            };

            initialCount = store.Count<Book>();

            // insert, telling ORM to insert references (cascade)
            store.Insert(a2, true);

            author = store.Select<Author>(a.AuthorID, true);
            count = store.Count<Book>();

            // we should have inserted 1 new book
            diff = count - initialCount;
            Assert.IsTrue(diff == 1);



        }

        private void TestGetEntityCount(SqlCeDataStore store, int iterations)
        {
            Stopwatch sw = new Stopwatch();

                sw.Reset();

                for (int i = 0; i < iterations; i++)
                {
                    sw.Start();

                    var count = store.Count<Book>();
                    sw.Stop();

                    if (i == 0)
                    {
                        Debug.WriteLine(string.Format("GetBookCount (pass 1):\t{0} s",
                            sw.Elapsed.TotalSeconds));
                        sw.Reset();
                    }
                }
                Debug.WriteLine(string.Format("GetBookCount (mean):\t{0} s",
                    sw.Elapsed.TotalSeconds / (iterations - 1)));
        }

        private void TestGetAllBooks(SqlCeDataStore store, int iterations)
        {
            Stopwatch sw = new Stopwatch();

            sw.Reset();

            for (int i = 0; i < iterations; i++)
            {
                sw.Start();
                var books = store.Select<Book>();
                sw.Stop();

                if (i == 0)
                {
                    Debug.WriteLine(string.Format("GetAllBooks (pass 1):\t{0} s",
                        sw.Elapsed.TotalSeconds));
                    sw.Reset();
                }
            }
            Debug.WriteLine(string.Format("GetAllBooks (mean):\t{0} s",
                sw.Elapsed.TotalSeconds / (iterations - 1)));

        }

        private void TestGetBookById(SqlCeDataStore store, int iterations, int id)
        {
            Stopwatch sw = new Stopwatch();

            sw.Reset();
            for (int i = 0; i < iterations; i++)
            {
                sw.Start();

                var books = store.Select<Book>(id);
                sw.Stop();

                if (i == 0)
                {
                    Debug.WriteLine(string.Format("GetBookById (pass 1):\t{0} s",
                        sw.Elapsed.TotalSeconds));
                    sw.Reset();
                }
            }
            Debug.WriteLine(string.Format("GetBookById (mean):\t{0} s",
                sw.Elapsed.TotalSeconds / (iterations - 1)));
        }
    }
}
