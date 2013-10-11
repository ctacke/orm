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
    internal class SessionDescriptor
    {
        public SessionDescriptor()
        {
        }

        public string id { get; set; }
        public string email { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string display_name { get; set; }
        public bool is_sys_admin { get; set; }
        public string last_login_date { get; set; }
        public string ticket { get; set; }
        public long ticket_expiry { get; set; }
        public string session_id { get; set; }
        public List<string> app_groups { get; set; }
        public List<ApplicationDescriptor> no_group_apps { get; set; }
    }
}
