using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace OpenNETCF.ORM
{
    public class StoreAlreadyExistsException : Exception
    {
        public StoreAlreadyExistsException()
            : base("Selected store already exists")
        {
        }
    }

    public class StoreNotFoundException : Exception
    {
        public StoreNotFoundException()
            : base("Selected store does not exist.  Have you called CreateStore?")
        {
        }
    }

    public class ReservedWordException : Exception
    {
        public ReservedWordException(string word)
            : base(string.Format("'{0}' is a reserved word.  It cannot be used for an Entity or Field name. Rename the entity or adjust its attributes.", word))
        {
        }
    }

    public class EntityNotFoundException : Exception
    {
        public EntityNotFoundException(Type type)
            : base(string.Format("Entity Type '{0}' not found. Is your Store up to date?", type.Name))
        {
        }
    }

    public class MutiplePrimaryKeyException : Exception
    {
        public MutiplePrimaryKeyException(string existingKeyName)
            : base(string.Format("The field '{0}' is already defined as the primary key for this entity.  Only one primary key is allowed per entity.", existingKeyName))
        {
        }
    }

    public class PrimaryKeyRequiredException : Exception
    {
        public PrimaryKeyRequiredException(string message)
            : base(message)
        {
        }
    }

    public class RecordNotFoundException : Exception
    {
        public RecordNotFoundException(string message)
            : base(message)
        {
        }
    }

    public class ReferenceFieldNotFoundException : Exception
    {
        public ReferenceFieldNotFoundException(Type referenceType, string referenceField)
            : base(string.Format("The refefrence type '{0}' doesn't contain a reference field named '{1}'.", referenceType.Name, referenceField))
        {
        }
    }

    public class InvalidReferenceTypeException : Exception
    {
        public Type ReferenceType { get; private set; }
        public string ReferenceField { get; private set; }

        public InvalidReferenceTypeException(Type referenceType, string referenceField, string message)
            : base(message)
        {
            ReferenceType = referenceType;
            ReferenceField = referenceField;
        }
    }

    public class SearchOrderRequiredException : Exception
    {
        public SearchOrderRequiredException(string entityName, string fieldName)
            : base(string.Format("The Entity '{0}' requires a SearchOrder attribute on the Field '{1}'.", entityName, fieldName))
        {
        }
    }

    public class FieldDefinitionException : Exception
    {
        public FieldDefinitionException(string entityName, string fieldName, string message)
            : base(string.Format("Invalid field definition data for '{0}.{1}': {2}", entityName, fieldName, message))
        {
        }
    }

    public class FieldNotFoundException : Exception
    {
        public FieldNotFoundException(string message)
            : base(message)
        {
        }
    }

    public class EntityDefinitionException : Exception
    {
        public string EntityName { get; private set; }

        public EntityDefinitionException(string entityName, string message)
            : base(message)
        {
            EntityName = entityName;
        }
    }
}
