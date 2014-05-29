using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace OpenNETCF.ORM.Replication
{
    public sealed class Replicator
    {
        private IDataStore m_destination;
        private IDataStore m_source;
        private int m_period;
        private int m_batchSize;
        private AutoResetEvent m_dataAvailable;
        private bool m_run = false;
        private object m_syncRoot = new object();

        private Registrations m_registrations = new Registrations();

        public const int MinReplicationPeriod = 100;
        public const int DefaultReplicationPeriod = 5000;
        public const int DefaultBatchSize = 50;

        public bool Running { get; private set; }
        public ReplicationBehavior Behavior { get; private set; }
        public bool CreateIdentityFieldInReplicatedTable { get; private set; }

        public Dictionary<Type, int> m_typeCounts;
        public Dictionary<string, int> m_nameCounts;

        public event EventHandler DataReplicated;

        public Replicator(IDataStore destination, ReplicationBehavior behavior)
            : this(destination, behavior, false)
        {
        }

        public Replicator(IDataStore destination, ReplicationBehavior behavior, bool addIdentityToDestination)
        {
            if (behavior != ReplicationBehavior.ReplicateAndDelete)
            {
                throw new NotSupportedException("Only ReplicateAndDelete is currently supported");
            }

            if (destination == null)
            {
                throw new ArgumentNullException();
            }

            CreateIdentityFieldInReplicatedTable = addIdentityToDestination;

            Behavior = behavior;
            m_destination = destination;

            ReplicationPeriod = DefaultReplicationPeriod;
            MaxReplicationBatchSize = DefaultBatchSize;

            m_dataAvailable = new AutoResetEvent(false);

            m_typeCounts = new Dictionary<Type, int>();
            m_nameCounts = new Dictionary<string, int>();
        }

        public IDataStore Destination
        {
            get { return m_destination; }
        }

        public void ResetCounts()
        {
            lock (m_typeCounts)
            {
                foreach (var t in m_typeCounts)
                {
                    m_typeCounts[t.Key] = 0;
                }
            }
            lock (m_nameCounts)
            {
                foreach (var n in m_nameCounts)
                {
                    m_nameCounts[n.Key] = 0;
                }
            }
        }

        public int GetCount<T>()
        {
            return GetCount(typeof(T));
        }

        public int GetCount(Type entityType)
        {
            lock (m_typeCounts)
            {
                if (!m_typeCounts.ContainsKey(entityType)) return 0;
                return m_typeCounts[entityType];
            }
        }

        public int GetCount(string entityName)
        {
            lock (m_nameCounts)
            {
                if (!m_nameCounts.ContainsKey(entityName)) return 0;
                return m_nameCounts[entityName];
            }
        }

        public int ReplicationPeriod
        {
            get { return m_period; }
            set
            {
                if (value < MinReplicationPeriod) throw new ArgumentOutOfRangeException();

                m_period = value;
            }
        }

        /// <summary>
        /// The maximum number of Entity instances (e.g. data rows) to send during any given ReplicationPeriod
        /// </summary>
        /// <remarks>A MaxReplicationBatchSize of 0 mean "send all data"</remarks>
        public int MaxReplicationBatchSize
        {
            get { return m_batchSize; }
            set
            {
                if (value < 0) value = 0;

                m_batchSize = value;
            }
        }

        internal void SetSource(IDataStore source)
        {
            m_source = source;

            m_source.AfterInsert += new EventHandler<EntityInsertArgs>(m_source_AfterInsert);
        }

        void m_source_AfterInsert(object sender, EntityInsertArgs e)
        {
            // if we have an insert on a replicated entity, don't wait for the full period, let the replication proc know immediately

            // TODO: add preemption?

            lock (m_registrations)
            {
                if (m_registrations.Contains(e.EntityName))
                {
                    m_dataAvailable.Set();
                }
                else if (m_registrations.Contains(e.Item.GetType()))
                {
                    m_dataAvailable.Set();
                }
            }
        }

        public void Start()
        {
            lock (m_syncRoot)
            {
                if (Running) return;
                Running = true;
                try
                {

                    new Thread(new ThreadStart(ReplicationProc))
                    {
                        IsBackground = true,
                        Name = "ReplicationProc",
                    }
                    .Start();
                }
                catch
                {
                    Running = false;

                }
            }
        }

        public void Stop()
        {
            m_run = false;
        }

        public void RegisterEntity<T>(ReplicationPriority priority)
        {
            RegisterEntity(typeof(T), priority);
        }

        public void RegisterEntity<T>()
        {
            RegisterEntity(typeof(T), ReplicationPriority.Normal);
        }

        public void RegisterEntity(Type entityType)
        {
            RegisterEntity(entityType, ReplicationPriority.Normal);
        }

        public void RegisterEntity(Type entityType, ReplicationPriority priority)
        {
            lock (m_registrations)
            {
                m_registrations.AddType(entityType, priority);

                lock (m_typeCounts)
                {
                    if (!m_typeCounts.ContainsKey(entityType))
                    {
                        m_typeCounts.Add(entityType, 0);
                    }
                }

                // TODO: look for failure and cache if it does (e.g. not connected scenarios)
                m_destination.AddType(entityType);
            }
        }

        public void RegisterEntity(string entityName)
        {
            RegisterEntity(entityName, ReplicationPriority.Normal);
        }

        public void RegisterEntity(string entityName, ReplicationPriority priority)
        {
            RegisterEntity(entityName, null, priority);
        }

        public void RegisterEntity(string entityName, string replicatedName, ReplicationPriority priority)
        {
            lock (m_registrations)
            {
                m_registrations.AddName(entityName, replicatedName, priority);

                lock (m_nameCounts)
                {
                    if (!m_nameCounts.ContainsKey(entityName))
                    {
                        m_nameCounts.Add(entityName, 0);
                    }
                }

                // TODO: look for failure and cache if it does (e.g. not connected scenarios)
                var definition = m_source.DiscoverDynamicEntity(entityName);

                definition.EntityName = replicatedName;

                // the replicated entity cannot use the same auto-increment field as the local table or we'll end up with replication problems where local IDs don't match remote IDs
                if (CreateIdentityFieldInReplicatedTable)
                {
                    var existing = definition.Fields.KeyField;
                    if(existing != null)
                    {
                        existing.IsPrimaryKey = false;
                    }

                    definition.EntityAttribute.KeyScheme = KeyScheme.Identity;
                    definition.Fields.Add(new FieldAttribute("ReplID", System.Data.DbType.Int32, true), true);
                }
                else
                {
                    definition.EntityAttribute.KeyScheme = KeyScheme.None;
                }

                m_destination.RegisterDynamicEntity(definition, true);
            }
        }

        private void OnReplicationError(Exception ex)
        {
            // TODO: handle this or pass it upstream
            Debug.WriteLine("Replication Error: " + ex.Message);
        }

        private void ReplicationProc()
        {
            try
            {
                m_run = true;
                Running = true;
                var et = 0;

                while (m_run)
                {
                    // wait either ReplicationPeriod or until data is available, whichever is smaller
#if WINDOWS_PHONE
                    var wait = m_dataAvailable.WaitOne(ReplicationPeriod);
#else
                    m_dataAvailable.WaitOne(ReplicationPeriod, false);
#endif

                    et = Environment.TickCount;

                    try
                    {
//                        Debug.Write("Replicating...");

                        if (DoReplicationForPriority(ReplicationPriority.High))
                        {
                            RaiseDataReplicated();
                        }
                        // TODO: add preemption?
                        if (DoReplicationForPriority(ReplicationPriority.Normal))
                        {
                            RaiseDataReplicated();
                        }
                        if (DoReplicationForPriority(ReplicationPriority.Low))
                        {
                            RaiseDataReplicated();
                        }

                        et = Environment.TickCount - et;

//                        Debug.WriteLine(string.Format("took {0}ms", et));
                    }
                    catch (Exception ex)
                    {
                        OnReplicationError(ex);
                    }
                }
            }
            finally
            {
                Running = false;
            }
        }

        private bool DoReplicationForPriority(ReplicationPriority priority)
        {
            bool dataSent = false;

            // loop through all registered entities
            lock (m_registrations)
            {
                foreach (var registration in m_registrations.GetNameRegistrations(priority))
                {
                    var items = m_source.Select(registration.LocalName).Take(MaxReplicationBatchSize);

                    foreach (var item in items)
                    {
                        item.EntityName = registration.ReplicatedName;
                        m_destination.Insert(item);

                        item.EntityName = registration.LocalName;
                        m_source.Delete(item);

                        // increment the count
                        m_nameCounts[registration.LocalName]++;

                        dataSent = true;

                        // yield so we're not chewing up processor time
                        Thread.Sleep(0);
                    }
                }

                foreach (var registration in m_registrations.GetTypeRegistrations(priority))
                {
                    var items = m_source.Select(registration.Type).Take(MaxReplicationBatchSize);

                    foreach (var item in items)
                    {
                        m_destination.Insert(item);

                        m_source.Delete(item);

                        // increment the count
                        m_typeCounts[registration.Type]++;

                        dataSent = true;

                        // yield so we're not chewing up processor time
                        Thread.Sleep(0);
                    }
                }

                return dataSent;
            }
        }

        private void RaiseDataReplicated()
        {
            var handler = DataReplicated;
            if (handler == null) return;

            handler(this, EventArgs.Empty);
        }
    }
}
