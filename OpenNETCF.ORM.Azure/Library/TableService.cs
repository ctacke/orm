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
    public class TableService
    {
        private ServiceProxy m_proxy;
        private AzureTableCollection m_tables;

        public TableService(string baseAddress, StorageCredentials credentials)
            : this(new Uri(baseAddress), credentials)
        {
        }

        public TableService(Uri baseUri, StorageCredentials credentials)
        {
            m_proxy = new ServiceProxy(baseUri, credentials);
        }

        public AzureTableCollection Tables
        {
            get
            {
                // lazy load the first instance
                if (m_tables == null)
                {
                    m_tables = new AzureTableCollection(m_proxy);
                    m_tables.Refresh();
                }

                return m_tables; 
            }
        }
    }
}
