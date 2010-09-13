using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using OpenNETCF.ORM.Test.Entities;

namespace OpenNETCF.ORM.Test
{
    public class XmlStoreTest : ITestClass
    {
        public void Initialize()
        {
            //XmlDataStore store = new XmlDataStore("\\data.xml");

            //if (store.StoreExists)
            //{
            //    store.DeleteStore();
            //}
            //store.DiscoverTypes(Assembly.GetExecutingAssembly());
            //store.CreateStore();

            //Author a = new Author
            //{
            //    AuthorID = 1,
            //    Name = "John Doe"
            //};

            //store.Insert(a);
            //var authors = store.Select<Author>();

            throw new NotImplementedException();
        }

        public OpenNETCF.ORM.Test.Entities.Book[] GetAllBooks()
        {
            throw new NotImplementedException();
        }

        public OpenNETCF.ORM.Test.Entities.Book[] GetBooksOfType(OpenNETCF.ORM.Test.Entities.BookType type)
        {
            throw new NotImplementedException();
        }

        public OpenNETCF.ORM.Test.Entities.Book GetBookById(int bookID)
        {
            throw new NotImplementedException();
        }

        public OpenNETCF.ORM.Test.Entities.Author GetAuthorByName(string name)
        {
            throw new NotImplementedException();
        }

        public OpenNETCF.ORM.Test.Entities.Author GetAuthorById(int id)
        {
            throw new NotImplementedException();
        }

        public OpenNETCF.ORM.Test.Entities.Author[] GetAuthors(int count, int offset)
        {
            throw new NotImplementedException();
        }

        public int GetAuthorCount()
        {
            throw new NotImplementedException();
        }

        public int GetBookCount()
        {
            throw new NotImplementedException();
        }

        public void TruncateBooks()
        {
            throw new NotImplementedException();
        }

        public void Insert(Author author)
        {
            throw new NotImplementedException();
        }

        #region ITestClass Members


        public void Update(Author author)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
