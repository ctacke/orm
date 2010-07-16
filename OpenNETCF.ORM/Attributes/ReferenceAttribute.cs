using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace OpenNETCF.ORM
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ReferenceAttribute : Attribute, IEquatable<ReferenceAttribute>
    {
        private Type m_entityType = null;

        public string ReferenceField { get; set; }
        public bool Autofill { get; set; }
        public PropertyInfo PropertyInfo { get; internal set; }

        public ReferenceAttribute(string referenceField)
        {
            ReferenceField = referenceField;
            Autofill = false;
        }

        public Type ReferenceEntityType
        {
            get
            {
                if (m_entityType == null)
                {
                    m_entityType = PropertyInfo.PropertyType.GetGenericArguments()[0];

                }

                return m_entityType;
            }
        }

        public bool Equals(ReferenceAttribute other)
        {
            if (!this.ReferenceEntityType.Equals(other.ReferenceEntityType)) return false;
            return string.Compare(this.ReferenceField, other.ReferenceField, true) == 0;
        }
    }
}
