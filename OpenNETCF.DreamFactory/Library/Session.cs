using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using OpenNETCF.Web;
using RestSharp;

namespace OpenNETCF.DreamFactory
{
    public sealed class Session
    {
        internal RestClient Client { get; private set; }
        public string DSPRootAddress { get; private set; }
        public string Username { get; private set; }
        public string ApplicationName { get; set; }
        private string Password { get; set; }

        private SessionDescriptor SessionDescriptor { get; set; }

        public Data Data { get; private set; }

        public Session(string dspRootAddress, string application, string username, string password)
        {
            DSPRootAddress = dspRootAddress;
            ApplicationName = application;
            Username = username;
            Password = password;
        }

        internal string ID
        {
            get { return SessionDescriptor.session_id; }
        }

        public void Initialize()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3;

            Client = new RestClient(DSPRootAddress);
            RestRequest request = new RestRequest("/rest/user/session", Method.POST);
            request.AddHeader("X-DreamFactory-Application-Name", "ORM");
            request.RequestFormat = DataFormat.Json;
            var creds = new CredentialDescriptor
            {
                email = Username,
                password = Password
            };
            request.AddBody(creds);

            var response = Client.Execute<SessionDescriptor>(request);
            
            SessionDescriptor = response.Data;

            // TODO: set some properties that might be of interest

            Data = new Data(this);
        }
    }
}
