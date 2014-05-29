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

namespace OpenNETCF.Azure
{
    public class TableNotFoundException : Exception
    {
        private string m_table;

        public TableNotFoundException(string tableName)
        {
            m_table = tableName;
        }

        public override string Message
        {
            get { return string.Format("Table '{0}' not found.", m_table); }
        }
    }

    public class EntityAlreadyExistsException : Exception
    {
        public EntityAlreadyExistsException()
        {
        }

        public override string Message
        {
            get { return "A matching entity already exists in the target table."; }
        }
    }
}

namespace OpenNETCF.WindowsAzure.StorageClient
{
    using OpenNETCF.Azure;

    public class StorageException : Exception
    {
        public string ErrorCode { get; private set; }
        public string ExtendedErrorInformation { get; private set; }
        public HttpStatusCode StatusCode { get; private set; }

        internal StorageException(HttpStatusCode status, XDocument errorDocument)
        {
            this.StatusCode = status;
            var e = errorDocument.Element(Namespaces.DataServicesMeta + "error");
            this.ErrorCode = e.Element(Namespaces.DataServicesMeta + "code").Value;
            this.ExtendedErrorInformation = e.Element(Namespaces.DataServicesMeta + "message").Value;
        }
    }
}
