using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenNETCF.ORM.Replication
{
    public sealed class ReplicatorCollection : IEnumerable<Replicator>
    {
        private List<Replicator> m_replicators;
        private IDataStore m_source;

        internal ReplicatorCollection(IDataStore source)
        {
            m_replicators = new List<Replicator>();
            m_source = source;
        }

        public void Add(Replicator replicator)
        {
            lock (m_replicators)
            {
                replicator.SetSource(m_source);
                m_replicators.Add(replicator);
            }

            replicator.Start();
        }

        public int Count
        {
            get { return m_replicators.Count; }
        }

        public IEnumerator<Replicator> GetEnumerator()
        {
            lock (m_replicators)
            {
                return m_replicators.GetEnumerator();
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
