using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace OpenNETCF.ORM
{
    public class FieldAttributeCollection : IEnumerable<FieldAttribute>
    {
        private Dictionary<string, FieldAttribute> m_fields = new Dictionary<string, FieldAttribute>();

        public bool OrdinalsAreValid { get; set; }
        public FieldAttribute KeyField { get; private set; }

        internal FieldAttributeCollection()
        {
            OrdinalsAreValid = false;
            KeyField = null;
        }

        internal FieldAttributeCollection(IEnumerable<FieldAttribute> fields)
        {
            OrdinalsAreValid = false;
            KeyField = null;

            AddRange(fields);
        }

        internal void AddRange(IEnumerable<FieldAttribute> fields)
        {
            lock (m_fields)
            {
                foreach (var f in fields)
                {
                    Add(f);
                }
            }
        }

        internal void Add(FieldAttribute attribute)
        {
            lock (m_fields)
            {
                if (attribute.IsPrimaryKey)
                {
                    if (KeyField == null)
                    {
                        KeyField = attribute;
                    }
                    else
                    {
                        throw new MutiplePrimaryKeyException(KeyField.FieldName);
                    }
                }

                m_fields.Add(attribute.FieldName.ToLower(), attribute);
            }
        }

        public int Count
        {
            get { return m_fields.Count; }
        }

        public FieldAttribute this[string fieldName]
        {
            get { return m_fields[fieldName.ToLower()]; }
        }

        public IEnumerator<FieldAttribute> GetEnumerator()
        {
            return m_fields.Values.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return m_fields.Values.GetEnumerator();
        }
    }
}
