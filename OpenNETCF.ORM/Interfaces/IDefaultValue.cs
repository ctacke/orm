using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace OpenNETCF.ORM
{
    public interface IDefaultValue
    {
        DefaultType DefaultType { get; }
        object GetDefaultValue();
    }
}
