using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Collections.Specialized;

namespace OpenNETCF.Azure
{
    public class AzureTableCollection : IEnumerable<AzureTable>
    {
        private OrderedDictionary<string, AzureTable> m_tables;
        private ServiceProxy m_proxy;

        internal AzureTableCollection(ServiceProxy proxy)
        {
            m_proxy = proxy;
            // table names are case-insensitive
            m_tables = new OrderedDictionary<string, AzureTable>(StringComparer.InvariantCultureIgnoreCase);
        }

        public AzureTable this[int index]
        {
            get { return m_tables[index]; }
        }

        public AzureTable this[string tableName]
        {
            get { return m_tables[tableName]; }
        }

        public int Count
        {
            get { return m_tables.Count; }
        }

        public void Refresh()
        {
            this.Clear();
            this.AddRange(m_proxy.GetTables());
        }

        public bool CreateTableIfNotExist(string tableName)
        {
            Validate
                .Begin()
                .IsNotNullOrEmpty(tableName)
                .Check();

            if (DoesTableExist(tableName)) return false;

            CreateTable(tableName);
            return true;
        }

        public bool DeleteTableIfExist(string tableName)
        {
            Validate
                .Begin()
                .IsNotNullOrEmpty(tableName)
                .Check();

            if (!DoesTableExist(tableName)) return false;

            DeleteTable(tableName);

            return true;
        }

        public void CreateTable(string tableName)
        {
            Validate
                .Begin()
                .IsNotNullOrEmpty(tableName)
                .Check();

            if (!m_proxy.IsValidTableName(tableName))
            {
                throw new ArgumentException("Invalid Table Name");
            }

            if (DoesTableExist(tableName))
            {
                throw new ArgumentException("Table already exists");
            }

            // create
            m_proxy.CreateTable(tableName);

            // call a refresh to populate the ID and LastUpdate on the new table
            Refresh();
        }

        public bool DoesTableExist(string tableName)
        {
            if (m_tables.ContainsKey(tableName)) return true;

            // refresh is we don't have it locally
            Refresh();

            return m_tables.ContainsKey(tableName);
        }

        public void DeleteTable(string tableName)
        {
            Validate
                .Begin()
                .IsNotNullOrEmpty(tableName)
                .Check();

            if (!DoesTableExist(tableName))
            {
                Refresh();
            }

            if (!DoesTableExist(tableName))
            {
                throw new ArgumentException(string.Format("Table '{0}' does not exist", tableName));
            }

            m_proxy.DeleteTable(tableName);

            Refresh();
        }


        private void Clear()
        {
            m_tables.Clear();
        }

        private void Add(AzureTable table)
        {
            m_tables.Add(table.TableName, table);
        }

        private void AddRange(IEnumerable<AzureTable> tables)
        {
            foreach (var table in tables)
            {
                m_tables.Add(table.TableName, table);
            }
        }

        public IEnumerator<AzureTable> GetEnumerator()
        {
            return m_tables.Values.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        internal static AzureTableCollection FromATOMFeed(ServiceProxy proxy, XDocument feedDocument)
        {
            var tables = new AzureTableCollection(proxy);

            var feed = feedDocument.Element(Namespaces.Atom + "feed");
            foreach (var entry in feed.Elements(Namespaces.Atom + "entry"))
            {
                var id = entry.Element(Namespaces.Atom + "id").Value;
                var updated = DateTime.Parse(entry.Element(Namespaces.Atom + "updated").Value);
                var content = entry.Element(Namespaces.Atom + "content");
                var name = content.Element(Namespaces.DataServicesMeta + "properties").Element(Namespaces.DataServices + "TableName").Value;

                tables.Add(new AzureTable(proxy, name, id, updated));
            }

            return tables;
        }
    }
}
