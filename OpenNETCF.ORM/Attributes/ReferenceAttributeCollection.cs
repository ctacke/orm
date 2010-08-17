using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace OpenNETCF.ORM
{
    public class ReferenceAttributeCollection : IEnumerable<ReferenceAttribute>
    {
        private Dictionary<int, ReferenceAttribute> m_references = new Dictionary<int, ReferenceAttribute>();

        internal ReferenceAttributeCollection()
        {
        }

        internal void Add(ReferenceAttribute reference)
        {
            m_references.Add(reference.GetHashCode(), reference);
        }

        public int Count
        {
            get { return m_references.Count; }
        }

        public ReferenceAttribute this[Type referenceType, string referenceFieldName]
        {
            get 
            {
                int hash = referenceType.Name.GetHashCode() | referenceFieldName.GetHashCode();
                return m_references[hash]; 
            }
        }

        public IEnumerator<ReferenceAttribute> GetEnumerator()
        {
            return m_references.Values.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return m_references.Values.GetEnumerator();
        }
    }
}
