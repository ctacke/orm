#if WINDOWS_PHONE
using HASH = System.Security.Cryptography.AesManaged;
#else
using HASH = System.Security.Cryptography.MD5;
#endif

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
        private HASH m_hash;
        private Dictionary<string, ReferenceAttribute> m_references = new Dictionary<string, ReferenceAttribute>();

        internal ReferenceAttributeCollection()
        {
#if WINDOWS_PHONE
            m_hash = new HASH();
#else
            m_hash = HASH.Create();
#endif
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
