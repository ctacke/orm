using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace OpenNETCF.ORM
{
    public class EntityInfoCollection<TEntityInfo> : IEnumerable<TEntityInfo>
        where TEntityInfo : EntityInfo
    {
        private Dictionary<string, TEntityInfo> m_entities = new Dictionary<string, TEntityInfo>();
        private Dictionary<Type, string> m_typeToNameMap = new Dictionary<Type, string>();

        internal EntityInfoCollection()
        {
        }

        internal void Add(TEntityInfo map)
        {
            string key = map.EntityName.ToLower();

            // check for dupes
            if (m_entities.ContainsKey(key)) return;

            m_entities.Add(key, map);
            m_typeToNameMap.Add(map.EntityType, map.EntityName);
        }

        public string GetNameForType(Type type)
        {
            if (!m_typeToNameMap.ContainsKey(type)) return null;

            return m_typeToNameMap[type];
        }

        public IEnumerator<TEntityInfo> GetEnumerator()
        {
            return m_entities.Values.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return m_entities.Values.GetEnumerator();
        }

        public TEntityInfo this[string entityName]
        {
            get { return m_entities[entityName.ToLower()]; }
        }
    }
}
