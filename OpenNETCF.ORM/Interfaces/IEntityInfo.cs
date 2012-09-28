using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Data;

namespace OpenNETCF.ORM
{
    public delegate object EntityCreatorDelegate(FieldAttributeCollection fields, IDataReader results);

    public interface IEntityInfo
    {
        Type EntityType { get; }
        FieldAttributeCollection Fields { get; }
        ReferenceAttributeCollection References { get; }
        EntityAttribute EntityAttribute { get; }
        string EntityName { get; }

        EntityCreatorDelegate CreateProxy { get; set; }
    }
}
