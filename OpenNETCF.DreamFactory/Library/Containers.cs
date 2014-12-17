using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RestSharp;
using System.Net;
using System.Diagnostics;

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
            if (Session.Disconnected)
            {
                Session.Reconnect();
            }

            // this is non-intuitive based on the published API.
            var request = Session.GetSessionRequest("/rest/app", Method.GET);

            var response = Session.Client.Execute<ContainerDescriptor>(request);

            var check = DreamFactoryException.ValidateIRestResponse(response);
            if (check != null)
            {
                throw new DeserializationException(string.Format("Failed to retrieve Container descriptors: {0}", response.ErrorMessage), check);
            }

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

                case HttpStatusCode.Forbidden:
                case HttpStatusCode.Unauthorized:
                    if (Debugger.IsAttached) Debugger.Break();

                    Session.Disconnected = true;

                    throw DreamFactoryException.Parse(response);
                default:
                    var error = SimpleJson.DeserializeObject<ErrorDescriptorList>(response.Content);
                    // TODO: make a library-specific Exception class
                    throw new Exception(error.error[0].message);
            }
        }

        public Container GetContainer(string containerName)
        {
            if (Session.Disconnected)
            {
                Session.Reconnect();
            }

            // todo: add caching

            // The trailing slash here is *required*. Without it we'll get back a NotFound
            var request = Session.GetSessionRequest(string.Format("/rest/app/{0}/", containerName), Method.GET);

            var response = Session.Client.Execute<ContainerDescriptor>(request);

            var check = DreamFactoryException.ValidateIRestResponse(response);
            if (check != null)
            {
                throw new DeserializationException(string.Format("Failed to container descriptor for '{0}': {1}", containerName, response.ErrorMessage), check);
            }

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
                case HttpStatusCode.Forbidden:
                case HttpStatusCode.Unauthorized:
                    if (Debugger.IsAttached) Debugger.Break();

                    Session.Disconnected = true;

                    throw DreamFactoryException.Parse(response);
                default:
                    var error = SimpleJson.DeserializeObject<ErrorDescriptorList>(response.Content);
                    // TODO: make a library-specific Exception class
                    throw new Exception(error.error[0].message);
            }
        }

        public Container CreateContainer(string containerName)
        {
            if (Session.Disconnected)
            {
                Session.Reconnect();
            }

            var request = Session.GetSessionRequest(string.Format("/rest/app/{0}/", containerName), Method.POST);

            var response = Session.Client.Execute(request);

            switch (response.StatusCode)
            {
                case HttpStatusCode.Created:
                case HttpStatusCode.OK:
                    return GetContainer(containerName);
                case HttpStatusCode.Forbidden:
                case HttpStatusCode.Unauthorized:
                    if (Debugger.IsAttached) Debugger.Break();

                    Session.Disconnected = true;

                    throw DreamFactoryException.Parse(response);
                default:
                    var error = SimpleJson.DeserializeObject<ErrorDescriptorList>(response.Content);
                    // TODO: make a library-specific Exception class
                    throw new Exception(error.error[0].message);
            }
        }

        public void DeleteContainer(string containerName)
        {
            if (Session.Disconnected)
            {
                Session.Reconnect();
            }

            var request = Session.GetSessionRequest(string.Format("/rest/app/{0}/", containerName), Method.DELETE);

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
                    var error = SimpleJson.DeserializeObject<ErrorDescriptorList>(response.Content);
                    // TODO: make a library-specific Exception class
                    throw new Exception(error.error[0].message);
            }
        }
    }
}
