using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace OpenNETCF.ORM.Test.Entities
{
    [Entity]
    public class Author
    {
        [Field(IsIdentity = true, IsPrimaryKey = true)]
        public int AuthorID { get; set; }

        [Reference(typeof(Book), "AuthorID", Autofill=false)]
        public Book[] Books { get; set; }

        [Field(SearchOrder = FieldSearchOrder.Ascending)]
        public string Name { get; set; }
    }
}
