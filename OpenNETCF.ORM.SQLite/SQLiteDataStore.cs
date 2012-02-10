﻿using System;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Linq;
using System.Data;
using System.Data.SQLite;
using System.Data.Common;

namespace OpenNETCF.ORM.SQLite
{
    public class SQLiteDataStore : SQLStoreBase<SQLiteEntityInfo>, IDisposable
    {
        private string m_connectionString;

        public string FileName { get; protected set; }

        protected SQLiteDataStore()
            : base()
        {
//            UseCommandCache = true;
        }

        public SQLiteDataStore(string fileName)
            : this()
        {
            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentException();
            }

            FileName = fileName;
        }

        private string ConnectionString
        {
            get
            {
                if (m_connectionString == null)
                {
                    m_connectionString = string.Format("Data Source={0}", FileName);

                }
                return m_connectionString;
            }
        }

        protected override System.Data.Common.DbCommand GetNewCommandObject()
        {
            return new SQLiteCommand();
        }

        protected override System.Data.Common.DbConnection GetNewConnectionObject()
        {
            return new SQLiteConnection(ConnectionString);
        }

        protected override string AutoIncrementFieldIdentifier
        {
            get { return "AUTOINCREMENT"; }
        }

        public override void CreateStore()
        {
            if (StoreExists)
            {
                throw new StoreAlreadyExistsException();
            }

            SQLiteConnection.CreateFile(FileName);

            var connection = GetConnection(true);
            try
            {
                foreach (var entity in this.Entities)
                {
                    CreateTable(connection, entity);
                }
            }
            finally
            {
                DoneWithConnection(connection, true);
            }
        }

        public override void DeleteStore()
        {
            if (StoreExists)
            {
                File.Delete(FileName);
            }
        }

        public override bool StoreExists
        {
            get { return File.Exists(FileName); }
        }

        private SQLiteCommand GetInsertCommand(string entityName)
        {
            // TODO: support command caching to improve bulk insert speeds
            //       simply use a dictionary keyed by entityname
            var keyScheme = Entities[entityName].EntityAttribute.KeyScheme;
            var insertCommand = new SQLiteCommand();

            var sbFields = new StringBuilder(string.Format("INSERT INTO {0} (", entityName));
            var sbParams = new StringBuilder( " VALUES (");

            foreach (var field in Entities[entityName].Fields)
            {
                // skip auto-increments
                if ((field.IsPrimaryKey) && (keyScheme == KeyScheme.Identity))
                {
                    continue;
                }
                sbFields.Append("[" + field.FieldName + "],");
                sbParams.Append("?,");

                insertCommand.Parameters.Add(new SQLiteParameter(field.FieldName));
            }

            // replace trailing commas
            sbFields[sbFields.Length - 1] = ')';
            sbParams[sbParams.Length - 1] = ')';

            insertCommand.CommandText = sbFields.ToString() + sbParams.ToString();

            return insertCommand;
        }

        /// <summary>
        /// Inserts the provided entity instance into the underlying data store.
        /// </summary>
        /// <param name="item"></param>
        /// <remarks>
        /// If the entity has an identity field, calling Insert will populate that field with the identity vale vefore returning
        /// </remarks>
        public override void Insert(object item, bool insertReferences)
        {
            var itemType = item.GetType();
            string entityName = m_entities.GetNameForType(itemType);

            if (entityName == null)
            {
                throw new EntityNotFoundException(item.GetType());
            }

            var connection = GetConnection(false);
            try
            {
                //                CheckOrdinals(entityName);

                FieldAttribute identity = null;
                var command = GetInsertCommand(entityName);
                command.Connection = connection as SQLiteConnection;

                var keyScheme = Entities[entityName].EntityAttribute.KeyScheme;

                // TODO: fill the parameters
                foreach (var field in Entities[entityName].Fields)
                {
                    if ((field.IsPrimaryKey) && (keyScheme == KeyScheme.Identity))
                    {
                        identity = field;
                        continue;
                    }
                    else if (field.DataType == DbType.Object)
                    {
                        // get serializer
                        var serializer = GetSerializer(itemType);

                        if (serializer == null)
                        {
                            throw new MissingMethodException(
                                string.Format("The field '{0}' requires a custom serializer/deserializer method pair in the '{1}' Entity",
                                field.FieldName, entityName));
                        }
                        var value = serializer.Invoke(item, new object[] { field.FieldName });
                        if (value == null)
                        {
                            command.Parameters[field.FieldName].Value = DBNull.Value;
                        }
                        else
                        {
                            command.Parameters[field.FieldName].Value = value;
                        }
                    }
                    else if (field.IsRowVersion)
                    {
                        // read-only, so do nothing
                    }
                    else if (field.PropertyInfo.PropertyType.UnderlyingTypeIs<TimeSpan>())
                    {
                        // SQL Compact doesn't support Time, so we're convert to a DateTime both directions
                        var value = field.PropertyInfo.GetValue(item, null);

                        if (value == null)
                        {
                            command.Parameters[field.FieldName].Value = DBNull.Value;
                        }
                        else
                        {
                            var timespanTicks = ((TimeSpan)value).Ticks;
                            command.Parameters[field.FieldName].Value = timespanTicks;
                        }
                    }
                    else
                    {
                        var value = field.PropertyInfo.GetValue(item, null);
                        command.Parameters[field.FieldName].Value = value;
                    }
                }

                command.ExecuteNonQuery();

                // did we have an identity field?  If so, we need to update that value in the item
                if (identity != null)
                {
                    var id = GetIdentity(connection);
                    identity.PropertyInfo.SetValue(item, id, null);
                }

                if (insertReferences)
                {
                    // cascade insert any References
                    // do this last because we need the PK from above
                    foreach (var reference in Entities[entityName].References)
                    {
                        var valueArray = reference.PropertyInfo.GetValue(item, null);
                        if (valueArray == null) continue;

                        var fk = Entities[entityName].Fields[reference.ReferenceField].PropertyInfo.GetValue(item, null);

                        string et = null;

                        // we've already enforced this to be an array when creating the store
                        foreach (var element in valueArray as Array)
                        {
                            if (et == null)
                            {
                                et = m_entities.GetNameForType(element.GetType());
                            }

                            // get the FK value
                            var keyValue = Entities[et].Fields.KeyField.PropertyInfo.GetValue(element, null);

                            bool isNew = false;


                            // only do an insert if the value is new (i.e. need to look for existing reference items)
                            // not certain how this will work right now, so for now we ask the caller to know what they're doing
                            switch (keyScheme)
                            {
                                case KeyScheme.Identity:
                                    // TODO: see if PK field value == -1
                                    isNew = keyValue.Equals(-1);
                                    break;
                                case KeyScheme.GUID:
                                    // TODO: see if PK field value == null
                                    isNew = keyValue.Equals(null);
                                    break;
                            }

                            if (isNew)
                            {
                                Entities[et].Fields[reference.ReferenceField].PropertyInfo.SetValue(element, fk, null);
                                Insert(element);
                            }
                        }
                    }
                }
            }
            finally
            {
                DoneWithConnection(connection, false);
            }
        }

