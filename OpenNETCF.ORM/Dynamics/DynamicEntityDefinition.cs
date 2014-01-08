using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenNETCF.ORM
{
    public class DynamicEntityDefinition : SqlEntityInfo, ICloneable
    {
        public DynamicEntityDefinition()
            : this(null, null)
        {
        }

        public DynamicEntityDefinition(string entityName, IEnumerable<FieldAttribute> fields)
            : this(entityName, fields, KeyScheme.None)
        {
        }

        public DynamicEntityDefinition(string entityName, IEnumerable<FieldAttribute> fields, KeyScheme keyScheme)
        {
            var entityAttribute = new EntityAttribute(keyScheme);
            entityAttribute.NameInStore = entityName;

            this.EntityType = typeof(DynamicEntityDefinition);
            this.Initialize(entityAttribute, this.EntityType);
            this.Fields.AddRange(fields);
        }

        private DynamicEntityDefinition(DynamicEntityDefinition source)
        {
            var entityAttribute = new EntityAttribute(source.EntityAttribute.KeyScheme);
            entityAttribute.NameInStore = source.EntityAttribute.NameInStore;

            this.EntityType = typeof(DynamicEntityDefinition);
            this.Initialize(entityAttribute, this.EntityType);

            // TODO make a copy of these?
            this.Fields.AddRange(source.Fields);
        }

        object ICloneable.Clone()
        {
            return this.Clone();
        }

        public DynamicEntityDefinition Clone()
        {
            return new DynamicEntityDefinition(this);
        }
    }
}
