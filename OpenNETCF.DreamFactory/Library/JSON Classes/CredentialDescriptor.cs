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
    internal class CredentialDescriptor
    {
        public string email { get; set; }
        public string password { get; set; }
    }
}
