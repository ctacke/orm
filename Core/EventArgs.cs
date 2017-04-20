using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Diagnostics;

namespace OpenNETCF.ORM
{
    public class EntityTypeAddedArgs : EventArgs
    {
        internal EntityTypeAddedArgs(IEntityInfo info) 
        {
            EntityInfo = info;
        }

        public IEntityInfo EntityInfo { get; set; }
    }

    public class EntityUpdateArgs : EventArgs
    {
        internal EntityUpdateArgs(string entityName, object item, bool cascadeUpdates, string fieldName) 
        {
            EntityName = entityName;
            Item = item;
            CascadeUpdates = cascadeUpdates;
            FieldName = fieldName;
        }

        public string EntityName { get; set; }
        public object Item { get; set; }
        public bool CascadeUpdates { get; set; }
        public string FieldName { get; set; }
    }

    public class EntityInsertArgs : EventArgs
    {
        internal EntityInsertArgs(string entityName, object item, bool insertReferences)
        {
            EntityName = entityName;
            Item = item;
            InsertReferences = insertReferences;
        }

        public string EntityName { get; set; }
        public object Item { get; set; }
        public bool InsertReferences { get; set; }
    }

    public class EntityDeleteArgs : EventArgs
    {
        internal EntityDeleteArgs(string entityName, object item)
        {
            EntityName = entityName;
            Item = item;
        }

        public string EntityName { get; set; }
        public object Item { get; set; }
    }
}
