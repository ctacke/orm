using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace OpenNETCF.ORM.Xml
{
    public class XmlEntityInfo : EntityInfo
    {
        public XElement EntityNode { get; internal set; }
    }
}
