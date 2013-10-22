using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RestSharp;

namespace OpenNETCF.DreamFactory
{
    public sealed class FileSystem
    {
        private Session Session { get; set; }

        internal FileSystem(Session session)
        {
            Session = session;
        }

        public Container[] GetContainers()
        {
            // this is non-intuitive based on the published API.
            var request = Session.GetSessionRequest("/rest/app", Method.GET);

            var response = Session.Client.Execute<ContainerDescriptor>(request);

            switch (response.StatusCode)
            {
                case System.Net.HttpStatusCode.OK:
                    var containers = new List<Container>();

                    var container = response.Data;

                    if (container != null)
                    {
                        foreach (var app in container.folder)
                        {
                            containers.Add(new Container(app));
                        }
                    }

                    return containers.ToArray();

                default:
                    var error = SimpleJson.DeserializeObject<ErrorDescriptorList>(response.Content);
                    // TODO: make a library-specific Exception class
                    throw new Exception(error.error[0].message);
            }
        }

        public Container GetContainer(string containerName)
        {
            // todo: add caching

            // The trailing slash here is *required*. Without it we'll get back a NotFound
            var request = Session.GetSessionRequest(string.Format("/rest/app/{0}/", containerName), Method.GET);

            var response = Session.Client.Execute<ContainerDescriptor>(request);

            switch (response.StatusCode)
            {
                case System.Net.HttpStatusCode.OK:
                    var container = response.Data;

                    if (container != null)
                    {
                        return new Container(container);
                    }

                    return null;
                case System.Net.HttpStatusCode.NotFound:
                    return null;
                default:
                    var error = SimpleJson.DeserializeObject<ErrorDescriptorList>(response.Content);
                    // TODO: make a library-specific Exception class
                    throw new Exception(error.error[0].message);
            }
        }

        public Container CreateContainer(string containerName)
        {
            var request = Session.GetSessionRequest(string.Format("/rest/app/{0}/", containerName), Method.POST);

            var response = Session.Client.Execute(request);

            switch (response.StatusCode)
            {
                case System.Net.HttpStatusCode.Created:
                    return GetContainer(containerName);
                default:
                    var error = SimpleJson.DeserializeObject<ErrorDescriptorList>(response.Content);
                    // TODO: make a library-specific Exception class
                    throw new Exception(error.error[0].message);
            }
        }

        public void DeleteContainer(string containerName)
        {
            var request = Session.GetSessionRequest(string.Format("/rest/app/{0}/", containerName), Method.DELETE);

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
