using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RestSharp;
using System.Net;
using System.Diagnostics;

namespace OpenNETCF.DreamFactory
{
    public sealed class Applications
    {
        private Session Session { get; set; }

        internal Applications(Session session)
        {
            Session = session;
        }

        public Application[] Get()
        {
            return Get(null);
        }

        public Application Find(string apiName)
        {
            return Get(string.Format("api_name = '{0}'", apiName)).FirstOrDefault();
        }

        public Application[] Get(string filter)
        {
            if (Session.Disconnected)
            {
                Session.Reconnect();
            }

            var request = Session.GetSessionRequest("/rest/system/app", Method.GET);

            if (!filter.IsNullOrEmpty())
            {
                request.Parameters.Add(new Parameter()
                {
                    Name = "filter",
                    Value = filter,
                    Type = ParameterType.GetOrPost
                });
            }

            var response = Session.Client.Execute<ApplicationDescriptorList>(request);

            var check = DreamFactoryException.ValidateIRestResponse(response);
            if (check != null)
            {
                throw new DeserializationException(string.Format("Failed to retrieve Application descriptors: {0}", response.ErrorMessage), check);
            }

            switch (response.StatusCode)
            {
                case System.Net.HttpStatusCode.OK:
                    var apps = new List<Application>();

                    var data = response.Data;

                    if (data != null)
                    {
                        foreach (var app in data.record)
                        {
                            apps.Add(new Application(app));
                        }
                    }

                    return apps.ToArray();

                case HttpStatusCode.Forbidden:
                case HttpStatusCode.Unauthorized:
                    if (Debugger.IsAttached) Debugger.Break();

                    Session.Disconnected = true;

                    throw DreamFactoryException.Parse(response);
                default:
                    throw DreamFactoryException.Parse(response);
            }
        }

        public Application Create(string name)
        {
            if (Session.Disconnected)
            {
                Session.Reconnect();
            }

            var descriptor = new ApplicationDescriptor()
            {
                name = name,
                api_name = name
            };

            var request = Session.GetSessionRequest("/rest/system/app", Method.POST);

            request.JsonSerializer.ContentType = "application/json; charset=utf-8";
            request.JsonSerializer.Options = new SerializerOptions()
            {
                SkipNullProperties = true
            };
            request.AddBody(descriptor);

            var response = Session.Client.Execute(request);

            switch (response.StatusCode)
            {
                case HttpStatusCode.Created:
                case HttpStatusCode.OK:
                    return Find(name);
                case HttpStatusCode.Forbidden:
                case HttpStatusCode.Unauthorized:
                    if (Debugger.IsAttached) Debugger.Break();

                    Session.Disconnected = true;

                    throw DreamFactoryException.Parse(response);
                default:
                    throw DreamFactoryException.Parse(response);
            }
        }

        public Application Update(Application application)
        {
            if (Session.Disconnected)
            {
                Session.Reconnect();
            }

            var request = Session.GetSessionRequest("/rest/system/app", Method.PUT);

            request.JsonSerializer.ContentType = "application/json; charset=utf-8";
            request.JsonSerializer.Options = new SerializerOptions()
            {
                SkipNullProperties = true
            };
            request.AddBody(application.AsApplicationDescriptor());

            var response = Session.Client.Execute(request);

            switch (response.StatusCode)
            {
                case System.Net.HttpStatusCode.OK:
                    return Find(application.APIName);
                case HttpStatusCode.Forbidden:
                case HttpStatusCode.Unauthorized:
                    if (Debugger.IsAttached) Debugger.Break();

                    Session.Disconnected = true;

                    throw DreamFactoryException.Parse(response);
                default:
                    throw DreamFactoryException.Parse(response);
            }
        }

        public void Delete(Application application)
        {
            Delete(application.ID);
        }

        public void Delete(string applicationID)
        {
            if (Session.Disconnected)
            {
                Session.Reconnect();
            }

            var request = Session.GetSessionRequest(string.Format("/rest/system/app/{0}", applicationID), Method.DELETE);

            var response = Session.Client.Execute(request);

            switch (response.StatusCode)
            {
                case System.Net.HttpStatusCode.OK:
                    return;
                case HttpStatusCode.Forbidden:
                case HttpStatusCode.Unauthorized:
                    if (Debugger.IsAttached) Debugger.Break();

                    Session.Disconnected = true;

                    throw DreamFactoryException.Parse(response);
                default:
                    throw DreamFactoryException.Parse(response);
            }
        }
    }
}
