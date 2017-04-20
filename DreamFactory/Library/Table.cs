using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Text;
using OpenNETCF.Web;
using RestSharp;
using System.Diagnostics;

namespace OpenNETCF.DreamFactory
{
    public sealed class Table
    {
        public Field KeyField { get; private set; }
        public Field[] Fields { get; private set; }
        public string Name { get; private set; }
        public string Label { get; private set; }

        private UriFactory m_uris;
        private Session Session { get; set; }
        
        internal Table(Session session, ResourceDescriptor resource)
            : this(session, resource.name)
        {
        }

        internal Table(Session session, string tableName)
        {
            Session = session;
            m_uris = new UriFactory(Session.ServerVersion);

            if (Session.Disconnected)
            {
                Session.Reconnect();
            }

            var request = Session.GetSessionRequest(string.Format("{0}/{1}", m_uris.SchemaRoot, tableName), Method.GET);

            var response = Session.Client.Execute<TableDescriptor>(request);

            var check = DreamFactoryException.ValidateIRestResponse(response);
            if (check != null)
            {
                throw new DeserializationException(string.Format("Failed to retrieve the schema for table '{0}': {1}", tableName, response.ErrorMessage), check);
            }

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    var descriptor = response.Data;

                    Name = descriptor.name;
                    Label = descriptor.label;

                    var fieldList = new List<Field>();

                    foreach (var f in descriptor.field)
                    {
                        var fld = f.AsField();
                        fieldList.Add(fld);
                        if (f.is_primary_key.HasValue && f.is_primary_key.Value)
                        {
                            KeyField = fld;
                        }
                    }

                    Fields = fieldList.ToArray();
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

        public IEnumerable<object[]> GetRecords(string filterStatement)
        {
            return GetRecords(filterStatement, null);
        }

        public IEnumerable<object[]> GetRecords(params object[] resourceIDs)
        {
            return GetRecords(null, resourceIDs);
        }

        public IEnumerable<object[]> GetRecords(string filterStatement, params object[] resourceIDs)
        {
            if (Session.Disconnected)
            {
                Session.Reconnect();
            }

            var request = Session.GetSessionRequest(string.Format("/rest/db/{0}", Name), Method.GET);

            if ((resourceIDs != null) && (resourceIDs.Length > 0))
            {
                request.Parameters.Add(new Parameter()
                    {
                        Name = "ids",
                        Value = string.Join(",", resourceIDs.Select(i => i.ToString()).ToArray()),
                        Type = ParameterType.GetOrPost
                    });
            }
            if (!filterStatement.IsNullOrEmpty())
            {
                request.Parameters.Add(new Parameter()
                {
                    Name = "filter",
                    Value = filterStatement,
                    Type = ParameterType.GetOrPost
                });
            }
            
            var response = Session.Client.Execute<ResourceDescriptorList>(request);

            var check = DreamFactoryException.ValidateIRestResponse(response);
            if (check != null)
            {
                throw new DeserializationException(string.Format("Failed to retrieve a records from table '{0}': {1}", Name, response.ErrorMessage), check);
            }

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:

                    JsonObject results = SimpleJson.DeserializeObject<JsonObject>(response.Content);

                    var records = new List<object[]>();

                    if (results.Count != 0)
                    {
                        // {"record":["Could not find record for id = '9'"]}
                        var jarray = results["record"] as JsonArray;

                        if (jarray.Count > 0)
                        {
                            if (jarray[0] as JsonObject == null)
                            {
                                Debug.WriteLine("No results");
                                return records;
                            }

                            foreach (JsonObject item in jarray)
                            {
                                var array = new object[Fields.Length];
                                var index = 0;

                                foreach (var field in Fields)
                                {
                                    array[index++] = item[field.Name];
                                }

                                records.Add(array);
                            }
                            
                        }
                    }
                    return records;
                case HttpStatusCode.Forbidden:
                case HttpStatusCode.Unauthorized:
                    if (Debugger.IsAttached) Debugger.Break();

                    Session.Disconnected = true;

                    throw DreamFactoryException.Parse(response);
                default:
                    throw DreamFactoryException.Parse(response);
            }
        }

