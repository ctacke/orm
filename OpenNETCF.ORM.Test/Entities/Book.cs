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

    [Entity]
    public class Book
    {
        [Field(IsIdentity=true, IsPrimaryKey=true)]
        public int BookID { get; set; }

        [Field]
        public int AuthorID { get; set; }

        [Field]
        public string Title { get; set; }

        [Field(SearchOrder=FieldSearchOrder.Ascending)]
        public BookType BookType { get; set; }
    }
}
