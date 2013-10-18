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
        private List<string> m_registeredNames = new List<string>();
        private List<Type> m_registeredTypes = new List<Type>();

        public const int MinReplicationPeriod = 100;
        public const int DefaultReplicationPeriod = 5000;
        public const int DefaultBatchSize = 50;

        public bool Running { get; private set; }
        public ReplicationBehavior Behavior { get; private set; }

        public Dictionary<Type, int> m_typeCounts;
        public Dictionary<string, int> m_nameCounts;

        public event EventHandler DataReplicated;

        public Replicator(IDataStore destination, ReplicationBehavior behavior)
        {
            if (behavior != ReplicationBehavior.ReplicateAndDelete)
            {
                throw new NotSupportedException("Only ReplicateAndDelete is currently supported");
            }

            Behavior = behavior;
            m_destination = destination;

            ReplicationPeriod = DefaultReplicationPeriod;
            MaxReplicationBatchSize = DefaultBatchSize;

            m_dataAvailable = new AutoResetEvent(false);

            m_typeCounts = new Dictionary<Type, int>();
            m_nameCounts = new Dictionary<string, int>();
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
            if (m_registeredNames.Contains(e.EntityName, StringComparer.InvariantCultureIgnoreCase))
            {
                m_dataAvailable.Set();
            }
            else if (m_registeredTypes.Contains(e.Item.GetType()))
            {
                m_dataAvailable.Set();
            }
        }

        public void Start()
        {
            if (Running) return;
            new Thread(new ThreadStart(ReplicationProc))
            {
                IsBackground = true,
                Name = "ReplicationProc"
            }
            .Start();
        }

        public void Stop()
        {
            m_run = false;
        }

        public void RegisterEntity<T>()
        {
            RegisterEntity(typeof(T));
        }

        public void RegisterEntity(Type entityType)
        {
            lock (m_registeredTypes)
            {
                if (m_registeredTypes.Contains(entityType)) return;
                m_registeredTypes.Add(entityType);

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
            lock (m_registeredNames)
            {
                if (m_registeredNames.Contains(entityName, StringComparer.InvariantCultureIgnoreCase)) return;
                m_registeredNames.Add(entityName);

                lock (m_nameCounts)
                {
                    if (!m_nameCounts.ContainsKey(entityName))
                    {
                        m_nameCounts.Add(entityName, 0);
                    }
                }

                // TODO: look for failure and cache if it does (e.g. not connected scenarios)
                var definition = m_source.DiscoverDynamicEntity(entityName);
                m_destination.RegisterDynamicEntity(definition);
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
                var dataSent = false;
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

                    dataSent = false;

                    // loop through all registered entities
                    foreach (var name in m_registeredNames)
                    {
                        var items = m_source.Select(name).Take(MaxReplicationBatchSize);

                        foreach (var item in items)
                        {
                            try
                            {
                                m_destination.Insert(item);

                                m_source.Delete(item);

                                // increment the count
                                m_nameCounts[name]++;

                                dataSent = true;
                            }
                            catch (Exception ex)
                            {
                                OnReplicationError(ex);
                                goto loop;
                            }

                            // yield so we're not chewing up processor time
                            Thread.Sleep(0);
                        }
                    }

                    foreach (var type in m_registeredTypes)
                    {
                        var items = m_source.Select(type).Take(MaxReplicationBatchSize);

                        foreach (var item in items)
                        {
                            try
                            {
                                m_destination.Insert(item);

                                m_source.Delete(item);

                                // increment the count
                                m_typeCounts[type]++;

                                dataSent = true;
                            }
                            catch (Exception ex)
                            {
                                OnReplicationError(ex);
                                goto loop;
                            }

                            // yield so we're not chewing up processor time
                            Thread.Sleep(0);
                        }
                    }

                // yes, I'm using a goto.  It's simpler than a flag variable and multiple continue calls.
                loop: ;
                    if (dataSent) RaiseDataReplicated();

                    et = Environment.TickCount - et;

                    Debug.WriteLine(string.Format("Replication took {0}ms", et));
                }
            }
            finally
            {
                Running = false;
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
