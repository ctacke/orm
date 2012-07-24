using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace OpenNETCF.ORM
{
    public enum ReferenceType
    {
        OneToMany,
        ManyToMany,
        ManyToOne
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class ReferenceAttribute : Attribute, IEquatable<ReferenceAttribute>
    {
        public Type ReferenceEntityType { get; set; }
        public string ReferenceField { get; set; }
        public bool Autofill { get; set; }
        public PropertyInfo PropertyInfo { get; internal set; }
        public bool CascadeDelete { get; set; }
        public ReferenceType ReferenceType { get; set; }

        public ReferenceAttribute(Type referenceEntityType, string referenceField)
        {
            ReferenceEntityType = referenceEntityType;
            ReferenceField = referenceField;
            Autofill = false;
            ReferenceType = ReferenceType.OneToMany;
        }

        public bool Equals(ReferenceAttribute other)
        {
            if (!this.ReferenceEntityType.Equals(other.ReferenceEntityType)) return false;
            return string.Compare(this.ReferenceField, other.ReferenceField, StringComparison.InvariantCultureIgnoreCase) == 0;
        }

        public override int GetHashCode()
        {
            return PropertyInfo.Name.GetHashCode() | ReferenceEntityType.Name.GetHashCode() | ReferenceField.GetHashCode();
        }
    }
}