        private int GetIdentity(DbConnection connection)
        {
            using (var command = new SQLiteCommand("SELECT last_insert_rowid()", connection as SQLiteConnection))
            {
                object id = command.ExecuteScalar();
                return Convert.ToInt32(id);
            }
        }

        public override void EnsureCompatibility()
        {
            throw new NotImplementedException();
        }

        public override T[] Select<T>()
        {
            throw new NotImplementedException();
        }

        public override T[] Select<T>(bool fillReferences)
        {
            throw new NotImplementedException();
        }

        public override T Select<T>(object primaryKey)
        {
            throw new NotImplementedException();
        }

        public override T Select<T>(object primaryKey, bool fillReferences)
        {
            throw new NotImplementedException();
        }

        public override T[] Select<T>(string searchFieldName, object matchValue)
        {
            throw new NotImplementedException();
        }

        public override T[] Select<T>(string searchFieldName, object matchValue, bool fillReferences)
        {
            throw new NotImplementedException();
        }

        public override T[] Select<T>(System.Collections.Generic.IEnumerable<FilterCondition> filters)
        {
            throw new NotImplementedException();
        }

        public override T[] Select<T>(System.Collections.Generic.IEnumerable<FilterCondition> filters, bool fillReferences)
        {
            throw new NotImplementedException();
        }

        public override object[] Select(Type entityType)
        {
            throw new NotImplementedException();
        }

        public override object[] Select(Type entityType, bool fillReferences)
        {
            throw new NotImplementedException();
        }

        public override void Update(object item)
        {
            throw new NotImplementedException();
        }

        public override void Update(object item, bool cascadeUpdates, string fieldName)
        {
            throw new NotImplementedException();
        }

        public override void Update(object item, string fieldName)
        {
            throw new NotImplementedException();
        }

        public override void Delete(object item)
        {
            throw new NotImplementedException();
        }

        public override void Delete<T>(object primaryKey)
        {
            throw new NotImplementedException();
        }

        public override void FillReferences(object instance)
        {
            throw new NotImplementedException();
        }

        public override T[] Fetch<T>(int fetchCount)
        {
            throw new NotImplementedException();
        }

        public override T[] Fetch<T>(int fetchCount, int firstRowOffset)
        {
            throw new NotImplementedException();
        }

        public override T[] Fetch<T>(int fetchCount, int firstRowOffset, string sortField)
        {
            throw new NotImplementedException();
        }

        public override T[] Fetch<T>(int fetchCount, int firstRowOffset, string sortField, FieldSearchOrder sortOrder, FilterCondition filter, bool fillReferences)
        {
            throw new NotImplementedException();
        }

        public override int Count<T>()
        {
            throw new NotImplementedException();
        }

        public override int Count<T>(System.Collections.Generic.IEnumerable<FilterCondition> filters)
        {
            throw new NotImplementedException();
        }

        public override void Delete<T>()
        {
            throw new NotImplementedException();
        }

        public override void Delete<T>(string fieldName, object matchValue)
        {
            throw new NotImplementedException();
        }

        public override bool Contains(object item)
        {
            throw new NotImplementedException();
        }

    }
}
