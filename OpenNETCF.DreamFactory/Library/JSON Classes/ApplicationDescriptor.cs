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
    internal class ApplicationDescriptor
    {
        public ApplicationDescriptor()
        {
        }

        public string id { get; set; }
        public string api_name { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string url { get; set; }
        public bool is_url_external { get; set; }
        public bool requires_fullscreen { get; set; }
        public bool allow_fullscreen_toggle { get; set; }
        public string toggle_location { get; set; }
        public bool is_default { get; set; }
    }
}
