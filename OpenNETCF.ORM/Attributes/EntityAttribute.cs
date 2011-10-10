using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace OpenNETCF.ORM
{
    [AttributeUsage(AttributeTargets.Class)]
    public class EntityAttribute : Attribute
    {
        public EntityAttribute()
            : this(KeyScheme.None)
        {
        }

        public EntityAttribute(KeyScheme keyScheme)
        {
            KeyScheme = keyScheme;
        }

        public string NameInStore { get; set; }
        public KeyScheme KeyScheme { get; set; }
    }
}
