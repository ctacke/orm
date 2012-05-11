using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace OpenNETCF.ORM
{
    public class EntityInfo : IEntityInfo
    {
        protected EntityInfo()
        {
            Fields = new FieldAttributeCollection();
            References = new ReferenceAttributeCollection();
        }

        internal void Initialize(EntityAttribute entityAttribute, Type entityType)
        {
            EntityAttribute = entityAttribute;
            EntityType = entityType;
        }

        public Type EntityType { get; protected set; }

        public FieldAttributeCollection Fields { get; private set; }
        public ReferenceAttributeCollection References { get; private set; }

        public EntityAttribute EntityAttribute { get; set; }

        public string EntityName 
        {
            get
            {
                return EntityAttribute.NameInStore;
            }
        }

        public override string ToString()
        {
            return EntityName;
        }

        protected void AddField(FieldAttribute field)
        {
            Fields.Add(field);
        }
    }
}
