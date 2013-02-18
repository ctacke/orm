using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlServerCe;
using System.Data;
using System.Reflection;

namespace OpenNETCF.ORM
{
    partial class SqlCeDataStore
    {
        private object GetKeyValue(FieldAttribute field, object item)
        {
            if (item is DynamicEntity)
            {
                return ((DynamicEntity)item).Fields[field.FieldName];
            }
            else
            {
                return field.PropertyInfo.GetValue(item, null);
            }
        }

        public override void OnUpdate(object item, bool cascadeUpdates, string fieldName)
        {
            object keyValue;
            var itemType = item.GetType();

            string entityName;

            if (itemType.Equals(typeof(DynamicEntity)))
            {
                entityName = ((DynamicEntity)item).EntityName;
            }
            else
            {
                entityName = m_entities.GetNameForType(itemType);
            }

            if (entityName == null)
            {
                throw new EntityNotFoundException(itemType);
            }

            if (Entities[entityName].Fields.KeyField == null)
            {
                throw new PrimaryKeyRequiredException("A primary key is required on an Entity in order to perform Updates");
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
                    command.IndexName = ((SqlEntityInfo)Entities[entityName]).PrimaryKeyIndexName;
                    command.Transaction = CurrentTransaction as SqlCeTransaction;

                    using (var results = command.ExecuteResultSet(ResultSetOptions.Scrollable | ResultSetOptions.Updatable))
                    {
                        keyValue = GetKeyValue(Entities[entityName].Fields.KeyField, item);

                        // seek on the PK
                        var found = results.Seek(DbSeekOptions.BeforeEqual, new object[] { keyValue });

                        if (!found)
                        {
                            // TODO: the PK value has changed - we need to store the original value in the entity or diallow this kind of change
                            throw new RecordNotFoundException("Cannot locate a record with the provided primary key.  You cannot update a primary key value through the Update method");
                        }

                        results.Read();
                        FieldAttribute id;
                        FillEntity(results.SetValue, entityName, itemType, item, out id);

                        results.Update();
                    }
                }
            }
            finally
            {
                DoneWithConnection(connection, false);
            }

            if (cascadeUpdates)
            {
                // TODO: move this into the base DataStore class as it's not SqlCe-specific
                foreach (var reference in Entities[entityName].References)
                {
                    var itemList = reference.PropertyInfo.GetValue(item, null) as Array;
                    if (itemList != null)
                    {
                        foreach (var refItem in itemList)
                        {
                            var foreignKey = refItem.GetType().GetProperty(reference.ReferenceField, BindingFlags.Instance | BindingFlags.Public);
                            foreignKey.SetValue(refItem, keyValue, null);

                            if (!this.Contains(refItem))
                            {
                                Insert(refItem, false);
                            }
                            else
                            {
                                Update(refItem, true, fieldName);
                            }
                        }
                    }
                }
            }
        }
    }
}
