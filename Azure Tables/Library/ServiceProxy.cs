using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Xml.Linq;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Collections.Specialized;
using OpenNETCF.WindowsAzure.StorageClient;

namespace OpenNETCF.Azure
{
    internal static class ComparisonOperators
    {
        public const string Equal = "eq";
        public const string GreaterThan = "gt";
        public const string GreaterThanOrEqual = "ge";
        public const string LessThan = "lt";
        public const string LessThanOrEqual = "le";
        public const string NotEqual = "ne";
    }

    internal class ServiceProxy
    {
        private StorageCredentials m_credentials;
        private Uri m_serviceBaseAddress;

        internal ServiceProxy(Uri baseAddressUri, StorageCredentials credentials)
        {
#if !WindowsCE
            // this *greatly* improves insert speed
            ServicePointManager.UseNagleAlgorithm = false;
#endif

            m_credentials = credentials;
            m_serviceBaseAddress = baseAddressUri;
        }

        internal AzureTable GetTable(string tableName)
        {
            var request = GenerateRequest("GET", tableName);

            using (var response = (HttpWebResponse)request.GetResponse(false))
            {
                var dataStream = response.GetResponseStream();

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    using (var reader = new StreamReader(dataStream))
                    {
                        var doc = XDocument.Load(reader);

                        throw new StorageException(response.StatusCode, doc);
                    }
                }


                using (var reader = new StreamReader(dataStream))
                {
                    try
                    {
                        var feedDocument = XDocument.Load(reader);
                        var feed = feedDocument.Element(Namespaces.Atom + "feed");

                        if (feed == null) return null;

                        var id = feed.Element(Namespaces.Atom + "id").Value;
                        var updated = DateTime.Parse(feed.Element(Namespaces.Atom + "updated").Value);
                        var content = feed.Element(Namespaces.Atom + "content");
                        var name = feed.Element(Namespaces.Atom + "title").Value;

                        return new AzureTable(this, name, id, updated);
                    }
                    catch
                    {
                        return null;
                    }
                }
            }
        }

