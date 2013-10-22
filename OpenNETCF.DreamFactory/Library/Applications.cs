using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RestSharp;

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

                default:
                    var error = SimpleJson.DeserializeObject<ErrorDescriptorList>(response.Content);
                    // TODO: make a library-specific Exception class
                    throw new Exception(error.error[0].message);
            }
        }

        public Application Create(string name)
        {
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
                case System.Net.HttpStatusCode.Created:
                    return Find(name);
                default:
                    var error = SimpleJson.DeserializeObject<ErrorDescriptorList>(response.Content);
                    // TODO: make a library-specific Exception class
                    throw new Exception(error.error[0].message);
            }
        }

        public Application Update(Application application)
        {
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
                default:
                    var error = SimpleJson.DeserializeObject<ErrorDescriptorList>(response.Content);
                    // TODO: make a library-specific Exception class
                    throw new Exception(error.error[0].message);
            }
        }

        public void Delete(Application application)
        {
            Delete(application.ID);
        }

        public void Delete(string applicationID)
        {
            var request = Session.GetSessionRequest(string.Format("/rest/system/app/{0}", applicationID), Method.DELETE);

            var response = Session.Client.Execute(request);

            switch (response.StatusCode)
            {
                case System.Net.HttpStatusCode.OK:
                    return;
                default:
                    var error = SimpleJson.DeserializeObject<ErrorDescriptorList>(response.Content);
                    // TODO: make a library-specific Exception class
                    throw new Exception(error.error[0].message);
            }
        }
    }
}
