using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using OpenNETCF.ORM.Test.Entities;
using System.Data.SqlServerCe;
using System.IO;

namespace OpenNETCF.ORM.Test
{
    class SqlCeTest
    {
        private const int IterationsPerTest = 50;

        public void ShowAuthorsPaged(SqlCeDataStore store)
        {
            Author[] authors;

            int offset = 0;
            int page = 0;
            int authorsPerPage = 10;

            do
            {
                authors = store.Fetch<Author>(authorsPerPage, offset, "Name");
                offset += authors.Length;

                Debug.WriteLine(string.Format("Authors page {0}", page));
                for (int a = 0; a < authors.Length; a++)
                {
                    Debug.WriteLine(string.Format("  ID: {0}  Name: {1}", authors[a].AuthorID, authors[a].Name));
                }

                page++;
            }
            while (authors.Length > 0);
        }


        public void RunTests()
        {
            List<ITestClass> tests = new List<ITestClass>();

            // create the sqlce store (we'll use it to populate sample data)
            SqlCeStoreTest store = new SqlCeStoreTest();
            store.Initialize();
            tests.Add(store);

            Debug.WriteLine(string.Format("Data set has {0} Books and {1} Authors", store.GetBookCount(), store.GetAuthorCount()));

            // uncomment to test truncation
            // store.TruncateBooks();
            // Debug.WriteLine(string.Format("Data set has now has {0} Books", store.GetBookCount()));

            // uncomment to test binary data
            //store.TestBinaryCRUD();

            // uncomment to test serialization
            //store.TestCustomObjectCRUD();

            // uncomment to test enum field handling
            store.TestEnumCRUD();

            TestCascadingInsert(tests);
            TestCascadingUpdates(tests);

            var test = new SqlCeDirectTest();
            test.Initialize();
            tests.Add(test);

            var r = new Random(Environment.TickCount);

            // now run the tests
            TestGetEntityCount(tests);

            TestGetAllBooks(tests);
            TestGetBookById(tests, r.Next(store.LastBookID));
            TestGetBooksByType(tests);

            // get a random author name
            var author = store.GetAuthorById(r.Next(store.LastAuthorID));

            TestGetAuthorByName(tests, author.Name);

            TestGetAuthorsByPage(tests, 10);
        }

        private void TestCascadingUpdates(List<ITestClass> tests)
        {
            var author = new Author
            {
                Name = "Theodore Geisel"
            };

            foreach (var t in tests)
            {
                t.Insert(author);

                var book = new Book
                {
                    BookType = BookType.Fiction,
                    Title = "Fox in Sox"
                };

                author.Books = new Book[] { book };

                t.Update(author);

                var existing = t.GetAuthorById(author.AuthorID);

                // the book should have been inserted, so it will be at index 0 now
                existing.Books[0].Title = "Green Eggs and Ham";

                // this should cascade update the book title
                t.Update(existing);

                existing = t.GetAuthorById(author.AuthorID);
            }
        }

        private void TestCascadingInsert(List<ITestClass> tests)
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


            foreach (var t in tests)
            {
                var initialCount = t.GetBookCount();

                t.Insert(a);

                var author = t.GetAuthorById(a.AuthorID);
                var count = t.GetBookCount();

                var diff = count - initialCount;
                // diff should == 3
                if (diff != 3) Debugger.Break();
            }

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

            foreach (var t in tests)
            {
                var initialCount = t.GetBookCount();

                t.Insert(a2);

                var author = t.GetAuthorById(a.AuthorID);
                var count = t.GetBookCount();
                var diff = count - initialCount;

                // diff should == 1
                if (diff != 1) Debugger.Break();
            }


        }

        private void TestGetAllBooks(List<ITestClass> tests)
        {
            Stopwatch sw = new Stopwatch();

            foreach (var t in tests)
            {
                sw.Reset();

                for (int i = 0; i < IterationsPerTest; i++)
                {
                    sw.Start();
                    var books = t.GetAllBooks();
                    sw.Stop();

                    if (i == 0)
                    {
                        Debug.WriteLine(string.Format("{0} GetAllBooks (pass 1):\t{1} s",
                            t.GetType().Name,
                            sw.Elapsed.TotalSeconds));
                        sw.Reset();
                    }
                }
                Debug.WriteLine(string.Format("{0} GetAllBooks (mean):\t{1} s",
                    t.GetType().Name,
                    sw.Elapsed.TotalSeconds / (IterationsPerTest - 1)));
            }
        }

