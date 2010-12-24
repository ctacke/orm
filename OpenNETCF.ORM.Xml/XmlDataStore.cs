using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Xml;

namespace OpenNETCF.ORM.Xml
{
    public class XmlDataStore : DataStore<XmlEntityInfo>, IDisposable
    {
        private Dictionary<Type, XmlSerializer> m_serializerCache = new Dictionary<Type, XmlSerializer>();
        private XDocument m_doc;
        private XElement m_root;

        public string FileName { get; private set; }
 
        public XmlDataStore(string fileName)
        {
            FileName = fileName;
        }

        public void Dispose()
        {
        }

        public override void CreateStore()
        {
            if (StoreExists)
            {
                throw new StoreAlreadyExistsException();
            }

            
            m_doc = new XDocument(new XDeclaration("1.0", "UTF-8", "yes"));

            m_root = new XElement("OrmData");
            m_doc.Add(m_root);

            foreach (var entity in this.Entities)
            {
                entity.EntityNode = CreateEntityNode(m_root, entity);
            }

            m_doc.Save(FileName);
        }

        private XElement CreateEntityNode(XElement root, XmlEntityInfo entity)
        {
            var node = new XElement("Entity",
                        new XAttribute("Name", entity.EntityName),
                        new XAttribute("Type", entity.EntityType)
                        );

            root.Add(node);

            return node;
        }

        public override void DeleteStore()
        {
            File.Delete(FileName);
        }

        public override bool StoreExists
        {
            get
            {
                return File.Exists(FileName);
            }
        }

        private XmlSerializer GetSerializer(Type type)
        {
            if (m_serializerCache.ContainsKey(type))
            {
                return m_serializerCache[type];
            }

            XmlSerializer serializer = new XmlSerializer(type);
            m_serializerCache.Add(type, serializer);
            return serializer;
        }

        public override void Insert(object item, bool insertReferences)
        {          
            XmlSerializer serializer = GetSerializer(item.GetType());

            string name = item.GetType().Name;
            
            StringBuilder sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                serializer.Serialize(writer, item);
            }

            var element = XElement.Parse(sb.ToString());
            Entities[name].EntityNode.Add(element);

            if (insertReferences)
            {
                // TODO:
            }

            // TODO: see if this is required.  If so, maybe add an attribute or method to delay flushing
            m_doc.Save(FileName);
        }
        
        public override T[] Select<T>()
        {
            var type = typeof(T);
            var typename = type.Name;

            List<T> instances = new List<T>();

            var nodes = (from e in m_root.Elements()
                        where e.Name == "Entity" && e.Attribute("Name").Value == typename
                        select e).FirstOrDefault().Elements();

            var serializer = GetSerializer(type);

            foreach (var node in nodes)
            {
                var instance = serializer.Deserialize(node.CreateReader());

                instances.Add((T)instance);
            }

            return instances.ToArray();
        }

        public override int Count<T>()
        {
            throw new NotImplementedException();
        }
        
        public override T Select<T>(object primaryKey)
        {
            throw new NotImplementedException();
        }

        public override T[] Select<T>(string searchFieldName, object matchValue)
        {
            throw new NotImplementedException();
        }

        public override void Update(object item)
        {
            throw new NotImplementedException();
        }

        public override void Delete(object item)
        {
            throw new NotImplementedException();
        }

        public override void Delete<T>(object primaryKey)
        {
            throw new NotImplementedException();
        }

        public override void Delete<T>()
        {
            throw new NotImplementedException();
        }

        public override void FillReferences(object instance)
        {
            throw new NotImplementedException();
        }

        public override T[] Fetch<T>(int fetchCount)
        {
            throw new NotImplementedException();
        }

        public override T[] Fetch<T>(int fetchCount, int firstRowOffset)
        {
            throw new NotImplementedException();
        }

        public override void Delete<T>(string fieldName, object matchValue)
        {
            throw new NotImplementedException();
        }

        public override bool Contains(object item)
        {
            throw new NotImplementedException();
        }

        public override object[] Select(Type entityType)
        {
            throw new NotImplementedException();
        }

        public override T[] Select<T>(IEnumerable<FilterCondition> filters)
        {
            throw new NotImplementedException();
        }

        public override void EnsureCompatibility()
        {
            throw new NotImplementedException();
        }

        public override T[] Select<T>(bool fillReferences)
        {
            throw new NotImplementedException();
        }

        public override T Select<T>(object primaryKey, bool fillReferences)
        {
            throw new NotImplementedException();
        }

        public override T[] Select<T>(string searchFieldName, object matchValue, bool fillReferences)
        {
            throw new NotImplementedException();
        }

        public override T[] Select<T>(IEnumerable<FilterCondition> filters, bool fillReferences)
        {
            throw new NotImplementedException();
        }

        public override object[] Select(Type entityType, bool fillReferences)
        {
            throw new NotImplementedException();
        }

        public override void Update(object item, bool cascadeUpdates, string fieldName)
        {
            throw new NotImplementedException();
        }

        public override void Update(object item, string fieldName)
        {
            throw new NotImplementedException();
        }

        public override T[] Fetch<T>(int fetchCount, int firstRowOffset, string sortField, FieldSearchOrder sortOrder, FilterCondition filter, bool fillReferences)
        {
            throw new NotImplementedException();
        }

        public override int Count<T>(IEnumerable<FilterCondition> filters)
        {
            throw new NotImplementedException();
        }

        public override T[] Fetch<T>(int fetchCount, int firstRowOffset, string sortField)
        {
            throw new NotImplementedException();
        }
    }
}
