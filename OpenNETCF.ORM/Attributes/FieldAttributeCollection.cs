using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace OpenNETCF.ORM
{
    public class FieldAttributeCollection : IEnumerable<FieldAttribute>, ICloneable
    {
        private Dictionary<string, FieldAttribute> m_fields = new Dictionary<string, FieldAttribute>(StringComparer.InvariantCultureIgnoreCase);

        private object m_syncRoot = new object();

        public bool OrdinalsAreValid { get; set; }
        public FieldAttribute KeyField { get; private set; }

        internal FieldAttributeCollection()
        {
            OrdinalsAreValid = false;
            KeyField = null;
        }

        public object SyncRoot
        {
            get { return m_syncRoot; }
        }

        public object Clone()
        {
            return new FieldAttributeCollection(m_fields.ToDictionary(e=>e.Key, e=>(FieldAttribute)e.Value.Clone()).Values);
        }

        internal FieldAttributeCollection(IEnumerable<FieldAttribute> fields)
        {
            OrdinalsAreValid = false;
            KeyField = null;

            AddRange(fields);
        }

        internal void AddRange(IEnumerable<FieldAttribute> fields)
        {
            lock (m_syncRoot)
            {
                foreach (var f in fields)
                {
                    Add(f);
                }
            }
        }

        internal void Add(FieldAttribute attribute)
        {
            Add(attribute, false);
        }

        internal void Add(FieldAttribute attribute, bool replaceKeyField)
        {
            lock (m_syncRoot)
            {
                if (attribute.IsPrimaryKey)
                {
                    if ((KeyField == null) || (replaceKeyField))
                    {
                        KeyField = attribute;
                    }
                    else
                    {
                        throw new MutiplePrimaryKeyException(KeyField.FieldName);
                    }
                }

                m_fields.Add(attribute.FieldName, attribute);
            }
        }

        public int Count
        {
            get
            {
                lock (m_syncRoot)
                {
                    return m_fields.Count;
                }
            }
        }

        public FieldAttribute this[string fieldName]
        {
            get
            {
                lock (m_syncRoot)
                {
                    return m_fields[fieldName.ToLower()];
                }
            }
        }

        public bool ContainsField(string fieldName)
        {
            lock (m_syncRoot)
            {
                return m_fields.ContainsKey(fieldName);
            }
        }

        public IEnumerator<FieldAttribute> GetEnumerator()
        {
            lock (m_syncRoot)
            {
                return m_fields.Values.GetEnumerator();
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return m_fields.Values.GetEnumerator();
        }
    }
}
