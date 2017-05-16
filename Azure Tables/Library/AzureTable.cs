using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Xml.Linq;
using System.IO;

namespace OpenNETCF.Azure
{
    public class AzureTable
    {
        public string ID { get; private set; }
        public DateTime LastUpdated { get; private set; }
        public string TableName { get; private set; }

        private ServiceProxy m_proxy;

        internal AzureTable(ServiceProxy proxy, string tableName, string id, DateTime updated)
        {
            m_proxy = proxy;
            TableName = tableName;
            ID = id;
            LastUpdated = updated;
        }

        public AzureEntity GetEntity(string partitionKey, string rowKey)
        {
            Validate
                .Begin()
                .IsNotNullOrEmpty(partitionKey)
                .IsNotNullOrEmpty(rowKey)
                .Check();

            return m_proxy.GetEntity(this.TableName, partitionKey, rowKey);
        }

        public void RefreshStatistics()
        {
            // TODO: re-get LastUpdated (and anything else?)
            var info = m_proxy.GetTable(TableName);
            if (info != null)
            {
                this.LastUpdated = info.LastUpdated;
            }
        }

        public IEnumerable<AzureEntity> GetEntities()
        {
            return m_proxy.GetEntities(this.TableName);
        }

        public IEnumerable<AzureEntity> GetEntities(int maxCount)
        {
            Validate
                .Begin()
                .IsGreaterThan(maxCount, 0)
                .Check();

            return m_proxy.GetEntities(this.TableName, maxCount);
        }

        public IEnumerable<AzureEntity> GetPartitionEntities(string partitionKey)
        {
            return m_proxy.GetEntities(this.TableName, partitionKey, -1);
        }

        public IEnumerable<AzureEntity> GetPartitionEntities(string partitionKey, int maxCount)
        {
            Validate
                .Begin()
                .IsGreaterThan(maxCount, 0)
                .Check();

            return m_proxy.GetEntities(this.TableName, partitionKey, maxCount);
        }

        public void Insert(AzureEntity entity)
        {
            Validate
                .Begin()
                .ParameterIsNotNull(entity, "entity")
                .Check();

            m_proxy.InsertEntity(this.TableName, entity);
        }

        public void InsertOrReplace(AzureEntity entity)
        {
            Validate
                .Begin()
                .ParameterIsNotNull(entity, "entity")
                .Check();

            m_proxy.InsertOrReplaceEntity(this.TableName, entity);
        }

        public void Update(AzureEntity entity)
        {
            Validate
                .Begin()
                .ParameterIsNotNull(entity, "entity")
                .Check();

            m_proxy.UpdateEntity(this.TableName, entity);
        }

        public void Delete(AzureEntity entity)
        {
            Validate
                .Begin()
                .ParameterIsNotNull(entity, "entity")
                .Check();

            Delete(entity.PartitionKey, entity.RowKey);
        }

        public void Delete(string partitionKey, string rowKey)
        {
            Validate
                .Begin()
                .IsNotNullOrEmpty(partitionKey)
                .IsNotNullOrEmpty(rowKey)
                .Check();

            m_proxy.DeleteEntity(this.TableName, partitionKey, rowKey);
        }
    }
}
