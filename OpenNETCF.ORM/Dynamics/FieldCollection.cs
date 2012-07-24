using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenNETCF.ORM
{
    public class FieldCollection : IEnumerable<FieldValue>
    {
        private Dictionary<string, FieldValue> m_fields;

        internal FieldCollection()
        {
            m_fields = new Dictionary<string, FieldValue>(StringComparer.InvariantCultureIgnoreCase);
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
                        throw new ArgumentException(string.Format("Field named '{0}' not present", fieldName));
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

        internal void Add(string fieldName)
        {
            Add(fieldName, null);
        }

        internal void Add(string fieldName, object value)
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
