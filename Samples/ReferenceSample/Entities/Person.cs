using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenNETCF.ORM;

namespace ReferenceSample.Entities
{
    [Entity(KeyScheme.Identity)]
    public class Person
    {
        [Field(IsPrimaryKey=true)]
        public int PersonID { get; set; }
        [Field]
        public string FirstName { get; set; }
        [Field]
        public string LastName { get; set; }
    }
}
