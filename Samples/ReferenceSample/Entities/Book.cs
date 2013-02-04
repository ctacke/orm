using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenNETCF.ORM;

namespace ReferenceSample.Entities
{
    [Entity(KeyScheme.Identity)]
    public class Book
    {
        [Field(IsPrimaryKey=true)]
        public int BookID { get; set; }

        [Field(DefaultValue="[untitled]")]
        public string Title { get; set; }

        [Reference(typeof(Person), "PersonID")]
        public Person[] Authors { get; set; }

        [Reference(typeof(Person), "PersonID")]
        public Person[] Illustrators { get; set; }

        [Field(DefaultType = DefaultType.CurrentDateTime)]
        public DateTime CreateDate { get; set; }
    }
}
