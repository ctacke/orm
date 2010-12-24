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
    }
}
