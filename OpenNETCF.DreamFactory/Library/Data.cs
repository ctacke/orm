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
                case HttpStatusCode.NotFound:
                    return null;
                case HttpStatusCode.Forbidden:
                    if (Debugger.IsAttached) Debugger.Break();
                    
                    Session.Disconnected = true;
                    
                    var ferror = SimpleJson.DeserializeObject<ErrorDescriptorList>(response.Content);
                    if (ferror.error.Count > 0)
                    {
                        throw new Exception(ferror.error[0].message);
                    }

                    throw new Exception("Not authorized");
                default:
                    var error = SimpleJson.DeserializeObject<ErrorDescriptorList>(response.Content);
                    if (error.error.Count > 0)
                    {
                        // As of 10/24/13 DreamFactory has a bug where it returns a 500 instead of a 404 for a not-found table
                        // Oddly, the error code inside the error returned is a 404, so they know it's not found.  This is the workaround.
                        if (error.error[0].code == 404)
                        {
                            return null;
                        }

                        if (Debugger.IsAttached) Debugger.Break();

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

                    lock (m_tableCache)
                    {
                        foreach (var resource in response.Data.resource)
                        {
                            var t = new Table(Session, resource);

                            list.Add(t);

                            if (!m_tableCache.ContainsKey(t.Name))
                            {
                                m_tableCache.Add(t.Name, t);
                            }
                            else
                            {
                                m_tableCache[t.Name] = t;
                            }
                        }

                    }
            
                    return list.ToArray();

                default:
                    if (Debugger.IsAttached) Debugger.Break();
                    var error = SimpleJson.DeserializeObject<ErrorDescriptorList>(response.Content);
                    if (error.error.Count > 0)
                    {
                        throw new Exception(error.error[0].message);
                    }

                    throw new Exception();
            }
        }

        public Table UpdateTable(string tableName, IEnumerable<Field> updatedFieldList)
        {
            var fieldDescriptors = new List<FieldDescriptor>();

            foreach (var f in updatedFieldList)
            {
                fieldDescriptors.Add(f.AsFieldDescriptor());
            }

            var request = Session.GetSessionRequest(string.Format("/rest/schema/{0}", tableName), Method.PUT);

            request.JsonSerializer.ContentType = "application/json; charset=utf-8";
            request.JsonSerializer.Options = new SerializerOptions()
            {
                SkipNullProperties = true
            };
            request.AddBody(fieldDescriptors);

            // create the table
            var response = Session.Client.Execute(request);

            // check response
            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    // query the table schema back
                    var actualTable = new Table(Session, tableName);

                    lock (m_tableCache)
                    {
                        if (!m_tableCache.ContainsKey(actualTable.Name))
                        {
                            m_tableCache.Add(actualTable.Name, actualTable);
                        }
                        else
                        {
                            m_tableCache[actualTable.Name] = actualTable;
                        }
                    }

                    // TODO: handle failure

                    return actualTable;
                default:
                    if (Debugger.IsAttached) Debugger.Break();
                    var error = SimpleJson.DeserializeObject<ErrorDescriptor>(response.Content);
                    throw new Exception(error.message);
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

            // check response
            switch (response.StatusCode)
            {
                case HttpStatusCode.Created:
                    // query the table schema back
                    var actualTable = new Table(Session, tableName);

                    lock (m_tableCache)
                    {
                        m_tableCache.Add(actualTable.Name, actualTable);
                    }

                    // TODO: handle failure

                    return actualTable;
                default:
                    if (Debugger.IsAttached) Debugger.Break();
                    var error = SimpleJson.DeserializeObject<ErrorDescriptor>(response.Content);
                    throw new Exception(error.message);
            }
        }

        public void DeleteTable(string tableName)
        {
            var request = Session.GetSessionRequest(string.Format("/rest/schema/{0}", tableName), Method.DELETE);

            // delete the table
            var response = Session.Client.Execute(request);

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    lock (m_tableCache)
                    {
                        if (m_tableCache.ContainsKey(tableName))
                        {
                            m_tableCache.Remove(tableName);
                        }
                    }
                    break;
                default:
                    if (Debugger.IsAttached) Debugger.Break();
                    var error = SimpleJson.DeserializeObject<ErrorDescriptor>(response.Content);
                    throw new Exception(error.message);
            }
        }
    }
}
