using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace OpenNETCF.ORM.Replication
{
    internal class Registrations
    {
        private List<ReplicationNameRegistration> m_nameRegistrations;
        private List<ReplicationTypeRegistration> m_typeRegistrations;

        internal Registrations()
        {
            m_nameRegistrations = new List<ReplicationNameRegistration>();
            m_typeRegistrations = new List<ReplicationTypeRegistration>();
        }

        public void AddName(string localName, ReplicationPriority priority)
        {
            AddName(localName, null, priority);
        }
        
        public void AddName(string localName, string replicatedName, ReplicationPriority priority)
        {
            lock (m_nameRegistrations)
            {
                var existing = m_nameRegistrations.FirstOrDefault(r => string.Compare(r.LocalName, localName, StringComparison.InvariantCultureIgnoreCase) == 0);

                if (existing != null)
                {
                    existing.Priority = priority;
                }
                else
                {
                    m_nameRegistrations.Add(new ReplicationNameRegistration(localName, replicatedName, priority));
                }

                m_nameRegistrations.Sort(CompareNameRegistrationPriorities);
            }
        }


        private int CompareNameRegistrationPriorities(ReplicationNameRegistration regA, ReplicationNameRegistration regB)
        {
            return regA.Priority.CompareTo(regB.Priority);
        }

        public void RemoveName(string name)
        {
            lock (m_nameRegistrations)
            {
                var existing = m_nameRegistrations.FirstOrDefault(r => string.Compare(r.LocalName, name, StringComparison.InvariantCultureIgnoreCase) == 0);

                if (existing != null)
                {
                    m_nameRegistrations.Remove(existing);
                }
            }
        }

        public ReplicationNameRegistration GetRegistration(string name)
        {
            return m_nameRegistrations.FirstOrDefault(r => string.Compare(r.LocalName, name, StringComparison.InvariantCultureIgnoreCase) == 0);
        }

        public bool Contains(string name)
        {
            return GetRegistration(name) != null;
        }

        public IEnumerable<ReplicationNameRegistration> GetNameRegistrations(ReplicationPriority priority)
        {
            return m_nameRegistrations.Where(r => r.Priority == priority);
        }

        public void AddType(Type type, ReplicationPriority priority)
        {
            lock (m_typeRegistrations)
            {
                var existing = m_typeRegistrations.FirstOrDefault(r => r.Type.Equals(type));

                if (existing != null)
                {
                    existing.Priority = priority;
                }
                else
                {
                    m_typeRegistrations.Add(new ReplicationTypeRegistration(type, priority));
                }

                m_typeRegistrations.Sort(CompareTypeRegistrationPriorities);
            }
        }

        private int CompareTypeRegistrationPriorities(ReplicationTypeRegistration regA, ReplicationTypeRegistration regB)
        {
            return regA.Priority.CompareTo(regB.Priority);
        }

        public void RemoveType(Type type)
        {
            lock (m_typeRegistrations)
            {
                var existing = m_typeRegistrations.FirstOrDefault(r => r.Type.Equals(type));

                if (existing != null)
                {
                    m_typeRegistrations.Remove(existing);
                }
            }
        }

        public ReplicationTypeRegistration GetRegistration(Type type)
        {
            return m_typeRegistrations.FirstOrDefault(r => r.Type.Equals(type));
        }

        public bool Contains(Type type)
        {
            return GetRegistration(type) != null;
        }

        public IEnumerable<ReplicationTypeRegistration> GetTypeRegistrations(ReplicationPriority priority)
        {
            return m_typeRegistrations.Where(r => r.Priority == priority);
        }
    }

    internal class ReplicationNameRegistration
    {
        public ReplicationNameRegistration(string localName, ReplicationPriority priority)
            : this(localName, localName, priority)
        {
        }

        public ReplicationNameRegistration(string localName, string replicatedName, ReplicationPriority priority)
        {
            LocalName = localName;
            ReplicatedName = replicatedName ?? localName;
            Priority = priority;
        }

        public ReplicationPriority Priority { get; set; }
        public string LocalName { get; private set; }
        public string ReplicatedName { get; private set; }

    }

    internal class ReplicationTypeRegistration
    {
        public ReplicationTypeRegistration(Type type, ReplicationPriority priority)
        {
            Type = type;
            Priority = priority;
        }

        public ReplicationPriority Priority { get; set; }
        public Type Type { get; private set; }
    }
}
