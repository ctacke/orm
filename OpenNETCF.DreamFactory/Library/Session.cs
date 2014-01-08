using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public bool Disconnected { get; internal set; }

        private SessionDescriptor SessionDescriptor { get; set; }

        public Data Data { get; private set; }
        public Applications Applications { get; private set; }

        public Session(string dspRootAddress, string application, string username, string password)
        {
            DSPRootAddress = dspRootAddress;
            ApplicationName = application;
            Username = username;
            Password = password;

            Disconnected = true;
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

            switch (response.StatusCode)
            {
                case HttpStatusCode.Created:
                    // successful session creation
                    Disconnected = false;
                    break;
                default:
                    var error = SimpleJson.DeserializeObject<ErrorDescriptorList>(response.Content);
                    if (error.error.Count > 0)
                    {
                        throw new Exception(error.error[0].message);
                    }
                    throw new Exception(response.StatusDescription);
            }

            SessionDescriptor = response.Data;

            // TODO: set some properties that might be of interest

            if (Data == null)
            {
                Data = new Data(this);
            }
            if (Applications == null)
            {
                Applications = new Applications(this);
            }
        }

        internal IRestRequest GetSessionRequest(string path, Method method)
        {
            if (Disconnected)
            {
                if (Debugger.IsAttached) Debugger.Break();

                // attempt to-re-establish the session (timeouts will end up here)
                Initialize();
            }

            var request = new RestRequest(path, method)
                .AddHeader("X-DreamFactory-Application-Name", this.ApplicationName)
                .AddHeader("X-DreamFactory-Session-Token", this.ID);

            request.RequestFormat = DataFormat.Json;

            return request;
        }
    }
}
