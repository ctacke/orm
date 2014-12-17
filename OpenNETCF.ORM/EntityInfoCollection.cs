using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace OpenNETCF.ORM
{
    public class EntityInfoCollection : IEnumerable<IEntityInfo>
    {
        private Dictionary<string, IEntityInfo> m_entities = new Dictionary<string, IEntityInfo>(StringComparer.InvariantCultureIgnoreCase);
        private Dictionary<Type, string> m_typeToNameMap = new Dictionary<Type, string>();
        private object m_sycRoot = new object();

        internal EntityInfoCollection()
        {
        }

        public object SyncRoot
        {
            get { return m_sycRoot; }
        }

        internal void Add(IEntityInfo map)
        {
            string key = map.EntityName.ToLower();

            lock (m_sycRoot)
            {
                // check for dupes
                if (!m_entities.ContainsKey(key))
                {
                    m_entities.Add(key, map);
                }

                // dynamic entities have no underlying EntityType
                if (map.EntityType != typeof(DynamicEntityDefinition))
                {
                    if (!m_typeToNameMap.ContainsKey(map.EntityType))
                    {
                        m_typeToNameMap.Add(map.EntityType, map.EntityName);
                    }
                }
            }
        }

        public void Remove(string entityName)
        {
            lock (m_sycRoot)
            {
                if (m_entities.ContainsKey(entityName))
                {
                    m_entities.Remove(entityName);
                }
                foreach(var t in m_typeToNameMap.ToArray())
                {
                    if(t.Value == entityName)
                    {
                        m_typeToNameMap.Remove(t.Key);
                    }
                }
            }
        }

        public string GetNameForType(Type type)
        {
            lock (m_sycRoot)
            {
                if (!m_typeToNameMap.ContainsKey(type)) return null;

                return m_typeToNameMap[type];
            }
        }

        public IEnumerator<IEntityInfo> GetEnumerator()
        {
            lock (m_sycRoot)
            {
                return m_entities.Values.GetEnumerator();
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            lock (m_sycRoot)
            {
                return m_entities.Values.GetEnumerator();
            }
        }

        public IEntityInfo this[string entityName]
        {
            get 
            {
                lock (m_sycRoot)
                {
                    if (!m_entities.ContainsKey(entityName)) return null;

                    return m_entities[entityName.ToLower()];
                }
            }
            internal set
            {
                lock (m_sycRoot)
                {
                    m_entities[entityName.ToLower()] = value;
                }
            }
        }

        public bool Contains(string entityName)
        {
            lock (m_sycRoot)
            {
                return m_entities.ContainsKey(entityName);
            }
        }

        public IEntityInfo[] ToArray()
        {
            lock (m_sycRoot)
            {
                return m_entities.Values.ToArray();
            }
        }
    }
}
