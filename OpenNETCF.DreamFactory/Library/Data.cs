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
        private Dictionary<string, Table> m_tableCache;
        private Session Session { get; set; }

        internal Data(Session session)
        {
            Session = session;
            m_tableCache = new Dictionary<string, Table>(StringComparer.InvariantCultureIgnoreCase);
        }

        public Table GetTable(string tableName)
        {
            if(m_tableCache.ContainsKey(tableName) )
            {
                return m_tableCache[tableName];
            }

            // TODO: enable caching of this info

            var request = Session.GetSessionRequest(string.Format("/rest/schema/{0}", tableName), Method.GET);

            var response = Session.Client.Execute<ResourceDescriptor>(request);

            switch(response.StatusCode)
            {
                case HttpStatusCode.OK:
                    var table = new Table(Session, response.Data);
                    if (!m_tableCache.ContainsKey(tableName))
                    {
                        m_tableCache.Add(tableName, table);
                    }
                    return table;
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
            var request = Session.GetSessionRequest("/rest/db", Method.GET);

            var response = Session.Client.Execute<ResourceDescriptorList>(request);

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    var list = new List<Table>();

                    foreach (var resource in response.Data.resource)
                    {
                        var t = new Table(Session, resource);
                        if (!m_tableCache.ContainsKey(t.Name))
                        {
                            m_tableCache.Add(t.Name, t);
                        }
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
            var request = Session.GetSessionRequest("/rest/schema", Method.POST);

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
                case HttpStatusCode.Created:
                    // query the table schema back
                    var actualTable = new Table(Session, tableName);

                    // TODO: handle failure

                    return actualTable;
                default:
                    var error = SimpleJson.DeserializeObject<ErrorDescriptor>(response.Content);
                    throw new Exception(error.message);
            }
        }

        public void DeleteTable(string tableName)
        {
            var request = Session.GetSessionRequest(string.Format("/rest/schema/{0}", tableName), Method.DELETE);

            // delete the table
            var response = Session.Client.Execute(request);

            // TODO: check response
        }

        public object GetRecords(string tableName)
        {
            var request = Session.GetSessionRequest(string.Format("/rest/db/{0}", tableName), Method.GET);

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
