using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenNETCF.ORM.SqlServer.Integration.Test
{
    [Entity(KeyScheme = KeyScheme.GUID)]
    public class GuidItem
    {
        public GuidItem()
        {
            ID = Guid.NewGuid();
        }

        [Field(IsPrimaryKey = true)]
        public Guid ID { get; set; }

        [Field]
        public int? FieldA { get; set; }
        [Field]
        public int FieldB { get; set; }
    }
}
