using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenNETCF.ORM
{
    public class DynamicEntity : ICloneable
    {
        public string EntityName { get; set; }
        public FieldCollection Fields { get; private set; }
        public string KeyField { get; set; }

        public DynamicEntity()
            : this(null, null)
        {
        }

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

                    if (f.IsPrimaryKey)
                    {
                        this.KeyField = f.FieldName;
                    }
                }
            }
        }

        public object Clone()
        {
            return new DynamicEntity(EntityName)
            {
                Fields = this.Fields.Clone() as FieldCollection,
                KeyField = this.KeyField
            };
        }
    }
}
