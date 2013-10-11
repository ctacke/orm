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
