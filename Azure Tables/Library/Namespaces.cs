using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace OpenNETCF.Azure
{
    internal static class Namespaces
    {
        static Namespaces()
        {
            Atom = XNamespace.Get("http://www.w3.org/2005/Atom");
            DataServices = XNamespace.Get("http://schemas.microsoft.com/ado/2007/08/dataservices");
            DataServicesMeta = XNamespace.Get("http://schemas.microsoft.com/ado/2007/08/dataservices/metadata");
        }

        public static XNamespace Atom { get; private set; }
        public static XNamespace DataServices { get; private set; }
        public static XNamespace DataServicesMeta { get; private set; }
    }
}
