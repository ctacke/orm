using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace OpenNETCF.ORM
{
    public interface IEntityInfo
    {
        Type EntityType { get; }
        FieldAttributeCollection Fields { get; }
        ReferenceAttributeCollection References { get; }
        EntityAttribute EntityAttribute { get; }
        string EntityName { get; } 
    }
}
