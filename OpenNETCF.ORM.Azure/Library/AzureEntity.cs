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
    public class Link
    {
        internal Link()
        {
        }

        public string Rel { get; internal set; }
        public string Title { get; internal set; }
        public string HRef { get; internal set; }
    }

    public class AzureEntity
    {
        public string ID { get; internal set; }
        public DateTime LastUpdated { get; internal set; }
        public Link Link { get; private set; }
        public string Category { get; private set; }

        public string PartitionKey { get; set; }
        public string RowKey { get; set; }

        public AzureFieldCollection Fields { get; private set; }

        private char[] DisallowedKeyCharacters = new char[] { '/', '\\', '#', '?' };

        public AzureEntity(string partitionKey, string rowKey)
            : this(partitionKey, rowKey, null)
        {
        }

        private AzureEntity(string partitionKey, string rowKey, AzureFieldCollection fields)
        {
            Validate
                .Begin()
                .IsNotNullOrEmpty(partitionKey)
                .IsNotNullOrEmpty(rowKey)
                .Check();

            if ((partitionKey.IndexOfAny(DisallowedKeyCharacters) >= 0) || (rowKey.IndexOfAny(DisallowedKeyCharacters) >= 0))
            {
                var message = "Key Values cannot contain any of the following characters: ";
                for (int i = 0; i < DisallowedKeyCharacters.Length; i++)
                {
                    message += string.Format("'{0}' ", DisallowedKeyCharacters[i]);
                }

                throw new ArgumentException(message);
            }

            PartitionKey = partitionKey;
            RowKey = rowKey;

            if (fields == null)
            {
                Fields = new AzureFieldCollection();
            }
            else
            {
                Fields = fields;
            }
        }

        internal XDocument AsATOMEntry()
        {
            var propertiesElement = new XElement(Namespaces.DataServicesMeta + "properties",
                new XElement(Namespaces.DataServices + "PartitionKey", this.PartitionKey),
                new XElement(Namespaces.DataServices + "RowKey", this.RowKey));

            foreach (var field in this.Fields)
            {
                propertiesElement.Add(field.AsATOMProperty());
            }

            var entry = new XDocument(new XDeclaration("1.0", "utf-8", "yes"),
                new XElement(Namespaces.Atom + "entry",
                    new XElement(Namespaces.Atom + "title"),
                    new XElement(Namespaces.Atom + "id"),
                    new XElement(Namespaces.Atom + "author",
                        new XElement(Namespaces.Atom + "name")),
                    new XElement(Namespaces.Atom + "updated", DateTime.UtcNow.ToString("o")),
                    new XElement(Namespaces.Atom + "content", new XAttribute("type", "application/xml"),
                        propertiesElement)));

            return entry;
        }

        internal static AzureEntity FromATOMFeed(XElement entryElement)
        {

            var id = entryElement.Element(Namespaces.Atom + "id").Value;
            var updated = DateTime.Parse(entryElement.Element(Namespaces.Atom + "updated").Value);
            var category = entryElement.Element(Namespaces.Atom + "category").Attribute("term").Value;

            var linkElement = entryElement.Element(Namespaces.Atom + "link");
            var link = new Link();
            link.HRef = (string)linkElement.Attribute("href");
            link.Rel = (string)linkElement.Attribute("rel");
            link.Title = (string)linkElement.Attribute("title");

            var properties = entryElement.Element(Namespaces.Atom + "content").Element(Namespaces.DataServicesMeta + "properties");

            string partitionKey = null, rowKey = null;
            AzureFieldCollection fields = new AzureFieldCollection();

            foreach (var prop in properties.Elements())
            {
                if (prop.Name == Namespaces.DataServices + "PartitionKey")
                {
                    partitionKey = prop.Value;
                }
                else if (prop.Name == Namespaces.DataServices + "RowKey")
                {
                    rowKey = prop.Value;
                }
                else
                {
                    fields.Add(AzureField.FromATOMFeed(prop));
                }
            }

            var entity = new AzureEntity(partitionKey, rowKey, fields)
            {
                ID = id,
                LastUpdated = updated,
                Category = category,
                Link = link
            };

            return entity;
        }
    }
}
