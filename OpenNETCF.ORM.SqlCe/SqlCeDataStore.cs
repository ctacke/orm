using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using System.Data;
using System.Data.SqlServerCe;
using System.IO;
using System.Runtime.InteropServices;

namespace OpenNETCF.ORM
{
    public class SqlCeDataStore : DataStore<SqlCeEntityInfo>, IDisposable
    {
        private string m_connectionString;
        private SqlCeConnection m_connection;

        private Dictionary<Type, object[]> m_referenceCache = new Dictionary<Type, object[]>();
        private Dictionary<Type, MethodInfo> m_serializerCache = new Dictionary<Type, MethodInfo>();
        private Dictionary<Type, MethodInfo> m_deserializerCache = new Dictionary<Type, MethodInfo>();

        private string Password { get; set; }

        public string FileName { get; private set; }
        public int DefaultStringFieldSize { get; set; }
        public int DefaultNumericFieldPrecision { get; set; }
        public ConnectionBehavior ConnectionBehavior { get; set; }

        public SqlCeDataStore(string fileName)
            : this(fileName, null)
        {
        }

        public SqlCeDataStore(string fileName, string password)
        {
            FileName = fileName;
            Password = password;
            DefaultStringFieldSize = 200;
            DefaultNumericFieldPrecision = 16;
        }

        ~SqlCeDataStore()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (m_connection != null)
            {
                m_connection.Dispose();
            }

            GC.SuppressFinalize(this);
        }

        public override bool StoreExists
        {
            get
            {
                return File.Exists(FileName);
            }
        }

        /// <summary>
        /// Deletes the underlying DataStore
        /// </summary>
        public override void DeleteStore()
        {
            File.Delete(FileName);
        }

