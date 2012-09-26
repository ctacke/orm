using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenNETCF.ORM.SqlCE.Integration.Test
{
    [Entity(KeyScheme = KeyScheme.Identity)]
    public class LateAddItem
    {
        [Field(IsPrimaryKey = true)]
        public int ID { get; set; }

        [Field]
        public string Name { get; set; }
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

        [Field]
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

        [Field(Length = int.MaxValue)]
        public string BigString { get; set; }

        public bool Equals(TestItem other)
        {
            return this.ID == other.ID;
        }
    }
}
