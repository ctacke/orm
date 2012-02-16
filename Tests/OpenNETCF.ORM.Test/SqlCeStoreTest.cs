using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using OpenNETCF.ORM.Test.Entities;

namespace OpenNETCF.ORM.Test
{
    public class SqlCeStoreTest : ITestClass
    {
        private SqlCeDataStore Store { get; set; }

        public int LastBookID { get; private set; }
        public int LastAuthorID { get; private set; }

        public void Initialize()
        {

            Store = new SqlCeDataStore("pubs.sdf");
            if (Store.StoreExists)
            {
                Store.DeleteStore();
            }

            Store.ConnectionBehavior = ConnectionBehavior.Persistent;

            Stopwatch sw = new Stopwatch();

            sw.Start();
            Store.DiscoverTypes(Assembly.GetExecutingAssembly());
            sw.Stop();
            Debug.WriteLine(string.Format("SQL CE Store Discover:\t{0}s", sw.Elapsed.TotalSeconds));

            sw.Start();
            Store.CreateStore();
            sw.Stop();
            Debug.WriteLine(string.Format("SQL CE Store Create:\t{0}", sw.Elapsed.TotalSeconds));

            CreateTestData();
        }

        public void CreateTestData()
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

                    Store.Insert(book);
                    LastBookID = book.BookID;
                }

                Store.Insert(author);
                LastAuthorID = author.AuthorID;
            }
        }

        public int GetAuthorCount()
        {
            return Store.Count<Author>();
        }

        public int GetBookCount()
        {
            return Store.Count<Book>();
        }

        private void DiscoverTypes(SqlCeDataStore store)
        {
            store.DiscoverTypes(Assembly.GetExecutingAssembly());
        }

        public Book[] GetAllBooks()
        {
            return Store.Select<Book>();
        }

        public Book[] GetBooksOfType(BookType type)
        {
            return Store.Select<Book>("BookType", type);
        }

        public Book GetBookById(int bookID)
        {
            return Store.Select<Book>(bookID);
        }

        public Author GetAuthorById(int id)
        {
            //get the author - this is the PK, so we don't need to qualify the field name
            var author = Store.Select<Author>(id);

            // fill in the books
            Store.FillReferences(author);

            return author;
        }

        public Author GetAuthorByName(string name)
        {
            //get the author
            var author = Store.Select<Author>("Name", name).FirstOrDefault();

            // fill in the books
            Store.FillReferences(author);

            return author;
        }

        public Author[] GetAuthors(int count, int offset)
        {
            return Store.Fetch<Author>(count, offset, "Name");
        }

        public void TruncateBooks()
        {
            Store.Delete<Book>();
        }

        public void Insert(Author author)
        {
            Store.Insert(author, true);
        }

        public void Update(Author author)
        {
            Store.Update(author);
        }

        public void TestCustomObjectCRUD()
        {
            // truncate the table for this test
            Store.Delete<TestTable>();

            var newObject = new CustomObject
            {
                ObjectName = "Object A",
                Identifier = Guid.NewGuid(),
                SomeIntProp = 12345
            };

            var testRow = new TestTable
            {
                CustomObject = newObject
            };

            Store.Insert(testRow);

            var existing = Store.Select<TestTable>().First();

            // make sure that the existing fields are what we inserted
            if (newObject.ObjectName != existing.CustomObject.ObjectName)
            {
                if (Debugger.IsAttached) Debugger.Break();
                Debug.WriteLine("** Custom object Insert failed on ObjectName! **");
            }
            if (newObject.SomeIntProp != existing.CustomObject.SomeIntProp)
            {
                if (Debugger.IsAttached) Debugger.Break();
                Debug.WriteLine("** Custom object Insert failed on SomeIntProp! **");
            }
            if (!newObject.Identifier.Equals(existing.CustomObject.Identifier))
            {
                if (Debugger.IsAttached) Debugger.Break();
                Debug.WriteLine("** Custom object Insert failed on Identifier! **");
            }

            existing.CustomObject.ObjectName = "New Name";

            Store.Update(existing);

            var secondPull = Store.Select<TestTable>().First();

            if (secondPull.CustomObject.ObjectName != existing.CustomObject.ObjectName)
            {
                if (Debugger.IsAttached) Debugger.Break();
                Debug.WriteLine("** Custom object Update failed on ObjectName! **");
            }

        }

        public void TestBinaryCRUD()
        {
            var rand = new Random();

            var small = new byte[200];
            var large = new byte[10000];

            rand.NextBytes(small);
            rand.NextBytes(large);

            // truncate the table for this test
            Store.Delete<TestTable>();

            var testRow = new TestTable
            {
                LongBinary = large,
                ShortBinary = small
            };

            Store.Insert(testRow);

            var existing = Store.Select<TestTable>().First();

            // make sure that the existing fields are what we inserted
            for (int i = 0; i < small.Length; i++)            
            {
                if (existing.ShortBinary[i] != small[i])
                {
                    if (Debugger.IsAttached) Debugger.Break();
                    Debug.WriteLine("** ShortBinary Insert failed **");
                    break;
                }
            }
            for (int i = 0; i < large.Length; i++)
            {
                if (existing.LongBinary[i] != large[i])
                {
                    if (Debugger.IsAttached) Debugger.Break();
                    Debug.WriteLine("** LongBinary Insert failed **");
                    break;
                }
            }

            rand.NextBytes(small);
            existing.ShortBinary = small;

            Store.Update(existing);

            var secondPull = Store.Select<TestTable>().First();

            for (int i = 0; i < small.Length; i++)
            {
                if (existing.ShortBinary[i] != small[i])
                {
                    if (Debugger.IsAttached) Debugger.Break();
                    Debug.WriteLine("** ShortBinary Insert failed **");
                    break;
                }
            }

        }

        public void TestEnumCRUD()
        {
            // truncate the table for this test
            Store.Delete<TestTable>();

            var testRow = new TestTable
            {
                 EnumField = TestEnum.ValueB
            };

            Store.Insert(testRow);

            var existing = Store.Select<TestTable>().First();
            if (testRow.EnumField != existing.EnumField)
            {
                if (Debugger.IsAttached) Debugger.Break();
                Debug.WriteLine("** EnumField Insert failed! **");
            }

            existing.EnumField = TestEnum.ValueC;
            Store.Update(existing);
            var secondPull = Store.Select<TestTable>().First();

            if (secondPull.EnumField != existing.EnumField)
            {
                if (Debugger.IsAttached) Debugger.Break();
                Debug.WriteLine("** EnumField Update failed! **");
            }
        }
    }
}
