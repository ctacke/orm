using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenNETCF.ORM
{
    public class FieldCollection : IEnumerable<FieldValue>, ICloneable
    {
        private Dictionary<string, FieldValue> m_fields;

        internal FieldCollection()
        {
            m_fields = new Dictionary<string, FieldValue>(StringComparer.InvariantCultureIgnoreCase);
        }

        public object Clone()
        {
            return new FieldCollection()
            {
                m_fields = this.m_fields.ToDictionary(e => e.Key, e => e.Value.Clone() as FieldValue, m_fields.Comparer)
            };
        }

        public int Count
        {
            get { return m_fields.Count; }
        }

        public object this[string fieldName]
        {
            get
            {
                lock (m_fields)
                {
                    if (!m_fields.ContainsKey(fieldName))
                    {
                        return null;
                    }
                    return m_fields[fieldName].Value;
                }
            }
            set 
            {
                lock (m_fields)
                {
                    if (!m_fields.ContainsKey(fieldName))
                    {
                        m_fields.Add(fieldName, new FieldValue(fieldName, value));
                    }
                    else
                    {
                        m_fields[fieldName] = new FieldValue(fieldName, value);
                    }
                }
            }
        }

        public bool ContainsField(string fieldName)
        {
            return m_fields.ContainsKey(fieldName);
        }

        public void Add(string fieldName)
        {
            Add(fieldName, null);
        }

        public void Add(string fieldName, object value)
        {
            lock (m_fields)
            {
                m_fields.Add(fieldName, new FieldValue(fieldName, value));
            }
        }

        public IEnumerator<FieldValue> GetEnumerator()
        {
            return m_fields.Values.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
