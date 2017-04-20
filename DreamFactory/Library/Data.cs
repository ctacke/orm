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
        private UriFactory m_uris;
        private Session Session { get; set; }

        internal Data(Session session)
        {
            Session = session;
            m_uris = new UriFactory(Session.ServerVersion);
            m_tableCache = new Dictionary<string, Table>(StringComparer.InvariantCultureIgnoreCase);
        }

        internal void RemoveTableFromCache(string tableName)
        {
            if(m_tableCache.ContainsKey(tableName))
            {
                m_tableCache.Remove(tableName);
            }
        }

        public Table GetTable(string tableName)
        {
            if(m_tableCache.ContainsKey(tableName) )
            {
                return m_tableCache[tableName];
            }

            // TODO: enable caching of this info

            if (Session.Disconnected)
            {
                Session.Reconnect();
            }

//            var request = Session.GetSessionRequest(string.Format("/rest/schema/{0}", tableName), Method.GET);
            var request = Session.GetSessionRequest(m_uris.GetTableSchema(tableName), Method.GET);

            var response = Session.Client.Execute<ResourceDescriptor>(request);

            var check = DreamFactoryException.ValidateIRestResponse(response);
            if (check != null)
            {
                // we've seen problems in Mono (3.2.3 on the windows desktop) with GZip decompression failure
                throw new DeserializationException(string.Format("Failed to deserialize schema response for table '{0}': {1}", tableName, response.ErrorMessage), check);
            }

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
                case HttpStatusCode.Unauthorized:
                    if (Debugger.IsAttached) Debugger.Break();
                    
                    Session.Disconnected = true;

                    // TODO: attempt to-reconnect?

                    throw DreamFactoryException.Parse(response);
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

                        throw DreamFactoryException.Parse(response);
                    }

                    throw DreamFactoryException.Parse(response);
            }
        }

        public Table[] GetTables()
        {
            return GetTables(true);
        }

        public Table[] GetTables(bool useCache)
        {
            if (Session.Disconnected)
            {
                Session.Reconnect();
            }

            var request = Session.GetSessionRequest("/rest/db", Method.GET);

            var response = Session.Client.Execute<ResourceDescriptorList>(request);

            var check = DreamFactoryException.ValidateIRestResponse(response);
            if (check != null)
            {
                throw new DeserializationException(string.Format("Failed to deserialize the list of tables: {0}", response.ErrorMessage), check);
            }

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    var list = new List<Table>();

                    lock (m_tableCache)
                    {
                        foreach (var resource in response.Data.resource)
                        {
                            Table t = null;

                            if (useCache)
                            {
                                if (m_tableCache.ContainsKey(resource.name))
                                {
                                    t = m_tableCache[resource.name];
                                    list.Add(t);
                                }
                            }

                            if (t == null)
                            {
                                try
                                {
                                    t = new Table(Session, resource);

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
                                catch (Exception ex)
                                {
                                    // TODO: log this?
                                    if (Debugger.IsAttached) Debugger.Break();
                                    Debug.WriteLine(ex.Message);
                                    throw;
                                }
                            }
                        }

                    }
            
                    return list.ToArray();
                case HttpStatusCode.Forbidden:
                case HttpStatusCode.Unauthorized:
                    if (Debugger.IsAttached) Debugger.Break();

                    Session.Disconnected = true;

                    throw DreamFactoryException.Parse(response);
                default:
                    throw DreamFactoryException.Parse(response);
            }
        }

        public Table UpdateTable(string tableName, IEnumerable<Field> updatedFieldList)
        {
            if (Session.Disconnected)
            {
                Session.Reconnect();
            }

            var tableDescriptor = new TableDescriptor();
            tableDescriptor.name = tableName;
            tableDescriptor.field = new List<FieldDescriptor>();
            foreach (var f in updatedFieldList)
            {
                tableDescriptor.field.Add(f.AsFieldDescriptor());
            }

            var request = Session.GetSessionRequest(m_uris.GetTableSchema(tableName), Method.PATCH);

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
                case HttpStatusCode.Forbidden:
                case HttpStatusCode.Unauthorized:
                    if (Debugger.IsAttached) Debugger.Break();

                    Session.Disconnected = true;

                    throw DreamFactoryException.Parse(response);
                default:
                    throw DreamFactoryException.Parse(response);
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
            if (Session.Disconnected)
            {
                Session.Reconnect();
            }

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
            var request = Session.GetSessionRequest(m_uris.SchemaRoot, Method.POST);

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
                case HttpStatusCode.OK:
                    // query the table schema back
                    var actualTable = new Table(Session, tableName);

                    lock (m_tableCache)
                    {
                        m_tableCache.Add(actualTable.Name, actualTable);
                    }

                    // TODO: handle failure

                    return actualTable;
                case HttpStatusCode.Forbidden:
                case HttpStatusCode.Unauthorized:
                    if (Debugger.IsAttached) Debugger.Break();

                    Session.Disconnected = true;

                    throw DreamFactoryException.Parse(response);
                default:
                    throw DreamFactoryException.Parse(response);
            }
        }

        public void DeleteTable(string tableName)
        {
            if (Session.Disconnected)
            {
                Session.Reconnect();
            }

            var request = Session.GetSessionRequest(string.Format("{0}/{1}", m_uris.SchemaRoot, tableName), Method.DELETE);

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
