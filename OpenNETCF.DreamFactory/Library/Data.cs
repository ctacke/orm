using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using OpenNETCF.Web;
using RestSharp;
using System.Diagnostics;

namespace OpenNETCF.DreamFactory
{
    public sealed class Data
    {
        private Session Session { get; set; }

        internal Data(Session session)
        {
            Session = session;
        }

        public Table GetTable(string tableName)
        {
            var request = new RestRequest(string.Format("/rest/schema/{0}", tableName, Method.GET));
            request.AddHeader("X-DreamFactory-Application-Name", Session.ApplicationName);
            request.AddHeader("X-DreamFactory-Session-Token", Session.ID);

            request.RequestFormat = DataFormat.Json;

            var response = Session.Client.Execute<ResourceDescriptor>(request);

            switch(response.StatusCode)
            {
                case HttpStatusCode.OK:
                    return new Table(Session, response.Data);
                default:
                    var error = SimpleJson.DeserializeObject<ErrorDescriptorList>(response.Content);
                    if (error.error.Count > 0)
                    {
                        throw new Exception(error.error[0].message);
                    }

                    throw new Exception();
            }
        }

        public Table[] GetTables()
        {
            var request = new RestRequest("/rest/db", Method.GET);
            request.AddHeader("X-DreamFactory-Application-Name", Session.ApplicationName);
            request.AddHeader("X-DreamFactory-Session-Token", Session.ID);

            request.RequestFormat = DataFormat.Json;

            var response = Session.Client.Execute<ResourceDescriptorList>(request);

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    var list = new List<Table>();

                    foreach (var resource in response.Data.resource)
                    {
                        var t = new Table(Session, resource);
                        list.Add(t);
                    }

                    return list.ToArray();

                default:
                    var error = SimpleJson.DeserializeObject<ErrorDescriptorList>(response.Content);
                    if (error.error.Count > 0)
                    {
                        throw new Exception(error.error[0].message);
                    }

                    throw new Exception();
            }
        }

        public Table CreateTable(string tableName, string label, params Field[] fields)
        {
            var l = new List<Field>();
            l.AddRange(fields);
            return CreateTable(tableName, label, l);
        }

        public Table CreateTable(string tableName, string label, IEnumerable<Field> fields)
        {
            var tableDescriptor = new TableDescriptor()
            {
                name = tableName,
                label = label,
                plural = tableName + "s"
            };

            var fieldDescriptors = new List<FieldDescriptor>();

            foreach (var f in fields)
            {
                fieldDescriptors.Add(f.AsFieldDescriptor());
            }

            tableDescriptor.field = fieldDescriptors;

            // build up a request to create the table
            var request = new RestRequest("/rest/schema", Method.POST);
            request.AddHeader("X-DreamFactory-Application-Name", Session.ApplicationName);
            request.AddHeader("X-DreamFactory-Session-Token", Session.ID);
            request.RequestFormat = DataFormat.Json;
            request.JsonSerializer.ContentType = "application/json; charset=utf-8";
            request.JsonSerializer.Options = new SerializerOptions()
            {
                SkipNullProperties = true
            };
            request.AddBody(tableDescriptor);

            // create the table
            var response = Session.Client.Execute(request);

            // TODO: check response
            switch (response.StatusCode)
            {
                case HttpStatusCode.BadRequest:
                    var error = SimpleJson.DeserializeObject<ErrorDescriptor>(response.Content);
                    throw new Exception(error.message);
                case HttpStatusCode.Created:
                    // query the table schema back
                    var actualTable = new Table(Session, tableName);

                    // TODO: handle failure

                    return actualTable;
                default:
                    if (Debugger.IsAttached)
                    {
                        Debugger.Break();
                    }
                    return null;
            }
        }

        public void DeleteTable(string tableName)
        {
            var request = new RestRequest(string.Format("/rest/schema/{0}", tableName), Method.DELETE);
            request.AddHeader("X-DreamFactory-Application-Name", Session.ApplicationName);
            request.AddHeader("X-DreamFactory-Session-Token", Session.ID);

            // delete the table
            var response = Session.Client.Execute(request);

            // TODO: check response
        }

        public object GetRecords(string tableName)
        {
            var request = new RestRequest(string.Format("/rest/db/{0}", tableName), Method.GET);
            request.AddHeader("X-DreamFactory-Application-Name", Session.ApplicationName);
            request.AddHeader("X-DreamFactory-Session-Token", Session.ID);

            request.RequestFormat = DataFormat.Json;

            var response = Session.Client.Execute(request);

            switch (response.ResponseStatus)
            {
                case ResponseStatus.Completed:
                    JsonObject records = SimpleJson.DeserializeObject<JsonObject>(response.Content);
                    foreach (var item in records)
                    {
                        if (item.Key == "record")
                        {
                            foreach (JsonObject fieldset in item.Value as JsonArray)
                            {
                                foreach (var field in fieldset.Keys)
                                {
                                }

                                foreach (var field in fieldset.Values)
                                {
                                }
                            }
                        }
                    }
                    break;
            }

            // {"record":[{"ID":"1","Name":"Item #1","UUID":null,"ITest":23,"Address":"Foo","FTest":"2.4","DBTest":null,"DETest":null,"TS":0}]}
            return null;
        }
    }
}
