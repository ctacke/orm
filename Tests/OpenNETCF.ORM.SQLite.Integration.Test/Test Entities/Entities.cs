using OpenNETCF.ORM.SQLite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace OpenNETCF.ORM.SQLite.Integration.Test
{
    [Entity(KeyScheme = KeyScheme.Identity)]
    public class BigID
    {
        public BigID()
        {
        }

        public BigID(string data)
        {
            Data = data;
        }

        [Field(IsPrimaryKey = true)]
        public long ID { get; set; }

        [Field]
        public string Data { get; set; }
    }

    [Entity(KeyScheme = KeyScheme.Identity)]
    public class Author
    {
        [Field(IsPrimaryKey = true)]
        public int ID { get; set; }

        [Field]
        public string Name { get; set; }

        [Reference(typeof(Book), "AuthorID")]
        Book[] Books { get; set; }
    }

    [Entity(KeyScheme = KeyScheme.Identity)]
    public class Book
    {
        [Field(IsPrimaryKey = true)]
        public int ID { get; set; }

        [Field]
        public int AuthorID { get; set; }

        [Field]
        public string Title { get; set; }
    }


    [Entity(KeyScheme=KeyScheme.Identity)]
    public class TestItem : IEquatable<TestItem>
    {
        public TestItem()
        {
        }

        public TestItem(string name)
        {
            Name = name;
        }

        [Field(IsPrimaryKey = true)]
        public int ID { get; set; }

        [Field(SearchOrder=FieldSearchOrder.Ascending)]
        public string Name { get; set; }

        [Field]
        public Guid? UUID { get; set; }

        [Field]
        public int ITest { get; set; }

        [Field]
        public string Address { get; set; }

        [Field]
        public float FTest { get; set; }

        [Field]
        public double DBTest { get; set; }

        [Field(Scale = 2)]
        public decimal DETest { get; set; }

        [Field]
        public TimeSpan TS { get; set; }

        public bool Equals(TestItem other)
        {
            return this.ID == other.ID;
        }
    }

    [Entity(KeyScheme = KeyScheme.Identity)]
    public class Position : IEquatable<Position>
    {
        public Position()
        {
        }

        [Field(IsPrimaryKey = true)]
        public int positionId { get; set; }

        [Field]
        public string Description { get; set; }

        [Reference(typeof(Location), "positionId")]
        public Location[] locations { get; set; }

        public bool Equals(Position other)
        {
            return this.positionId == other.positionId;
        }
    }

    [Entity(KeyScheme = KeyScheme.Identity)]
    public class Location : IEquatable<Location>
    {
        public Location()
        {
            // this is required for cascading inserts to work
            locationId = -1;
        }

        [Field(IsPrimaryKey = true)]
        public int locationId { get; set; }

        [Field]
        public string Description { get; set; }

        [Field]
        public int positionId { get; set; }

        public bool Equals(Location other)
        {
            return this.locationId == other.locationId;
        }
    }
}
