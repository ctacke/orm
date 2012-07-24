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

        internal EntityInfoCollection()
        {
        }

        internal void Add(IEntityInfo map)
        {
            string key = map.EntityName.ToLower();

            // check for dupes
            if (m_entities.ContainsKey(key)) return;

            m_entities.Add(key, map);
            
            // dynamic entities have no underlying EntityType
            if (map.EntityType != typeof(DynamicEntityDefinition))
            {
                m_typeToNameMap.Add(map.EntityType, map.EntityName);
            }
        }

        public string GetNameForType(Type type)
        {
            if (!m_typeToNameMap.ContainsKey(type)) return null;

            return m_typeToNameMap[type];
        }

        public IEnumerator<IEntityInfo> GetEnumerator()
        {
            return m_entities.Values.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return m_entities.Values.GetEnumerator();
        }

        public IEntityInfo this[string entityName]
        {
            get { return m_entities[entityName]; }
        }
    }
}
