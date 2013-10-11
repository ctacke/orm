using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenNETCF.ORM.DreamFactory.Integration.Test
{
    [Entity(KeyScheme = KeyScheme.Identity)]
    public class TestItem : IEquatable<TestItem>
    {
        public TestItem()
        {
            RecordDate = DateTime.Now;
        }

        public TestItem(string name)
            : this()
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

        [Field]
        public DateTime RecordDate { get; set; }

        public bool Equals(TestItem other)
        {
            return this.ID == other.ID;
        }
    }
}
