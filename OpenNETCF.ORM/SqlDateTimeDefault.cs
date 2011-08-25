using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Reflection;

namespace OpenNETCF.ORM
{
    public class SqlDateTimeDefault : IDefaultValue
    {
        public DefaultType DefaultType
        {
            get { return ORM.DefaultType.CurrentDateTime; }
        }

        public object GetDefaultValue()
        {
            return "GETDATE()";
        }
    }
}