        public int GetRecordCount()
        {
            return GetRecordCount(null);
        }

        public int GetRecordCount(string filter)
        {
            if (Session.Disconnected)
            {
                Session.Reconnect();
            }

            var request = Session.GetSessionRequest(string.Format("/rest/db/{0}", Name), Method.GET);
            request.Parameters.Add(new Parameter() { Name = "limit", Value = 1, Type = ParameterType.GetOrPost });
            request.Parameters.Add(new Parameter() { Name = "include_count", Value = "true", Type = ParameterType.GetOrPost });

            if (!string.IsNullOrEmpty(filter))
            {
                request.Parameters.Add(new Parameter() { Name = "filter", Value = filter, Type = ParameterType.GetOrPost });
            }

            var response = Session.Client.Execute<ResourceDescriptorList>(request);

            var check = DreamFactoryException.ValidateIRestResponse(response);
            if (check != null)
            {
                throw new DeserializationException(string.Format("Failed to retrieve a record count on table '{0}': {1}", Name, response.ErrorMessage), check);
            }

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    // TODO: this feels fragile - we should look at improving it
                    return Convert.ToInt32(((SimpleJson.DeserializeObject(response.Content) as JsonObject)[1] as JsonObject)[0]);

                case HttpStatusCode.Forbidden:
                case HttpStatusCode.Unauthorized:
                    if (Debugger.IsAttached) Debugger.Break();

                    Session.Disconnected = true;

                    throw DreamFactoryException.Parse(response);
                default:
                    throw DreamFactoryException.Parse(response);
            }
        }

        public IEnumerable<object[]> GetRecords()
        {
            return GetRecords(-1);
        }

        public IEnumerable<object[]> GetRecords(int limit)
        {
            return GetRecords(limit, 0, null, null);
        }

