using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Diagnostics;

namespace OpenNETCF.ORM
{
    public class EntityUpdateArgs : EventArgs
    {
        internal EntityUpdateArgs(object item, bool cascadeUpdates, string fieldName) 
        {
            Item = item;
            CascadeUpdates = cascadeUpdates;
            FieldName = fieldName;
        }

        public object Item { get; set; }
        public bool CascadeUpdates { get; set; }
        public string FieldName { get; set; }
    }

    public class EntityInsertArgs : EventArgs
    {
        internal EntityInsertArgs(object item, bool insertReferences)
        {
            Item = item;
            InsertReferences = insertReferences;
        }

        public object Item { get; set; }
        public bool InsertReferences { get; set; }
    }
}
