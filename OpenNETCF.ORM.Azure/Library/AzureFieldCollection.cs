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
    public class AzureFieldCollection : IEnumerable<AzureField>
    {
        private OrderedDictionary<string, AzureField> m_fields;

        internal AzureFieldCollection()
        {
            m_fields = new OrderedDictionary<string, AzureField>(StringComparer.InvariantCultureIgnoreCase);
        }

        public AzureField this[int index]
        {
            get { return m_fields[index]; }
        }

        public AzureField this[string name]
        {
            get { return m_fields[name]; }
        }

        public void Add(string fieldName, object value)
        {
            Add(new AzureField(fieldName, value));
        }

        public void Add(AzureField field)
        {
            m_fields.Add(field.Name, field);
        }

        public int Count
        {
            get { return m_fields.Count; }
        }

        public IEnumerator<AzureField> GetEnumerator()
        {
            return m_fields.Values.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        internal static AzureFieldCollection FromATOMFeed(XElement feedElement)
        {
            throw new NotImplementedException();
        }
    }
}
