﻿using System;
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
        /// <summary>
        /// The type of the referenced Entity
        /// </summary>
        public Type ReferenceEntityType { get; set; }
        /// <summary>
        /// The name of the key Field in the referenced Entity (typically the Primary Key)
        /// </summary>
        public string ReferenceField { get; set; }
        public bool Autofill { get; set; }
        public PropertyInfo PropertyInfo { get; internal set; }
        public bool CascadeDelete { get; set; }

        private ReferenceType m_type;

        public ReferenceType ReferenceType 
        {
            get { return m_type; }
            set
            {
                if (value == ORM.ReferenceType.ManyToMany)
                {
                    throw new NotImplementedException();
                }

                m_type = value;
            }
        }

        /// <summary>
        /// The type of the Joining entity for many-to-many relationships
        /// </summary>
        public Type JoinEntitytype { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="referenceEntityType">The type of the referenced Entity (the other Entity, not this one)</param>
        /// <param name="referenceField">The name of the key Field in the referenced Entity (typically the Primary Key)</param>
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
    }
}
