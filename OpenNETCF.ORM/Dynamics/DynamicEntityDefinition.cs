using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenNETCF.ORM
{
    public class DynamicEntityDefinition : SqlEntityInfo
    {
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

    }
}