        /// <summary>
        /// Creates the underlying DataStore
        /// </summary>
        public override void CreateStore()
        {
            if (StoreExists)
            {
                throw new StoreAlreadyExistsException();
            }

            // create the file
            using (SqlCeEngine engine = new SqlCeEngine(ConnectionString))
            {
                engine.CreateDatabase();
            }

            SqlCeConnection connection = GetConnection(true);
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

        private SqlCeConnection GetConnection(bool maintenance)
        {
            switch (ConnectionBehavior)
            {
                case ConnectionBehavior.AlwaysNew:
                    var connection = CreateConnection();
                    connection.Open();
                    return connection;
                case ConnectionBehavior.HoldMaintenance:
                    if (m_connection == null)
                    {
                        m_connection = CreateConnection();
                        m_connection.Open();
                    }
                    if (maintenance) return m_connection;
                    var connection2 = CreateConnection();
                    connection2.Open();
                    return connection2;
                case ConnectionBehavior.Persistent:
                    if (m_connection == null)
                    {
                        m_connection = CreateConnection();
                        m_connection.Open();
                    }
                    return m_connection;
                default:
                    throw new NotSupportedException();
            }
        }

        private void DoneWithConnection(SqlCeConnection connection, bool maintenance)
        {
            switch (ConnectionBehavior)
            {
                case ConnectionBehavior.AlwaysNew:
                    connection.Close();
                    connection.Dispose();
                    break;
                case ConnectionBehavior.HoldMaintenance:
                    if (maintenance) return;
                    connection.Close();
                    connection.Dispose();
                    break;
                case ConnectionBehavior.Persistent:
                    return;
                default:
                    throw new NotSupportedException();
            }
        }

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

            SqlCeConnection connection = GetConnection(true);
            try
            {
                using (var command = new SqlCeCommand())
                {
                    command.Connection = connection;
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

            SqlCeConnection connection = GetConnection(false);
            try
            {
                CheckOrdinals(entityName);
                CheckPrimaryKeyIndex(entityName);

                using (var command = new SqlCeCommand())
                {
                    command.Connection = connection;
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

            SqlCeConnection connection = GetConnection(true);
            try
            {
                using (var command = new SqlCeCommand())
                {
                    command.Connection = connection;
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

        private MethodInfo GetSerializer(Type itemType)
        {
            if (m_serializerCache.ContainsKey(itemType))
            {
                return m_serializerCache[itemType];
            }

            var serializer = itemType.GetMethod("Serialize", BindingFlags.Public | BindingFlags.Instance);

            if (serializer == null) return null;

            m_serializerCache.Add(itemType, serializer);
            return serializer;
        }

        private MethodInfo GetDeserializer(Type itemType)
        {
            if (m_deserializerCache.ContainsKey(itemType))
            {
                return m_deserializerCache[itemType];
            }

            var deserializer = itemType.GetMethod("Deserialize", BindingFlags.Public | BindingFlags.Instance);

            if (deserializer == null) return null;

            m_deserializerCache.Add(itemType, deserializer);
            return deserializer;
        }

        /// <summary>
        /// Updates the backing DataStore with the values in the specified entity instance
        /// </summary>
        /// <param name="item"></param>
        /// <remarks>
        /// The instance provided must have a valid primary key value
        /// </remarks>
        public override void Update(object item)
        {
            var itemType = item.GetType();
            string entityName = m_entities.GetNameForType(itemType);

            if (entityName == null)
            {
                throw new EntityNotFoundException(itemType);
            }

            if (Entities[entityName].Fields.KeyField == null)
            {
                throw new PrimaryKeyRequiredException("A primary key is required on an Entity in order to perform Updates");
            }

            SqlCeConnection connection = GetConnection(false);
            try
            {
                CheckOrdinals(entityName);
                CheckPrimaryKeyIndex(entityName);

                using (var command = new SqlCeCommand())
                {
                    command.Connection = connection;
                    command.CommandText = entityName;
                    command.CommandType = CommandType.TableDirect;
                    command.IndexName = Entities[entityName].PrimaryKeyIndexName;

                    using (var results = command.ExecuteResultSet(ResultSetOptions.Scrollable | ResultSetOptions.Updatable))
                    {
                        var keyValue = Entities[entityName].Fields.KeyField.PropertyInfo.GetValue(item, null);

                        // seek on the PK
                        var found = results.Seek(DbSeekOptions.BeforeEqual, new object[] { keyValue });

                        if (!found)
                        {
                            // TODO: the PK value has changed - we need to store the original value in the entity or diallow this kind of change
                            throw new RecordNotFoundException("Cannot locate a record with the provided primary key.  You cannot update a primary key value through the Update method");
                        }

                        results.Read();

                        // update the values
                        foreach (var field in Entities[entityName].Fields)
                        {
                            // do not update PK fields
                            if (field.IsPrimaryKey)
                            {
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
                                results.SetValue(field.Ordinal, value);
                            }
                            else
                            {
                                var value = field.PropertyInfo.GetValue(item, null);

                                // TODO: should we update only if it's changed?  Does it really matter at this point?
                                results.SetValue(field.Ordinal, value);
                            }
                        }

                        results.Update();
                    }
                }
            }
            finally
            {
                DoneWithConnection(connection, false);
            }
        }

        /// <summary>
        /// Retrieves a single entity instance from the DataStore identified by the specified primary key value
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="primaryKey"></param>
        /// <returns></returns>
        public override T Select<T>(object primaryKey)
        {
            return (T)Select(typeof(T), null, primaryKey, -1, -1).FirstOrDefault();
        }

        /// <summary>
        /// Populates the ReferenceField members of the provided entity instance
        /// </summary>
        /// <param name="instance"></param>
        public override void FillReferences(object instance)
        {
            FillReferences(instance, null, null, false);
        }

        private void FlushReferenceTableCache()
        {
            m_referenceCache.Clear();
        }

        private void FillReferences(object instance, object keyValue, ReferenceAttribute[] fieldsToFill, bool cacheReferenceTable)
        {
            Type type = instance.GetType();
            string entityName = m_entities.GetNameForType(type);

            if (entityName == null)
            {
                throw new EntityNotFoundException(type);
            }

            if (Entities[entityName].References.Count == 0) return;

            Dictionary<ReferenceAttribute, object[]> referenceItems = new Dictionary<ReferenceAttribute, object[]>();

            // query the key if not provided
            if (keyValue == null)
            {
                keyValue = m_entities[entityName].Fields.KeyField.PropertyInfo.GetValue(instance, null);
            }

            // populate reference fields
            foreach (var reference in Entities[entityName].References)
            {
                if (fieldsToFill != null)
                {
                    if (!fieldsToFill.Contains(reference))
                    {
                        continue;
                    }
                }

                // get the lookup values - until we support filtered selects, this may be very expensive memory-wise
                if (!referenceItems.ContainsKey(reference))
                {
                    object[] refData;
                    if (cacheReferenceTable)
                    {
                        // TODO: ref cache needs to be type->reftype->ref's, not type->refs

                        if (!m_referenceCache.ContainsKey(reference.ReferenceEntityType))
                        {
                            refData = Select(reference.ReferenceEntityType, null, null, -1, 0);
                            m_referenceCache.Add(reference.ReferenceEntityType, refData);
                        }
                        else
                        {
                            refData = m_referenceCache[reference.ReferenceEntityType];
                        }
                    }
                    else
                    {
                        refData = Select(reference.ReferenceEntityType, null, null, -1, 0);
                    }

                    referenceItems.Add(reference, refData);
                }

                // get the lookup field
                var childEntityName = m_entities.GetNameForType(reference.ReferenceEntityType);

                System.Collections.ArrayList children = new System.Collections.ArrayList();

                // now look for those that match our pk
                foreach (var child in referenceItems[reference])
                {
                    var childKey = m_entities[childEntityName].Fields[reference.ReferenceField].PropertyInfo.GetValue(child, null);

                    // this seems "backward" because childKey may turn out null, 
                    // so doing it backwards (keyValue.Equals instead of childKey.Equals) prevents a null referenceexception
                    if (keyValue.Equals(childKey))
                    {
                        children.Add(child);
                    }
                }
                //var carr = children.ToArray(reference.ReferenceEntityType);
                reference.PropertyInfo.SetValue(instance, children.ToArray(reference.ReferenceEntityType), null);
            }
        }

        /// <summary>
        /// Retrieves all entity instances of the specified type from the DataStore
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public override T[] Select<T>()
        {
            var type = typeof(T);
            var items = Select(type, null, null, -1, 0);
            return items.Cast<T>().ToArray();
        }

        public override T[] Select<T>(string searchFieldName, object matchValue)
        {
            var type = typeof(T);
            var items = Select(type, searchFieldName, matchValue, - 1, 0);
            return items.Cast<T>().ToArray();
        }

        /// <summary>
        /// Fetches up to the requested number of entity instances of the specified type from the DataStore, starting with the first instance
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fetchCount"></param>
        /// <returns></returns>
        public override T[] Fetch<T>(int fetchCount)
        {
            var type = typeof(T);
            var items = Select(type, null, null, fetchCount, 0);
            return items.Cast<T>().ToArray();
        }

        /// <summary>
        /// Fetches up to the requested number of entity instances of the specified type from the DataStore, starting with the specified instance
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fetchCount"></param>
        /// <param name="firstRowOffset"></param>
        /// <returns></returns>
        public override T[] Fetch<T>(int fetchCount, int firstRowOffset)
        {
            var type = typeof(T);
            var items = Select(type, null, null, fetchCount, firstRowOffset);
            return items.Cast<T>().ToArray();
        }

        /// <summary>
        /// Fetches a sorted list of entities, up to the requested number of entity instances, of the specified type from the DataStore, starting with the specified instance
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="searchFieldName"></param>
        /// <param name="fetchCount"></param>
        /// <param name="firstRowOffset"></param>
        /// <returns></returns>
        public override T[] Fetch<T>(string searchFieldName, int fetchCount, int firstRowOffset)
        {
            var type = typeof(T);
            var items = Select(type, searchFieldName, null, fetchCount, firstRowOffset);
            return items.Cast<T>().ToArray();
        }

        /// <summary>
        /// Returns the number of instances of the given type in the DataStore
        /// </summary>
        /// <typeparam name="T">Entity type to count</typeparam>
        /// <returns>The number of instances in the store</returns>
        public override int Count<T>()
        {
            var t = typeof(T);
            string entityName = m_entities.GetNameForType(t);

            if (entityName == null)
            {
                throw new EntityNotFoundException(t);
            }

            SqlCeConnection connection = GetConnection(true);
            try
            {
                using (var command = new SqlCeCommand())
                {
                    command.Connection = connection;
                    command.CommandText = string.Format("SELECT COUNT(*) FROM {0}", entityName);
                    return (int)command.ExecuteScalar();
                }
            }
            finally
            {
                DoneWithConnection(connection, true);
            }
        }

        private object[] Select(Type objectType, string searchFieldName, object matchValue, int fetchCount, int firstRowOffset)
        {
            string entityName = m_entities.GetNameForType(objectType);

            if (entityName == null)
            {
                throw new EntityNotFoundException(objectType);
            }

            UpdateIndexCacheForType(entityName);

            var items = new List<object>();

            SqlCeConnection connection = GetConnection(false);
            try
            {
                CheckOrdinals(entityName);

                using (var command = new SqlCeCommand())
                {
                    command.Connection = connection;
                    command.CommandText = entityName;
                    command.CommandType = CommandType.TableDirect;

                    int searchOrdinal = -1;

                    if (searchFieldName != null)
                    {
                        string indexName = string.Format("ORM_IDX_{0}_{1}", entityName, searchFieldName);
                        // check for index name to see if it exists
                        if (!Entities[entityName].IndexNames.Contains(indexName))
                        {
                            throw new SearchOrderRequiredException(entityName, searchFieldName);
                        }

                        command.IndexName = indexName;
                    }
                    else
                    {
                        CheckPrimaryKeyIndex(entityName);
                        command.IndexName = Entities[entityName].PrimaryKeyIndexName;
                        searchOrdinal = Entities[entityName].PrimaryKeyOrdinal;
                    }

                    using (var results = command.ExecuteResultSet(ResultSetOptions.None))
                    {
                        if (results.HasRows)
                        {
                            ReferenceAttribute[] referenceFields = null;

                            int currentOffset = 0;

                            if (matchValue != null)
                            {
                                // convert enums to an int, else the .Equals later check will fail
                                // this feels a bit kludgey, but for now it's all I can think of
                                if (matchValue.GetType().IsEnum)
                                {
                                    matchValue = (int)matchValue;
                                }
                                
                                if (searchOrdinal < 0)
                                {
                                    searchOrdinal = results.GetOrdinal(searchFieldName);
                                }

                                results.Seek(DbSeekOptions.FirstEqual, new object[] { matchValue });
                            }

                            while (results.Read())
                            {
                                if (currentOffset < firstRowOffset)
                                {
                                    currentOffset++;
                                    continue;
                                }

                                if (matchValue != null)
                                {
                                    // if we have a match value, we'll have seeked to the first match above
                                    // then at this point the first non-match means we have no more matches, so
                                    // we can exit out once we hit the first non-match
                                    if (!results[searchOrdinal].Equals(matchValue))
                                    {
                                        break;
                                    }
                                }

                                object item = Activator.CreateInstance(objectType);
                                object rowPK = null;

                                foreach (var field in Entities[entityName].Fields)
                                {
                                    var value = results[field.Ordinal];
                                    if (value != DBNull.Value)
                                    {
                                        if (field.DataType == DbType.Object)
                                        {
                                            // get serializer
                                            var itemType = item.GetType();
                                            var deserializer = GetDeserializer(itemType);

                                            if (deserializer == null)
                                            {
                                                throw new MissingMethodException(
                                                    string.Format("The field '{0}' requires a custom serializer/deserializer method pair in the '{1}' Entity",
                                                    field.FieldName, entityName));
                                            }

                                            var @object = deserializer.Invoke(item, new object[] { field.FieldName, value });
                                            field.PropertyInfo.SetValue(item, @object, null);
                                        }
                                        else
                                        {
                                            field.PropertyInfo.SetValue(item, value, null);
                                        }
                                    }
                                    if (field.IsPrimaryKey)
                                    {
                                        rowPK = value;
                                    }
                                }
                                
                                // autofill references if desired
                                if (referenceFields == null)
                                {
                                    referenceFields = Entities[entityName].References.ToArray();
                                }

                                if (referenceFields.Length > 0)
                                {
                                    FillReferences(item, rowPK, referenceFields, true);
                                }

                                items.Add(item);

                                if ((fetchCount > 0) && (items.Count >= fetchCount))
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                FlushReferenceTableCache();
                DoneWithConnection(connection, false);
            }

            return items.ToArray();
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

            // we'll use table direct for inserts - no point in getting the query parser involved in this
            SqlCeConnection connection = GetConnection(false);
            try
            {
                CheckOrdinals(entityName);

                FieldAttribute identity = null;

                using (var command = new SqlCeCommand())
                {
                    command.Connection = connection;
                    command.CommandText = entityName;
                    command.CommandType = CommandType.TableDirect;

                    using (var results = command.ExecuteResultSet(ResultSetOptions.Updatable))
                    {
                        var record = results.CreateRecord();

                        var keyScheme = Entities[entityName].EntityAttribute.KeyScheme;

                        foreach (var field in Entities[entityName].Fields)
                        {
                            if((keyScheme == KeyScheme.Identity) && field.IsPrimaryKey)
                            {
                                identity = field;
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
                                record.SetValue(field.Ordinal, value);
                            }
                            else
                            {
                                var value = field.PropertyInfo.GetValue(item, null);
                                record.SetValue(field.Ordinal, value);
                            }
                        }

                        results.Insert(record);

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

                                // we've already enforced this to be an array when creating the store
                                foreach (var element in valueArray as Array)
                                {
                                    bool isNew = false;

                                    // only do an insert if the value is new (i.e. need to look for existing reference items)
                                    // not certain how this will work right now, so for now we ask the caller to know what they're doing
                                    switch (keyScheme)
                                    {
                                        case KeyScheme.Identity:
                                            // TODO: see if PK field value == -1
                                            break;
                                        case KeyScheme.GUID:
                                            // TODO: see if PK field value == null
                                            break;
                                    }

                                    if (isNew)
                                    {
                                        var et = m_entities.GetNameForType(element.GetType());
                                        Entities[et].Fields[reference.ReferenceField].PropertyInfo.SetValue(element, fk, null);
                                        Insert(element);
                                    }
                                }
                            }
                        }
                    }

                    command.Dispose();
                }
            }
            finally
            {
                DoneWithConnection(connection, false);
            }
        }

        private int GetIdentity(SqlCeConnection connection)
        {
            using (var command = new SqlCeCommand("SELECT @@IDENTITY", connection))
            {
                object id = command.ExecuteScalar();
                return Convert.ToInt32(id);
            }
        }

        private void CheckOrdinals(string entityName)
        {
            if (Entities[entityName].Fields.OrdinalsAreValid) return;

            SqlCeConnection connection = GetConnection(true);
            try
            {
                using (var command = new SqlCeCommand())
                {
                    command.Connection = connection;
                    command.CommandText = entityName;
                    command.CommandType = CommandType.TableDirect;

                    using (var reader = command.ExecuteReader())
                    {
                        foreach (var field in Entities[entityName].Fields)
                        {
                            field.Ordinal = reader.GetOrdinal(field.FieldName);
                        }

                        Entities[entityName].Fields.OrdinalsAreValid = true;
                    }

                    command.Dispose();
                }
            }
            finally
            {
                DoneWithConnection(connection, true);
            }
        }

        private void UpdateIndexCacheForType(string entityName)
        {
            // have we already cached this?
            if (Entities[entityName].IndexNames != null) return;

            // get all iindex names for the type
            SqlCeConnection connection = GetConnection(true);
            try
            {
                string sql = string.Format("SELECT INDEX_NAME FROM information_schema.indexes WHERE (TABLE_NAME = '{0}')", entityName);

                using (SqlCeCommand command = new SqlCeCommand(sql, connection))
                using(var reader = command.ExecuteReader())
                {
                    List<string> nameList = new List<string>();

                    while (reader.Read())
                    {
                        nameList.Add(reader.GetString(0));
                    }

                    Entities[entityName].IndexNames = nameList;
                }
            }
            finally
            {
                DoneWithConnection(connection, true);
            }
        }

        private void CheckPrimaryKeyIndex(string entityName)
        {
            if (Entities[entityName].PrimaryKeyIndexName != null) return;

            SqlCeConnection connection = GetConnection(true);
            try
            {
                string sql = string.Format("SELECT INDEX_NAME FROM information_schema.indexes WHERE (TABLE_NAME = '{0}') AND (PRIMARY_KEY = 1)", entityName);

                using (SqlCeCommand command = new SqlCeCommand(sql, connection))
                {
                    Entities[entityName].PrimaryKeyIndexName = command.ExecuteScalar() as string;
                }
            }
            finally
            {
                DoneWithConnection(connection, true);
            }
        }

        private string ConnectionString
        {
            get
            {
                if (m_connectionString == null)
                {
                    m_connectionString = string.Format("Data Source={0};Persist Security Info=False;", FileName);

                    if (!string.IsNullOrEmpty(Password))
                    {
                        m_connectionString += string.Format("Password={0};", Password);
                    }
                }
                return m_connectionString;
            }
        }

        private SqlCeConnection CreateConnection()
        {
            return new SqlCeConnection(ConnectionString);
        }

        private void CreateTable(SqlCeConnection connection, EntityInfo entity)
        {
            StringBuilder sql = new StringBuilder();

            if (ReservedWords.Contains(entity.EntityName, StringComparer.InvariantCultureIgnoreCase))
            {
                throw new ReservedWordException(entity.EntityName);
            }

            sql.AppendFormat("CREATE TABLE {0} (", entity.EntityName);

            int count = entity.Fields.Count;

            foreach (var field in entity.Fields)
            {
                //if (field is ReferenceFieldAttribute)
                //{
                //    count--;
                //    continue;
                //}

                if (ReservedWords.Contains(field.FieldName, StringComparer.InvariantCultureIgnoreCase))
                {
                    throw new ReservedWordException(field.FieldName);
                }

                sql.AppendFormat("[{0}] {1} {2}",
                    field.FieldName,
                    field.DataType.ToSqlTypeString(),
                    GetFieldCreationAttributes(entity.EntityAttribute, field));

                if (--count > 0) sql.Append(", ");
            }

            sql.Append(")");

            Debug.WriteLine(sql);

            using (SqlCeCommand command = new SqlCeCommand(sql.ToString(), connection))
            {
                int i = command.ExecuteNonQuery();
            }

            // create indexes
            foreach (var field in entity.Fields)
            {
                if (field.SearchOrder != FieldSearchOrder.NotSearchable)
                {
                    var idxsql = string.Format("CREATE INDEX ORM_IDX_{0}_{1} ON {0}({1} {2})",
                        entity.EntityName,
                        field.FieldName,
                        field.SearchOrder == FieldSearchOrder.Descending ? "DESC" : string.Empty);

                    Debug.WriteLine(idxsql);

                    using (SqlCeCommand command = new SqlCeCommand(idxsql, connection))
                    {
                        int i = command.ExecuteNonQuery();
                    }
                }
            }
        }

        private string GetFieldCreationAttributes(EntityAttribute attribute, FieldAttribute field)
        {
            StringBuilder sb = new StringBuilder();

            switch (field.DataType)
            {
                case DbType.String:
                    if (field.Length > 0)
                    {
                        sb.AppendFormat("({0}) ", field.Length);
                    }
                    else
                    {
                        sb.AppendFormat("({0}) ", DefaultStringFieldSize);
                    }
                    break;
                case DbType.Decimal:
                    int p = field.Precision == 0 ? DefaultNumericFieldPrecision : field.Precision;
                    sb.AppendFormat("({0},{1}) ", p, field.Scale);
                    break;
            }

            if (field.IsPrimaryKey)
            {
                sb.Append("PRIMARY KEY ");

                if (attribute.KeyScheme == KeyScheme.Identity)
                {
                    switch(field.DataType)
                    {
                        case DbType.Int32:
                        case DbType.UInt32:
                            sb.Append("IDENTITY ");
                            break;
                        case DbType.Guid:
                            sb.Append("ROWGUIDCOL ");
                            break;
                        default:
                            throw new FieldDefinitionException(attribute.NameInStore, field.FieldName,
                                string.Format("Data Type '{0}' cannot be marked as an Identity field", field.DataType));
                    }
                }
            }

            if (!field.AllowsNulls)
            {
                sb.Append("NOT NULL ");
            }

            if (field.RequireUniqueValue)
            {
                sb.Append("UNIQUE ");
            }

            return sb.ToString();
        }

        public static string[] ReservedWords = new string[]
        {
            "IDENTITY" ,"ENCRYPTION" ,"ORDER" ,"ADD" ,"END" ,"OUTER" ,"ALL" ,"ERRLVL" ,"OVER" ,"ALTER" ,"ESCAPE" ,"PERCENT" ,"AND" ,"EXCEPT" ,"PLAN" ,"ANY" ,"EXEC" ,"PRECISION" ,"AS" ,"EXECUTE" ,"PRIMARY" ,"ASC",
            "EXISTS" ,"PRINT" ,"AUTHORIZATION" ,"EXIT" ,"PROC" ,"AVG" ,"EXPRESSION" ,"PROCEDURE" ,"BACKUP" ,"FETCH" ,"PUBLIC" ,"BEGIN" ,"FILE" ,"RAISERROR" ,"BETWEEN" ,"FILLFACTOR" ,"READ" ,"BREAK" ,"FOR" ,"READTEXT",
            "BROWSE" ,"FOREIGN" ,"RECONFIGURE" ,"BULK" ,"FREETEXT" ,"REFERENCES" ,"BY" ,"FREETEXTTABLE" ,"REPLICATION" ,"CASCADE" ,"FROM" ,"RESTORE" ,"CASE" ,"FULL" ,"RESTRICT" ,"CHECK" ,"FUNCTION" ,"RETURN" ,"CHECKPOINT",
            "GOTO" ,"REVOKE" ,"CLOSE" ,"GRANT" ,"RIGHT" ,"CLUSTERED" ,"GROUP" ,"ROLLBACK" ,"COALESCE" ,"HAVING" ,"ROWCOUNT" ,"COLLATE" ,"HOLDLOCK" ,"ROWGUIDCOL" ,"COLUMN" ,"IDENTITY" ,"RULE",
            "COMMIT" ,"IDENTITY_INSERT" ,"SAVE" ,"COMPUTE" ,"IDENTITYCOL" ,"SCHEMA" ,"CONSTRAINT" ,"IF" ,"SELECT" ,"CONTAINS" ,"IN" ,"SESSION_USER" ,"CONTAINSTABLE" ,"INDEX" ,"SET" ,"CONTINUE" ,"INNER" ,"SETUSER",
            "CONVERT" ,"INSERT" ,"SHUTDOWN" ,"COUNT" ,"INTERSECT" ,"SOME" ,"CREATE" ,"INTO" ,"STATISTICS" ,"CROSS" ,"IS" ,"SUM" ,"CURRENT" ,"JOIN" ,"SYSTEM_USER" ,"CURRENT_DATE" ,"KEY" ,"TABLE" ,"CURRENT_TIME" ,"KILL",
            "TEXTSIZE" ,"CURRENT_TIMESTAMP" ,"LEFT" ,"THEN" ,"CURRENT_USER" ,"LIKE" ,"TO" ,"CURSOR" ,"LINENO" ,"TOP" ,"DATABASE" ,"LOAD" ,"TRAN" ,"DATABASEPASSWORD" ,"MAX" ,"TRANSACTION" ,"DATEADD" ,"MIN" ,"TRIGGER",
            "DATEDIFF" ,"NATIONAL" ,"TRUNCATE" ,"DATENAME" ,"NOCHECK" ,"TSEQUAL" ,"DATEPART" ,"NONCLUSTERED" ,"UNION" ,"DBCC" ,"NOT" ,"UNIQUE" ,"DEALLOCATE", "NULL", "UPDATE", "DECLARE", "NULLIF", "UPDATETEXT",
            "DEFAULT", "OF", "USE", "DELETE", "OFF", "USER", "DENY", "OFFSETS", "VALUES", "DESC", "ON", "VARYING", "DISK", "OPEN", "VIEW", "DISTINCT", "OPENDATASOURCE", "WAITFOR", "DISTRIBUTED", "OPENQUERY", "WHEN", 
            "DOUBLE", "OPENROWSET", "WHERE", "DROP", "OPENXML", "WHILE", "DUMP", "OPTION", "WITH", "ELSE", "OR", "WRITETEXT" 
        };
    }
}
