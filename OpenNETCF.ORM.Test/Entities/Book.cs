using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace OpenNETCF.ORM.Test.Entities
{
    public enum BookType
    {
        Fiction,
        NonFiction
    }

    [Entity(KeyScheme.Identity)]
    public class Book
    {
        public Book()
        {
            // this is required for cascading inserts to work
            BookID = -1;
        }

        [Field(IsPrimaryKey=true)]
        public int BookID { get; set; }

        [Field]
        public int AuthorID { get; set; }

        [Reference(typeof(Author), "AuthorID", ReferenceType = ReferenceType.ManyToOne)]
        public Author Author { get; set; }

        [Field]
        public string Title { get; set; }
        

        [Field(SearchOrder=FieldSearchOrder.Ascending)]
        public BookType BookType { get; set; }

        [Field(IsRowVersion=true)]
        public long RowVersion { get; set; }
    }
}