        private void TestGetBookById(List<ITestClass> tests, int id)
        {
            Stopwatch sw = new Stopwatch();

            foreach (var t in tests)
            {
                sw.Reset();
                for (int i = 0; i < IterationsPerTest; i++)
                {
                    sw.Start();

                    var books = t.GetBookById(id);
                    sw.Stop();

                    if (i == 0)
                    {
                        Debug.WriteLine(string.Format("{0} GetBookById (pass 1):\t{1} s",
                            t.GetType().Name,
                            sw.Elapsed.TotalSeconds));
                        sw.Reset();
                    }
                }
                Debug.WriteLine(string.Format("{0} GetBookById (mean):\t{1} s",
                    t.GetType().Name,
                    sw.Elapsed.TotalSeconds / (IterationsPerTest - 1)));
            }
        }

        private void TestGetBooksByType(List<ITestClass> tests)
        {
            Stopwatch sw = new Stopwatch();

            foreach (var t in tests)
            {
                sw.Reset();
             
                for (int i = 0; i < IterationsPerTest; i++)
                {
                    sw.Start();

                    var books = t.GetBooksOfType(BookType.NonFiction);
                    sw.Stop();

                    if (i == 0)
                    {
                        Debug.WriteLine(string.Format("{0} GetBooksOfType (pass 1):\t{1} s",
                            t.GetType().Name,
                            sw.Elapsed.TotalSeconds));
                        sw.Reset();
                    }
                }
                Debug.WriteLine(string.Format("{0} GetBooksOfType (mean):\t{1} s",
                    t.GetType().Name,
                    sw.Elapsed.TotalSeconds / (IterationsPerTest - 1)));
            }
        }

        private void TestGetAuthorByName(List<ITestClass> tests, string name)
        {
            Stopwatch sw = new Stopwatch();

            foreach (var t in tests)
            {
                sw.Reset();

                for (int i = 0; i < IterationsPerTest; i++)
                {
                    sw.Start();

                    var author = t.GetAuthorByName(name);
                    sw.Stop();

                    if (i == 0)
                    {
                        Debug.WriteLine(string.Format("{0} GetAuthorByName (pass 1):\t{1} s",
                            t.GetType().Name,
                            sw.Elapsed.TotalSeconds));
                        sw.Reset();
                    }
                }
                Debug.WriteLine(string.Format("{0} GetAuthorByName (mean):\t{1} s",
                    t.GetType().Name,
                    sw.Elapsed.TotalSeconds / (IterationsPerTest - 1)));
            }
        }

        private void TestGetAuthorsByPage(List<ITestClass> tests, int authorsPerPage)
        {
            Stopwatch sw = new Stopwatch();

            foreach (var t in tests)
            {
                int count = 0;
                sw.Reset();

                for (int i = 0; i < IterationsPerTest; i++)
                {

                    int offset = 0;
                    int page = 0;

                    Author[] authors;

                    do
                    {
                        sw.Start();
                        authors = t.GetAuthors(authorsPerPage, offset);
                        sw.Stop();
                        count++;
                        if (authors == null) break;

                        offset += authors.Length;

                        page++;
                    }
                    while (authors.Length > 0);
                }

                Debug.WriteLine(string.Format("{0} TestGetAuthorsByPage (per page):\t{1} s",
                    t.GetType().Name,
                    sw.Elapsed.TotalSeconds / (count)));
            }
        }

        private void TestGetEntityCount(List<ITestClass> tests)
        {
            Stopwatch sw = new Stopwatch();

            foreach (var t in tests)
            {
                sw.Reset();

                for (int i = 0; i < IterationsPerTest; i++)
                {
                    sw.Start();

                    var count = t.GetBookCount();
                    sw.Stop();

                    if (i == 0)
                    {
                        Debug.WriteLine(string.Format("{0} GetBookCount (pass 1):\t{1} s",
                            t.GetType().Name,
                            sw.Elapsed.TotalSeconds));
                        sw.Reset();
                    }
                }
                Debug.WriteLine(string.Format("{0} GetBookCount (mean):\t{1} s",
                    t.GetType().Name,
                    sw.Elapsed.TotalSeconds / (IterationsPerTest - 1)));
            }
        }
        
    }
}