        internal IEnumerable<AzureTable> GetTables()
        {
            var request = GenerateRequest("GET", "Tables");

            using (var response = (HttpWebResponse)request.GetResponse(false))
            {
                var dataStream = response.GetResponseStream();

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    using (var reader = new StreamReader(dataStream))
                    {
                        var doc = XDocument.Load(reader);

                        throw new StorageException(response.StatusCode, doc);
                    }
                }

                using (var reader = new StreamReader(dataStream))
                {
                    var feedDocument = XDocument.Load(reader);
                    var feed = feedDocument.Element(Namespaces.Atom + "feed");
                    foreach (var entry in feed.Elements(Namespaces.Atom + "entry"))
                    {
                        var id = entry.Element(Namespaces.Atom + "id").Value;
                        var updated = DateTime.Parse(entry.Element(Namespaces.Atom + "updated").Value);
                        var content = entry.Element(Namespaces.Atom + "content");
                        var name = content.Element(Namespaces.DataServicesMeta + "properties").Element(Namespaces.DataServices + "TableName").Value;

                        yield return new AzureTable(this, name, id, updated);
                    }
                }
            }
        }

        internal void CreateTable(string tableName)
        {
            Validate
                .Begin()
                .IsNotNullOrEmpty(tableName)
                .IsTrue(IsValidTableName(tableName))
                .Check();

            var createAtom = new XDocument(new XDeclaration("1.0", "utf-8", "yes"),
                new XElement(Namespaces.Atom + "entry",
                    new XElement(Namespaces.Atom + "title"),
                    new XElement(Namespaces.Atom + "id"),
                    new XElement(Namespaces.Atom + "author",
                        new XElement(Namespaces.Atom + "name")),
                    new XElement(Namespaces.Atom + "updated", DateTime.UtcNow.ToString("o")),
                    new XElement(Namespaces.Atom + "content", new XAttribute("type", "application/xml"),
                        new XElement(Namespaces.DataServicesMeta + "properties",
                            new XElement(Namespaces.DataServices + "TableName", tableName)))));

            var request = GenerateRequest("POST", "Tables", null, createAtom);

            using (var response = (HttpWebResponse)request.GetResponse(false))
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.Created: // SUCCESS
                        return;
                    case HttpStatusCode.Conflict:
                        throw new Exception("Table already exists");
                    default:
                        var dataStream = response.GetResponseStream();
                        using (var reader = new StreamReader(dataStream))
                        {
                            var doc = XDocument.Load(reader);
                            throw new StorageException(response.StatusCode, doc);
                        }
                }
            }
        }

        internal void DeleteTable(string tableName)
        {
            Validate
                .Begin()
                .IsNotNullOrEmpty(tableName)
                .Check();

            var request = GenerateRequest("DELETE", string.Format("Tables('{0}')", tableName));

            using (var response = (HttpWebResponse)request.GetResponse())
            {
                if (response.StatusCode != HttpStatusCode.NoContent)
                {
                    throw new Exception("Delete failed");
                }
            }
        }

        internal bool IsValidTableName(string tableName)
        {
            return Regex.Match(tableName, "^[A-Za-z][A-Za-z0-9]{2,62}$").Success;
        }

        internal void InsertEntity(string tableName, AzureEntity entity)
        {
            Validate
                .Begin()
                .ParameterIsNotNull(tableName, "tableName")
                .ParameterIsNotNull(entity, "entity")
                .Check();

            var entry = entity.AsATOMEntry();

            var request = GenerateRequest("POST", tableName, null, entry);

            using (var response = (HttpWebResponse)request.GetResponse(false))
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.Created: // SUCCESS
                        return; 
                    case HttpStatusCode.Conflict: // already exists
                        throw new EntityAlreadyExistsException();
                    default:
                        var dataStream = response.GetResponseStream();
                        using (var reader = new StreamReader(dataStream))
                        {
                            var doc = XDocument.Load(reader);
                            throw new StorageException(response.StatusCode, doc);
                        }
                }
            }
        }

        public void UpdateEntity(string tableName, AzureEntity entity)
        {
            InsertOrReplaceEntity(tableName, entity, true);
        }

        public void InsertOrReplaceEntity(string tableName, AzureEntity entity)
        {
            InsertOrReplaceEntity(tableName, entity, false);
        }

        public void InsertOrReplaceEntity(string tableName, AzureEntity entity, bool setIfMatchHeader)
        {
            Validate
                .Begin()
                .ParameterIsNotNull(tableName, "tableName")
                .ParameterIsNotNull(entity, "entity")
                .Check();

            var entry = entity.AsATOMEntry();

            var request = GenerateRequest(
                "PUT",
                string.Format("{0}(PartitionKey='{1}',RowKey='{2}')", tableName, entity.PartitionKey, entity.RowKey), 
                null, 
                entry);

            if (setIfMatchHeader)
            {
                request.Headers.Add("If-Match", "*");
            }

            using (var response = (HttpWebResponse)request.GetResponse(false))
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.NoContent: // SUCCESS
                        return;
                    default:
                        var dataStream = response.GetResponseStream();
                        using (var reader = new StreamReader(dataStream))
                        {
                            var doc = XDocument.Load(reader);
                            throw new StorageException(response.StatusCode, doc);
                        }
                }
            }
        }

        internal AzureEntity GetEntity(string tableName, string partitionKey, string rowKey)
        {
            var request = GenerateRequest("GET", string.Format("{0}(PartitionKey='{1}',RowKey='{2}')", tableName, partitionKey, rowKey));

            using (var response = (HttpWebResponse)request.GetResponse(false))
            {
                var dataStream = response.GetResponseStream();
                using (var reader = new StreamReader(dataStream))
                {
                    var doc = XDocument.Load(reader);

                    switch (response.StatusCode)
                    {
                        case HttpStatusCode.OK:
                            var entry = doc.Element(Namespaces.Atom + "entry");
                            return AzureEntity.FromATOMFeed(entry);
                        case HttpStatusCode.NotFound:
                            return null;
                        default:
                            throw new StorageException(response.StatusCode, doc);
                    }
                }
            }
        }

        public void DeleteEntity(string tableName, string partitionKey, string rowKey)
        {
            var request = GenerateRequest("DELETE", string.Format("{0}(PartitionKey='{1}',RowKey='{2}')", tableName, partitionKey, rowKey));
            request.Headers.Add("If-Match", "*");
            using (var response = (HttpWebResponse)request.GetResponse(false))
            {
                var dataStream = response.GetResponseStream();
                using (var reader = new StreamReader(dataStream))
                {
                    switch (response.StatusCode)
                    {
                        case HttpStatusCode.NoContent: // success
                            return;
                        default:
                            var doc = XDocument.Load(reader);
                            throw new StorageException(response.StatusCode, doc);
                    }
                }
            }
        }

        internal IEnumerable<AzureEntity> GetEntities(string tableName)
        {
            return GetEntities(tableName, null, -1);
        }

        internal IEnumerable<AzureEntity> GetEntities(string tableName, int maxRows)
        {
            return GetEntities(tableName, null, maxRows);
        }

        internal IEnumerable<AzureEntity> GetEntities(string tableName, string partitionKey, int maxRows)
        {
            var uri = string.Format("{0}()", tableName);
            var queryString = string.Empty;

            if (maxRows > 0)
            {
                queryString = string.Format("$top={0}", maxRows);
            }
            if (!partitionKey.IsNullOrEmpty())
            {
                if (!queryString.IsNullOrEmpty())
                {
                    queryString += "&";
                }

                queryString += BuildQueryExpression("PartitionKey", ComparisonOperators.Equal, partitionKey);
            }


            var request = GenerateRequest("GET", uri, queryString);

            using (var response = (HttpWebResponse)request.GetResponse(false))
            {
                var dataStream = response.GetResponseStream();

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    using (var reader = new StreamReader(dataStream))
                    {
                        var doc = XDocument.Load(reader);

                        throw new StorageException(response.StatusCode, doc);
                    }
                }

                using (var reader = new StreamReader(dataStream))
                {
                    var doc = XDocument.Load(reader);

                    foreach (var entry in doc.Element(Namespaces.Atom + "feed").Elements(Namespaces.Atom + "entry"))
                    {
                        var entity = AzureEntity.FromATOMFeed(entry);
                        yield return entity;
                    }
                }
            }
        }

        private string BuildQueryExpression(string property, string @operator, string constant)
        {
            var filter = new StringBuilder("$filter=");

            filter.Append(property);
            filter.Append(" ");
            filter.Append(@operator);
            filter.Append(" '");
            filter.Append(constant);
            filter.Append("'");

            return filter.ToString().Replace(" ", "%20");
        }

        private void SetRequestContent(HttpWebRequest request, XDocument content)
        {
            request.ContentType = "application/atom+xml";
            var data = Encoding.UTF8.GetBytes(content.ToString(true));
            request.ContentLength = data.Length;

            using (var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }
        }

        internal HttpWebRequest GenerateRequest(string httpMethod, string urlPath)
        {
            return GenerateRequest(httpMethod, urlPath, null, null);
        }

        internal HttpWebRequest GenerateRequest(string httpMethod, string urlPath, string queryString)
        {
            return GenerateRequest(httpMethod, urlPath, queryString, null);
        }

        internal HttpWebRequest GenerateRequest(string httpMethod, string urlPath, string queryString, XDocument content)
        {
            var storageServiceVersion = "2011-08-18";
            var sendContentType = string.Empty;

            var dateInRfc1123Format = DateTime.UtcNow.ToString("R", CultureInfo.InvariantCulture);
            var canonicalizedResource = String.Format("/{0}/{1}", m_credentials.AccountName, urlPath);

            byte[] contentBuffer = null;
            if (content != null)
            {
                sendContentType = "application/atom+xml";
                var xml = content.ToString(true);
                contentBuffer = Encoding.UTF8.GetBytes(xml);
                // var bufferHash = MD5.Create().ComputeHash(contentBuffer);  // apparently unused, even thought he docs say it is?
            }

            var stringToSign = String.Format(
                  "{0}\n{1}\n{2}\n{3}\n{4}",
                  httpMethod.ToUpper(),
                  string.Empty, //Convert.ToBase64String(bufferHash),
                  sendContentType,
                  dateInRfc1123Format,
                  canonicalizedResource);

            var authorizationHeader = CreateAuthorizationHeader(stringToSign);

            if ((!queryString.IsNullOrEmpty()) && (queryString[0] != '?'))
            {
                queryString = '?' + queryString;
            }

            var uri = new Uri(m_serviceBaseAddress + urlPath + (queryString ?? string.Empty));

            var request = (HttpWebRequest)WebRequest.Create(uri);
            request.Method = httpMethod;
            request.Headers.Add("x-ms-date", dateInRfc1123Format);
            request.Headers.Add("x-ms-version", storageServiceVersion);
            request.Headers.Add("Authorization", authorizationHeader);
            request.Headers.Add("Accept-Charset", "UTF-8");
            request.Accept = "application/atom+xml,application/xml";

            request.Headers.Add("DataServiceVersion", "1.0;NetFx");
            request.Headers.Add("MaxDataServiceVersion", "1.0;NetFx");

            if (content != null)
            {
                request.ContentType = sendContentType;
                request.ContentLength = contentBuffer.Length;

                using (var stream = request.GetRequestStream())
                {
                    stream.Write(contentBuffer, 0, contentBuffer.Length);
                }
            }

            return request;
        }

        private string CreateAuthorizationHeader(String canonicalizedString)
        {
            string signature = m_credentials.ComputeHmac(canonicalizedString);

            var header = String.Format(CultureInfo.InvariantCulture, "{0} {1}:{2}",
                "SharedKey",
                m_credentials.AccountName,
                signature);

            return header;
        }
    }
}
