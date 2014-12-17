using OpenNETCF.ORM;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace ImageSample
{
    [Entity]
    class DataClass
    {
        [Field]
        public string Name { get; set; }
        [Field]
        public Image Picture { get; set; }

        public byte[] Serialize(string fieldName)
        {
            if (fieldName == "Picture")
            {
                using (var ms = new MemoryStream())
                {
                    this.Picture.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                    return ms.ToArray();
                }
            }

            return null;
        }

        public object Deserialize(string fieldName, byte[] data)
        {
            if (fieldName == "Picture")
            {
                using (var ms = new MemoryStream(data))
                {
                    return Image.FromStream(ms);
                }
            }

            return null;
        }
    }
}
