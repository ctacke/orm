﻿using System;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace OpenNETCF.ORM
{
    public enum TestEnum
    {
        ValueA,
        ValueB,
        ValueC
    }

    [Entity(KeyScheme = KeyScheme.Identity)]
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

        [Field(SearchOrder = FieldSearchOrder.Ascending)]
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

        [Field(DefaultValue = "Default")]
        public string DefString { get; set; }

        [Field(DefaultType = DefaultType.CurrentDateTime)]
        public DateTime CreateDate { get; set; }

        [Field]
        public TestEnum EnumField { get; set; }

        [Field(DataType = DbType.String)]
        public TestEnum EnumFieldAsString { get; set; }

        public bool Equals(TestItem other)
        {
            return this.ID == other.ID;
        }
    }

    [Entity(KeyScheme = KeyScheme.Identity)]
    public class Author
    {
        [Field(IsPrimaryKey = true)]
        public int ID { get; set; }

        [Field]
        public string Name { get; set; }

        [Reference(typeof(Book), "AuthorID")]
        public Book[] Books { get; set; }
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
}
