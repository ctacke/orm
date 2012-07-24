using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenNETCF.ORM
{
    public class DynamicEntity
    {
        public string EntityName { get; private set; }
        public FieldCollection Fields { get; private set; }

        public DynamicEntity(string entityName)
            : this(entityName, null)
        {
        }

        public DynamicEntity(string entityName, FieldAttributeCollection fields)
        {
            EntityName = entityName;
            Fields = new FieldCollection();

            if (fields != null)
            {
                foreach (var f in fields)
                {
                    this.Fields.Add(f.FieldName);
                }
            }
        }

    }
}
