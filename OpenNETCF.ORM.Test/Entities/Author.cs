﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace OpenNETCF.ORM.Test.Entities
{
    [Entity(KeyScheme.Identity)]
    public class Author
    {
        [Field(IsPrimaryKey = true)]
        public int AuthorID { get; set; }

        [Reference("AuthorID", Autofill=true)]
        public ReferenceCollection<Book> Books { get; set; }

        [Field(SearchOrder = FieldSearchOrder.Ascending)]
        public string Name { get; set; }
    }
}
