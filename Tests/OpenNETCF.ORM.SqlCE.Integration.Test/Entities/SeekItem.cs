using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenNETCF.ORM.SqlCE.Integration.Test
{
    [Entity(KeyScheme.Identity)]
    class SeekItem
    {
        [Field(IsPrimaryKey = true)]
        public int ID { get; set; }

        [Field(SearchOrder=FieldSearchOrder.Ascending)]
        public int SeekField { get; set; }

        [Field]
        public string Data { get; set; }

    }
}
