using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace OpenNETCF.ORM
{
    public class SqlFilterCondition : FilterCondition
    {
        public bool PrimaryKey { get; set; }
    }
}
