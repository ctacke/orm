using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using OpenNETCF.Web;
using RestSharp;
using RestSharp.Serializers;

namespace OpenNETCF.DreamFactory
{
    [SerializeAs(Name = "resource")]
    internal class ResourceDescriptor
    {
        public string name { get; set; }
        public string label { get; set; }
        public string plural { get; set; }
    }

    internal class ResourceDescriptorList
    {
        public List<ResourceDescriptor> resource { get; set; }
    }
}
