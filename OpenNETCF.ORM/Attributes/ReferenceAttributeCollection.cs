using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Security.Cryptography;

namespace OpenNETCF.ORM
{
    public class ReferenceAttributeCollection : IEnumerable<ReferenceAttribute>
    {
        private MD5 m_hash;
        private Dictionary<string, ReferenceAttribute> m_references = new Dictionary<string, ReferenceAttribute>();

        internal ReferenceAttributeCollection()
        {
            m_hash = MD5.Create();
        }

        internal void Add(ReferenceAttribute reference)
        {
            m_references.Add(reference.GenerateHash(), reference);
        }

        public int Count
        {
            get { return m_references.Count; }
        }

        public ReferenceAttribute this[Type referenceType, string referenceName, string referenceFieldName]
        {
            get 
            {
                var hash = string.Format("{0}{1}{2}", referenceName, referenceType.Name, referenceFieldName);
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
