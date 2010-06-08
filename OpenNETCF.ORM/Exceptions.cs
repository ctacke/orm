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
}
