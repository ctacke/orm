using RestSharp;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Security;

namespace OpenNETCF.DreamFactory
{
    public sealed class Session
    {
        internal RestClient Client { get; private set; }
        public string DSPRootAddress { get; private set; }
        public string Username { get; private set; }
        public string ApplicationName { get; set; }
        private string Password { get; set; }

        public bool EnableCompressedResponseData { get; set; }

        public bool Disconnected { get; internal set; }

        private SessionDescriptor SessionDescriptor { get; set; }
        private SystemConfigDescriptor ConfigDescriptor { get; set; }

        public Data Data { get; private set; }
        public Applications Applications { get; private set; }

        public Version ServerVersion { get; private set; }

        public Session(string dspRootAddress, string application, string username, string password)
            : this(dspRootAddress, application, username, password, null, true)
        {
        }

        public Session(
            string dspRootAddress, 
            string application, 
            string username, 
            string password, 
            RemoteCertificateValidationCallback certificateCallback,
            bool enableCompressedResponseData)
        {
            DSPRootAddress = dspRootAddress;
            ApplicationName = application;
            Username = username;
            Password = password;
            EnableCompressedResponseData = enableCompressedResponseData;

            Disconnected = true;

            if (certificateCallback != null)
            {
                ServicePointManager.ServerCertificateValidationCallback = certificateCallback;
            }
        }

        public void Reconnect()
        {
            if (!this.Disconnected) return;

            Initialize();
        }

        internal string ID
        {
            get { return SessionDescriptor.session_id; }
        }

        public void Initialize()
        {
//            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3;

            Client = new RestClient(DSPRootAddress);
            var request = new RestRequest("/rest/user/session", Method.POST);
            request.AddHeader("X-DreamFactory-Application-Name", ApplicationName);
            request.RequestFormat = DataFormat.Json;
            var creds = new CredentialDescriptor
            {
                email = Username,
                password = Password
            };
            request.AddBody(creds);

            if (EnableCompressedResponseData)
            {
                request.AcceptDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip | DecompressionMethods.None;
            }
            else
            {
                request.AcceptDecompression = DecompressionMethods.None;
            }

            var response = Client.Execute<SessionDescriptor>(request);

            var check = DreamFactoryException.ValidateIRestResponse(response);
            if (check != null)
            {
                throw new DeserializationException(string.Format("Failed to deserialize the Session descriptor: {0}", check.Message), check);
            }

            switch (response.StatusCode)
            {
                case HttpStatusCode.Created:
                case HttpStatusCode.OK:
                    // successful session opened
                    Disconnected = false;
                    break;
                default:
                    throw DreamFactoryException.Parse(response);
            }

            SessionDescriptor = response.Data;

            // TODO: set some properties that might be of interest

            // get the server version
            ConfigDescriptor = GetSystemConfig();
            ServerVersion = new Version(ConfigDescriptor.dsp_version);

            if (Data == null)
            {
                Data = new Data(this);
            }
            if (Applications == null)
            {
                Applications = new Applications(this);
            }
        }

        private SystemConfigDescriptor GetSystemConfig()
        {
            var request = GetSessionRequest("/rest/system/config", Method.GET, false);
            var response = Client.Execute<SystemConfigDescriptor>(request);

            var check = DreamFactoryException.ValidateIRestResponse(response);
            if (check != null)
            {
                throw new DeserializationException(string.Format("Failed to deserialize the Session descriptor: {0}", response.ErrorMessage), check);
            }

            switch (response.StatusCode)
            {
                case HttpStatusCode.Created:
                case HttpStatusCode.OK:
                    return response.Data;
                default:
                    throw DreamFactoryException.Parse(response);
            }
        }

        private DateTime? m_lastRequest;

        internal IRestRequest GetSessionRequest(string path, Method method)
        {
            return GetSessionRequest(path, method, true);
        }

        internal IRestRequest GetSessionRequest(string path, Method method, bool checkConnection)
        {
            if (Disconnected)
            {
                if (Debugger.IsAttached) Debugger.Break();

                // attempt to-re-establish the session (timeouts will end up here)
                Initialize();
            }

            if (checkConnection)
            {
                // how long has it been since our last request?  If it's been a long time, verify we're still connected before returning
                var now = DateTime.Now;
                if (m_lastRequest.HasValue)
                {
                    if ((now < m_lastRequest.Value) // host time changed
                        || ((now - m_lastRequest.Value).TotalSeconds) > 60)
                    {
                        if (!TestConnection())
                        {
                            Reconnect();
                        }
                    }
                }
                m_lastRequest = now;
            }

            var request = new RestRequest(path, method)
                .AddHeader("X-DreamFactory-Application-Name", this.ApplicationName)
                .AddHeader("X-DreamFactory-Session-Token", this.ID);

            request.RequestFormat = DataFormat.Json;

            if (EnableCompressedResponseData)
            {
                request.AcceptDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip | DecompressionMethods.None;
            }
            else
            {
                request.AcceptDecompression = DecompressionMethods.None;
            }

            return request;
        }

        public bool TestConnection()
        {
            try
            {
                var cfg = GetSystemConfig();
                Disconnected = false;
            }
            catch (DreamFactoryException)
            {
                Disconnected = true;
            }

            return !Disconnected;
        }
    }
}
