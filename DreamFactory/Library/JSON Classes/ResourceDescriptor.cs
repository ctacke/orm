using RestSharp.Serializers;
using System.Collections.Generic;

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