        public IEnumerable<object[]> GetRecords(int limit, int offset, string filter, string order)
        {
            if (Session.Disconnected)
            {
                Session.Reconnect();
            }

            var request = Session.GetSessionRequest(string.Format("/rest/db/{0}", Name), Method.GET);
            if (limit > 0)
            {
                request.Parameters.Add(new Parameter() { Name = "limit", Value = limit, Type = ParameterType.GetOrPost });
            }
            if (offset > 0)
            {
                request.Parameters.Add(new Parameter() { Name = "offset", Value = offset, Type = ParameterType.GetOrPost });
            }
            if (!filter.IsNullOrEmpty())
            {
                request.Parameters.Add(new Parameter() { Name = "filter", Value = filter, Type = ParameterType.GetOrPost });
            }
            if (!order.IsNullOrEmpty())
            {
                request.Parameters.Add(new Parameter() { Name = "order", Value = order, Type = ParameterType.GetOrPost });
            }

            var response = Session.Client.Execute<ResourceDescriptorList>(request);

            var check = DreamFactoryException.ValidateIRestResponse(response);
            if (check != null)
            {
                throw new DeserializationException(string.Format("Failed to retrieve a records from table '{0}': {1}", Name, response.ErrorMessage), check);
            }

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:

                    JsonObject results = SimpleJson.DeserializeObject<JsonObject>(response.Content);

                    var records = new List<object[]>();

                    foreach (JsonObject item in results["record"] as JsonArray)
                    {
                        var array = new object[Fields.Length];
                        var index = 0;

                        foreach (var field in Fields)
                        {
                            array[index++] = item[field.Name];
                        }

                        records.Add(array);
                    }

                    return records;
                case HttpStatusCode.NotFound:
                    // table doesn't exist, make sure it's not in the cache (e.g. remote delete)
                    Session.Data.RemoveTableFromCache(this.Name);

                    throw new TableNotFoundException(this.Name);
                case HttpStatusCode.Forbidden:
                case HttpStatusCode.Unauthorized:
                    if (Debugger.IsAttached) Debugger.Break();

                    Session.Disconnected = true;

                    throw DreamFactoryException.Parse(response);
                default:
                    throw DreamFactoryException.Parse(response);
            }
        }

        public void DeleteFilteredRecords(string filter)
        {
            if (Session.Disconnected)
            {
                Session.Reconnect();
            }

            var request = Session.GetSessionRequest(string.Format("/rest/db/{0}", Name), Method.DELETE);

            request.Parameters.Add(new Parameter()
            {
                Name = "filter",
                Value = filter,
                Type = ParameterType.GetOrPost
            });

            var response = Session.Client.Execute(request);

            switch (response.StatusCode)
            {
                case HttpStatusCode.Created:
                case HttpStatusCode.OK:
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

        public void DeleteRecords(params object[] resourceIDs)
        {
            if (Session.Disconnected)
            {
                Session.Reconnect();
            }

            var request = Session.GetSessionRequest(string.Format("/rest/db/{0}", Name), Method.DELETE);

            if ((resourceIDs != null) && (resourceIDs.Length > 0))
            {
                request.Parameters.Add(new Parameter()
                {
                    Name = "ids",
                    Value = string.Join(",", resourceIDs.Select(i => i.ToString()).ToArray()),
                    Type = ParameterType.GetOrPost
                });
            }
            else
            {
                // this is to delete all - we're setting up a "filter" that (hopefully) matches no record in the table
                if (KeyField != null)
                {
                    request.Parameters.Add(new Parameter()
                    {
                        Name = "filter",
                        Value = string.Format("{0}!='fOObARbAZ'", KeyField.Name),
                        Type = ParameterType.GetOrPost
                    });
                }
                else
                {
                    request.Parameters.Add(new Parameter()
                    {
                        Name = "filter",
                        Value = string.Format("{0}!='fOObARbAZ'", Fields[0].Name),
                        Type = ParameterType.GetOrPost
                    });
                }
            }

            var response = Session.Client.Execute(request);

            switch (response.StatusCode)
            {
                case HttpStatusCode.Created:
                case HttpStatusCode.OK:
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

        public object UpdateRecord(Dictionary<string, object> fields)
        {
            return SendRecord(fields, true);
        }

        public object InsertRecord(Dictionary<string, object> fields)
        {
            return SendRecord(fields, false);
        }

        private object SendRecord(Dictionary<string, object> fields, bool isUpdate)
        {
            try
            {
                if (Session.Disconnected)
                {
                    Session.Reconnect();
                }

                var request = Session.GetSessionRequest(string.Format("/rest/db/{0}", Name), isUpdate ? Method.PUT : Method.POST);

                // "{\"record\":[{\"ID\":\"1\",\"Name\":\"Item #1\",\"UUID\":null,\"ITest\":23,\"Address\":\"Foo\",\"FTest\":\"2.4\",\"DBTest\":null,\"DETest\":null,\"TS\":0}]}"
                var o = SimpleJson.SerializeObject(fields);

                // we have to name the collection or the API will fail
                o = string.Format("{{ \"record\": [{0}] }}", o);

                // NOTE: We must use AddParameter here because we've already serialized the JSON.  AddBody attempts to serialize it again
                // At some point I want to fix RestSharp to not do this, as it's stupid behavior
                request.AddParameter("application/json", o, ParameterType.RequestBody);

                var response = Session.Client.Execute(request);

                switch (response.StatusCode)
                {
                    case HttpStatusCode.Created:
                    case HttpStatusCode.OK:
                        // the key for the inserted item gets returned (if there is one)
                        var pkJson = SimpleJson.DeserializeObject(response.Content) as JsonObject;
                        if (pkJson.Count == 0) return null;

                        var key = (pkJson[0] as JsonArray)[0] as JsonObject;
                        // no data sent back (i.e. there was no key field)
                        if (key == null) return null;

                        var name = key.Keys.First();
                        // no data sent back (i.e. there was no key field)
                        if (name.IsNullOrEmpty()) return null;

                        var value = key[name];
                        return value;
                    case HttpStatusCode.InternalServerError:
                        var ex = DreamFactoryException.Parse(response);
                        if ((Session.ServerVersion == new Version("1.9.2")) && (ex.Message == "array (\n)"))
                        {
                            // this is an edge-case in DF 1.9.2 where a successful insert sometime will still return a 500 *even though* the record did insert.  WTF??
                            return null;
                        }
                        throw ex;
                    case HttpStatusCode.Forbidden:
                    case HttpStatusCode.Unauthorized:
                        if (Debugger.IsAttached) Debugger.Break();

                        Session.Disconnected = true;

                        throw DreamFactoryException.Parse(response);
                    default:
                        throw DreamFactoryException.Parse(response);
                }
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }
    }

    // SAMPLE SCHEMA SHOWING ALL SUPPORTED DATA TYPES (as of 7/26/13)
    //{
    //"name":"MyTestTable",
    //"label":"MyTestTable",
    //"plural":"MyTestTables",
    //"primary_key":"FieldA",
    //"name_field":null,

    //"field":[
    //{
    //    "name":"FieldA",
    //    "label":"FieldA",
    //    "type":"id",
    //    "data_type":"integer",
    //    "db_type":"int(11)",
    //    "length":11,
    //    "precision":11,
    //    "scale":0,
    //    "default":null,
    //    "required":false,
    //    "allow_null":false,
    //    "fixed_length":false,
    //    "supports_multibyte":false,
    //    "auto_increment":true,
    //    "is_primary_key":true,
    //    "is_foreign_key":false,
    //    "ref_table":"",
    //    "ref_fields":"",
    //    "validation":"",
    //    "values":[]
    //},
    //{
    //    "name":"FieldB",
    //    "label":"FieldB",
    //    "type":"string",
    //    "data_type":"string",
    //    "db_type":"varchar(255)",
    //    "length":255,
    //    "precision":255,
    //    "scale":0,
    //    "default":null,
    //    "required":false,
    //    "allow_null":true,
    //    "fixed_length":false,
    //    "supports_multibyte":false,
    //    "auto_increment":false,
    //    "is_primary_key":false,
    //    "is_foreign_key":false,
    //    "ref_table":"",
    //    "ref_fields":"",
    //    "validation":"",
    //    "values":[]
    //},
    //{
    //    "name":"FieldC",
    //    "label":"FieldC",
    //    "type":"integer",
    //    "data_type":"integer",
    //    "db_type":"int(11)",
    //    "length":11,
    //    "precision":11,
    //    "scale":0,
    //    "default":null,
    //    "required":false,
    //    "allow_null":true,
    //    "fixed_length":false,
    //    "supports_multibyte":false,
    //    "auto_increment":false,
    //    "is_primary_key":false,
    //    "is_foreign_key":false,
    //    "ref_table":"",
    //    "ref_fields":"",
    //    "validation":"",
    //    "values":[]
    //},
    //{
    //    "name":"FieldD",
    //    "label":"FieldD",
    //    "type":"text",
    //    "data_type":"string",
    //    "db_type":"text",
    //    "length":0,
    //    "precision":0,
    //    "scale":0,
    //    "default":null,
    //    "required":false,
    //    "allow_null":true,
    //    "fixed_length":false,
    //    "supports_multibyte":false,
    //    "auto_increment":false,
    //    "is_primary_key":false,
    //    "is_foreign_key":false,
    //    "ref_table":"",
    //    "ref_fields":"",
    //    "validation":"",
    //    "values":[]
    //},
    //{
    //    "name":"FieldE",
    //    "label":"FieldE",
    //    "type":"boolean",
    //    "data_type":"integer",
    //    "db_type":"tinyint(1)",
    //    "length":1,
    //    "precision":1,
    //    "scale":0,
    //    "default":null,
    //    "required":false,
    //    "allow_null":true,
    //    "fixed_length":false,
    //    "supports_multibyte":false,
    //    "auto_increment":false,
    //    "is_primary_key":false,
    //    "is_foreign_key":false,
    //    "ref_table":"",
    //    "ref_fields":"",
    //    "validation":"",
    //    "values":[]
    //},
    //{
    //    "name":"FieldF",
    //    "label":"FieldF",
    //    "type":"binary",
    //    "data_type":"string",
    //    "db_type":"varbinary(255)",
    //    "length":255,
    //    "precision":255,
    //    "scale":0,
    //    "default":null,
    //    "required":false,
    //    "allow_null":true,
    //    "fixed_length":false,
    //    "supports_multibyte":false,
    //    "auto_increment":false,
    //    "is_primary_key":false,
    //    "is_foreign_key":false,
    //    "ref_table":"",
    //    "ref_fields":"",
    //    "validation":"",
    //    "values":[]
    //    },
    //    {
    //    "name":"FieldG",
    //    "label":"FieldG",
    //    "type":"blob",
    //    "data_type":"string",
    //    "db_type":"blob",
    //    "length":0,
    //    "precision":0,
    //    "scale":0,
    //    "default":null,
    //    "required":false,
    //    "allow_null":true,
    //    "fixed_length":false,
    //    "supports_multibyte":false,
    //    "auto_increment":false,
    //    "is_primary_key":false,
    //    "is_foreign_key":false,
    //    "ref_table":"",
    //    "ref_fields":"",
    //    "validation":"",
    //    "values":[]
    //},
    //{
    //    "name":"FieldH",
    //    "label":"FieldH",
    //    "type":"float",
    //    "data_type":"double",
    //    "db_type":"float",
    //    "length":0,
    //    "precision":0,
    //    "scale":0,
    //    "default":null,
    //    "required":false,
    //    "allow_null":true,
    //    "fixed_length":false,
    //    "supports_multibyte":false,
    //    "auto_increment":false,
    //    "is_primary_key":false,
    //    "is_foreign_key":false,
    //    "ref_table":"",
    //    "ref_fields":"",
    //    "validation":"",
    //    "values":[]
    //},
    //{
    //    "name":"FieldI",
    //    "label":"FieldI",
    //    "type":"decimal",
    //    "data_type":"string",
    //    "db_type":"decimal(10,0)",
    //    "length":10,
    //    "precision":10,
    //    "scale":0,
    //    "default":null,
    //    "required":false,
    //    "allow_null":true,
    //    "fixed_length":false,
    //    "supports_multibyte":false,
    //    "auto_increment":false,
    //    "is_primary_key":false,
    //    "is_foreign_key":false,
    //    "ref_table":"",
    //    "ref_fields":"",
    //    "validation":"",
    //    "values":[]
    //},
    //{
    //    "name":"FieldJ",
    //    "label":"FieldJ",
    //    "type":"datetime",
    //    "data_type":"string",
    //    "db_type":"datetime",
    //    "length":0,
    //    "precision":0,
    //    "scale":0,
    //    "default":null,
    //    "required":false,
    //    "allow_null":true,
    //    "fixed_length":false,
    //    "supports_multibyte":false,
    //    "auto_increment":false,
    //    "is_primary_key":false,
    //    "is_foreign_key":false,
    //    "ref_table":"",
    //    "ref_fields":"",
    //    "validation":"",
    //    "values":[]
    //},
    //{
    //    "name":"FieldK",
    //    "label":"FieldK",
    //    "type":"date",
    //    "data_type":"string",
    //    "db_type":"date",
    //    "length":0,
    //    "precision":0,
    //    "scale":0,
    //    "default":null,
    //    "required":false,
    //    "allow_null":true,
    //    "fixed_length":false,
    //    "supports_multibyte":false,
    //    "auto_increment":false,
    //    "is_primary_key":false,
    //    "is_foreign_key":false,
    //    "ref_table":"",
    //    "ref_fields":"",
    //    "validation":"",
    //    "values":[]
    //},
    //{
    //    "name":"FieldL",
    //    "label":"FieldL",
    //    "type":"time",
    //    "data_type":"string",
    //    "db_type":"time",
    //    "length":0,
    //    "precision":0,
    //    "scale":0,
    //    "default":null,
    //    "required":false,
    //    "allow_null":true,
    //    "fixed_length":false,
    //    "supports_multibyte":false,
    //    "auto_increment":false,
    //    "is_primary_key":false,
    //    "is_foreign_key":false,
    //    "ref_table":"",
    //    "ref_fields":"",
    //    "validation":"",
    //    "values":[]}],
    //    "related":[]
    //}
}
