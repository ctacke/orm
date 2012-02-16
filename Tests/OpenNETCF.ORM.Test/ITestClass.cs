using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using OpenNETCF.ORM.Test.Entities;

namespace OpenNETCF.ORM.Test
{
    public interface ITestClass
    {
        void Initialize();
        Book[] GetAllBooks();
        Book[] GetBooksOfType(BookType type);
        Book GetBookById(int bookID);

        Author GetAuthorByName(string name);
        Author GetAuthorById(int id);

        Author[] GetAuthors(int count, int offset);

        int GetAuthorCount();
        int GetBookCount();

        void Insert(Author author);
        void Update(Author author);
    }
}
