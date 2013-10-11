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
    [SerializeAs(Name = "field")]
    internal class FieldDescriptor
    {
        private string m_type;

        public string name { get; set; }
        public string label { get; set; }
        public string type { get; set; }
        public string db_type { get; set; }

        public int? length { get; set; }
        public int? precision { get; set; }
        public int? scale { get; set; }

//        public int default { get; set; }

        public bool? required { get; set; }
        public bool? allow_null { get; set; }
        public bool? fixed_length { get; set; }
        public bool? supports_multibyte { get; set; }
        public bool? auto_increment { get; set; }
        public bool? is_primary_key { get; set; }
        public bool? is_foreign_key { get; set; }

        public string ref_table { get; set; }
        public string ref_fields { get; set; }
        public string validation { get; set; }
    }
}
