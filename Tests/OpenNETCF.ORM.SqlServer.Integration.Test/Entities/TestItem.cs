using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace OpenNETCF.ORM.Tests
{
    [Entity(KeyScheme = KeyScheme.Identity)]
    public class BinaryItem
    {
        [Field(IsPrimaryKey = true)]
        public int ID { get; set; }

        [Field]
        public byte[] Data { get; set; }
    }

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

    [Entity(KeyScheme.Identity)]
    public class TestItemD : IEquatable<TestItemD>
    {
        private static TestItemD ORM_CreateProxy(FieldAttributeCollection fields, IDataReader results)
        {
            var item = new TestItemD();

            foreach (var field in fields)
            {
                var value = results[field.Ordinal];

                switch (field.FieldName)
                {
                    case "ID":
                        item.ID = value == DBNull.Value ? 0 : (int)value;
                        break;
                    case "Name":
                        item.Name = value == DBNull.Value ? null : (string)value;
                        break;
                    case "UUID":
                        item.UUID = value == DBNull.Value ? null : (Guid?)value;
                        break;
                    case "ITest":
                        item.ITest = value == DBNull.Value ? 0 : (int)value;
                        break;
                    case "Address":
                        item.Address = value == DBNull.Value ? null : (string)value;
                        break;
                    case "FTest":
                        item.FTest = value == DBNull.Value ? 0 : (float)value;
                        break;
                    case "DBTest":
                        item.DBTest = value == DBNull.Value ? 0 : (double)value;
                        break;
                    case "DETest":
                        item.DETest = value == DBNull.Value ? 0 : (decimal)value;
                        break;
                    case "TS":
                        item.TS = value == DBNull.Value ? TimeSpan.MinValue : new TimeSpan((long)value);
                        break;
                    case "BigString":
                        item.BigString = value == DBNull.Value ? null : (string)value;
                        break;
                }
            }

            return item;
        }

        public static explicit operator TestItemD(TestItem item)
        {
            return new TestItemD()
            {
                Address = item.Address,
                BigString = item.BigString,
                DBTest = item.DBTest,
                DETest = item.DETest,
                FTest = item.FTest,
                ITest = item.ITest,
                Name = item.Name,
                TS = item.TS,
                UUID = item.UUID
            };
        }

        public TestItemD()
        {
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

        public bool Equals(TestItemD other)
        {
            return this.ID == other.ID;
        }
    }
}
