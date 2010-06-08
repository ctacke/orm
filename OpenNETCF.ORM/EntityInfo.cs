using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace OpenNETCF.ORM
{
    public partial class EntityInfo
    {
        protected EntityInfo()
        {
            Fields = new FieldAttributeCollection();
            References = new ReferenceAttributeCollection();
        }

        internal void Initialize(string entityName, Type entityType)
        {
            EntityName = entityName;
            EntityType = entityType;
        }

        public string EntityName { get; protected set; }
        public Type EntityType { get; protected set; }

        public FieldAttributeCollection Fields { get; private set; }
        public ReferenceAttributeCollection References { get; private set; }

        public override string ToString()
        {
            return EntityName;
        }
    }
}
