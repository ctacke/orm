using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Data;

namespace OpenNETCF.ORM
{
    public class EntityInfo : IEntityInfo
    {
        public EntityInfo()
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
        public EntityCreatorDelegate CreateProxy { get; set; }

        public string EntityName 
        {
            get
            {
                return EntityAttribute.NameInStore;
            }
            internal set
            {
                EntityAttribute.NameInStore = value;
            }
        }

        public override string ToString()
        {
            return EntityName;
        }

        public void AddField(FieldAttribute field)
        {
            Fields.Add(field);
        }
    }
}
