using RestSharp.Serializers;
using System.Collections.Generic;

namespace OpenNETCF.DreamFactory
{
    [SerializeAs(Name = "table")]
    internal class TableDescriptor
    {
        public string name { get; set; }
        public string label { get; set; }
        public string plural { get; set; }
        public string primary_key { get; set; }
        public string name_field { get; set; }

        public List<FieldDescriptor> field { get; set; }
    }
}
