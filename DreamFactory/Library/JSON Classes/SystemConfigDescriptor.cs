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
    [SerializeAs(Name = "config")]
    internal class SystemConfigDescriptor
    {
        public bool allow_admin_remote_logins { get; set; }
        public bool allow_guest_user { get; set; }
        public bool allow_open_registration { get; set; }
        public bool allow_remote_logins { get; set; }
//  "allowed_hosts": [],
//  "created_by_id": null,
//  "created_date": null,
//  "custom_settings": [],

        public string db_version { get; set; }
        public string dsp_version { get; set; }
        public string editable_profile_fields { get; set; }
        public int guest_role_id { get; set; }
        public int id { get; set; }
        public string install_name { get; set; }
        public int install_type { get; set; }

//  "invite_email_service_id": null,
//  "invite_email_template_id": null,

        public bool is_guest { get; set; }
        public bool is_hosted { get; set; }
        public bool is_private { get; set; }
        public int last_modified_by_id { get; set; }
        public DateTime last_modified_date { get; set; }
        public string latest_version { get; set; }

  //"open_reg_email_service_id": null,
  //"open_reg_email_template_id": null,
  //"open_reg_role_id": null,
  //"password_email_service_id": null,
  //"password_email_template_id": null,
  //"remote_login_providers": null,
  //"restricted_verbs": [],

        public string server_os { get; set; }
        public bool upgrade_available { get; set; }
    }
}
