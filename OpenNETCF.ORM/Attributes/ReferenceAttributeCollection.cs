using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace OpenNETCF.ORM
{
    public class ReferenceAttributeCollection : IEnumerable<ReferenceAttribute>
    {
        private Dictionary<string, ReferenceAttribute> m_references = new Dictionary<string, ReferenceAttribute>();

        internal ReferenceAttributeCollection()
        {
        }

        internal void Add(ReferenceAttribute reference)
        {
            m_references.Add(reference.ReferenceField.ToLower(), reference);
        }

        public int Count
        {
            get { return m_references.Count; }
        }

        public ReferenceAttribute this[string referenceName]
        {
            get { return m_references[referenceName.ToLower()]; }
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
