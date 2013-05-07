using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace OpenNETCF.ORM.Test.Entities
{
    public class CustomObject
    {
        public string ObjectName { get; set; }
        public Guid Identifier { get; set; }
        public int SomeIntProp { get; set; }

        public CustomObject()
        {
        }

        public CustomObject(byte[] data)
        {
            // deserialization ctor
            int offset = 0;

            // get the name length
            var nameLength = BitConverter.ToInt32(data, offset);

            // get the name bytes
            offset += 4; // past the length
            this.ObjectName = Encoding.ASCII.GetString(data, offset, nameLength);

            // get the GUID
            offset += nameLength;
            byte[] guidData = new byte[16];
            // we must copy the data since Guid doesn't have a ctor that allows us to specify an offset
            Buffer.BlockCopy(data, offset, guidData, 0, guidData.Length);
            this.Identifier = new Guid(guidData);

            // get the int property
            offset += guidData.Length;
            this.SomeIntProp = BitConverter.ToInt32(data, offset);
        }

        public byte[] AsByteArray()
        {
            List<byte> buffer = new List<byte>();

            byte[] nameData = Encoding.ASCII.GetBytes(this.ObjectName);

            // store the name length
            buffer.AddRange(BitConverter.GetBytes(nameData.Length));

            // store the name data
            buffer.AddRange(nameData);

            // store the GUID
            buffer.AddRange(this.Identifier.ToByteArray());

            // store the IntProp
            buffer.AddRange(BitConverter.GetBytes(this.SomeIntProp));

            return buffer.ToArray();
        }
    }
}
