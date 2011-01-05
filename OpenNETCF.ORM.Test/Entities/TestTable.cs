using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace OpenNETCF.ORM.Test.Entities
{
    public enum TestEnum
    {
        ValueA,
        ValueB,
        ValueC
    }

    [Entity(KeyScheme.Identity)]
    public class TestTable
    {
        [Field(IsPrimaryKey = true)]
        public int TestID { get; set; }

        [Field(Length=200)]
        public byte[] ShortBinary { get; set; }

        [Field(Length = 20000)]
        public byte[] LongBinary { get; set; }

        [Field]
        public TestEnum EnumField { get; set; }

        [Field]
        public CustomObject CustomObject { get; set; }

        // ORM-required serialization routines for any Object Field
        public byte[] Serialize(string fieldName)
        {
            if (fieldName == "CustomObject")
            {
                // This will always be true in this case since CustomObject is our only
                // Object Field.  The "if" block could be omitted, but for sample 
                // clarity I'm keeping it
                if (this.CustomObject == null) return null;
                return this.CustomObject.AsByteArray();
            }

            throw new NotSupportedException();
        }

        public object Deserialize(string fieldName, byte[] data)
        {
            if (fieldName == "CustomObject")
            {
                // This will always be true in this case since CustomObject is our only
                // Object Field.  The "if" block could be omitted, but for sample 
                // clarity I'm keeping it
                return new CustomObject(data);
            }

            throw new NotSupportedException();
        }
    }
}
