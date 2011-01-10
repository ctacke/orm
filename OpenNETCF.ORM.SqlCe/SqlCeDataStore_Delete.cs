using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlServerCe;
using System.Data;

namespace OpenNETCF.ORM
{
    partial class SqlCeDataStore
    {
        public override void Delete<T>(string fieldName, object matchValue)
        {
            Delete(typeof(T), fieldName, matchValue);
        }

        /// <summary>
        /// Deletes entities of a given type where the specified field name matches a specified value
        /// </summary>
        /// <param name="t"></param>
        /// <param name="indexName"></param>
        /// <param name="matchValue"></param>
        private void Delete(Type entityType, string fieldName, object matchValue)
        {
            string entityName = m_entities.GetNameForType(entityType);

            var connection = GetConnection(true);
            try
            {
                using (var command = new SqlCeCommand())
                {
                    command.Connection = connection as SqlCeConnection;
                    command.CommandText = string.Format("DELETE FROM {0} WHERE {1} = ?", entityName, fieldName);
                    command.Parameters.Add("@val", matchValue);
                    command.ExecuteNonQuery();
                }
            }
            finally
            {
                DoneWithConnection(connection, true);
            }
        }

        private void Delete(Type t, object primaryKey)
        {
            string entityName = m_entities.GetNameForType(t);

            if (entityName == null)
            {
                throw new EntityNotFoundException(t);
            }

            if (Entities[entityName].Fields.KeyField == null)
            {
                throw new PrimaryKeyRequiredException("A primary key is required on an Entity in order to perform a Delete");
            }

            // handle cascade deletes
            foreach (var reference in Entities[entityName].References)
            {
                if (!reference.CascadeDelete) continue;

                Delete(reference.ReferenceEntityType, reference.ReferenceField, primaryKey);
            }

            var connection = GetConnection(false);
            try
            {
                CheckOrdinals(entityName);
                CheckPrimaryKeyIndex(entityName);

                using (var command = new SqlCeCommand())
                {
                    command.Connection = connection as SqlCeConnection;
                    command.CommandText = entityName;
                    command.CommandType = CommandType.TableDirect;
                    command.IndexName = Entities[entityName].PrimaryKeyIndexName;

                    using (var results = command.ExecuteResultSet(ResultSetOptions.Scrollable | ResultSetOptions.Updatable))
                    {

                        // seek on the PK
                        var found = results.Seek(DbSeekOptions.BeforeEqual, new object[] { primaryKey });

                        if (!found)
                        {
                            throw new RecordNotFoundException("Cannot locate a record with the provided primary key.  Unable to delete the item");
                        }

                        results.Read();
                        results.Delete();
                    }
                }
            }
            finally
            {
                DoneWithConnection(connection, false);
            }
        }

        /// <summary>
        /// Deletes an entity instance with the specified primary key from the DataStore
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="primaryKey"></param>
        public override void Delete<T>(object primaryKey)
        {
            Delete(typeof(T), primaryKey);
        }

        /// <summary>
        /// Deletes all entity instances of the specified type from the DataStore
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public override void Delete<T>()
        {
            var t = typeof(T);
            string entityName = m_entities.GetNameForType(t);

            if (entityName == null)
            {
                throw new EntityNotFoundException(t);
            }

            // TODO: handle cascade deletes?

            var connection = GetConnection(true);
            try
            {
                using (var command = new SqlCeCommand())
                {
                    command.Connection = connection as SqlCeConnection;
                    command.CommandText = string.Format("DELETE FROM {0}", entityName);
                    command.ExecuteNonQuery();
                }
            }
            finally
            {
                DoneWithConnection(connection, true);
            }
        }

        /// <summary>
        /// Deletes the specified entity instance from the DataStore
        /// </summary>
        /// <param name="item"></param>
        /// <remarks>
        /// The instance provided must have a valid primary key value
        /// </remarks>
        public override void Delete(object item)
        {
            var type = item.GetType();
            string entityName = m_entities.GetNameForType(type);

            if (entityName == null)
            {
                throw new EntityNotFoundException(type);
            }

            if (Entities[entityName].Fields.KeyField == null)
            {
                throw new PrimaryKeyRequiredException("A primary key is required on an Entity in order to perform a Delete");
            }
            var keyValue = Entities[entityName].Fields.KeyField.PropertyInfo.GetValue(item, null);

            Delete(type, keyValue);
        }
    }
}
