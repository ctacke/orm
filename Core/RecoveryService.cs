using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace OpenNETCF.ORM
{
    public class RecoverableInfo
    {
        internal RecoverableInfo()
        {
        }

        public int InsertsPending { get; internal set; }
        public int InsertsRecovered { get; internal set; }
        public int InsertsAbandoned { get; internal set; }
    }

    internal class RecoveryService<TEntityInfo> : DisposableBase
                where TEntityInfo : EntityInfo, new()
    {
        private RecoverableInfo m_recoverInfo;
        private DataStore<TEntityInfo> m_store;
        private bool m_enabled;
        private Thread m_workerThread;

        public int RetriesBeforeAbandon { get; set; }
        public int RetryPeriod { get; set; }
        public int RetryBufferDepth { get; private set; }

        private CircularBuffer<RecoverableInsertItem> m_insertQueue;
        private object m_workingItem;

        public RecoveryService(DataStore<TEntityInfo> store)
        {
            m_store = store;
            ResetStats();

            RetriesBeforeAbandon = 6;
            RetryPeriod = 30000;
            RetryBufferDepth = 100;

            m_insertQueue = new CircularBuffer<RecoverableInsertItem>(RetryBufferDepth);
        }

        protected override void ReleaseManagedResources()
        {
            Enabled = false;
        }

        public RecoverableInfo GetStats()
        {
            return m_recoverInfo;
        }

        public void ResetStats()
        {
            m_recoverInfo = new RecoverableInfo();
        }

        public void QueueInsertForRecovery(object item, bool insertReferences)
        {
            if (!Enabled) return;

            var wi = m_workingItem as RecoverableInsertItem;
            if ((wi != null) && (item.Equals(wi.Item))) return;

            lock (m_insertQueue)
            {
                m_insertQueue.Enqueue(new RecoverableInsertItem(item, insertReferences));
            }
        }

        public bool Enabled
        {
            get { return m_enabled; }
            set
            {
                if (value == Enabled) return;

                if (value)
                {
                    // start the thread on first enable
                    if (m_workerThread == null)
                    {
                        m_workerThread = new Thread(RecoveryThreadProc)
                        {
                            IsBackground = true,
                            Name = "Data Store Recovery Thread"
                        };

                        m_workerThread.Start();
                    }
                }

                m_enabled = value;
            }
        }

        private void RecoveryThreadProc()
        {
            while (!IsDisposed)
            {
                if (Enabled)
                {

                    // attempt recovery on insert items
                    while (m_insertQueue.Count > 0)
                    {
                        try
                        {
                            lock (m_insertQueue)
                            {
                                var wi = m_insertQueue.Peek() as RecoverableInsertItem;
                                m_workingItem = wi;

                                // try the insert again
                                m_store.Insert(wi.Item, wi.InsertReferences, true);

                                // success, so remove from queue and update stats
                                m_insertQueue.Dequeue();
                            }
                            m_recoverInfo.InsertsRecovered++;
                            m_recoverInfo.InsertsPending = m_insertQueue.Count;

                            // prevent CPU saturation on success
                            Thread.Sleep(1000);
                        }
                        catch
                        {
                            // failed, update the count and wait for a retry period
                            if (++(m_workingItem as RecoverableInsertItem).Retries > RetriesBeforeAbandon)
                            {
                                // retry threshold exceeded - toss it out
                                lock (m_insertQueue)
                                {
                                    m_insertQueue.Dequeue();
                                }
                                m_recoverInfo.InsertsAbandoned++;
                                m_recoverInfo.InsertsPending = m_insertQueue.Count;
                            }

                            // no point in trying more, we'll wait a retry period and come back in then
                            break;
                        }
                        finally
                        {
                            m_workingItem = null;
                        }
                    }
                }

                try
                {
                    Thread.Sleep(RetryPeriod);
                }
                catch (ThreadAbortException)
                {
                    // if we shut down or are disposed while sleeping, we'll throw and end up here
                    // it's save to ignore, as we're shutting down anyway
                    return;
                }
            }
        }

        private class RecoverableInsertItem
        {
            public RecoverableInsertItem(object item, bool insertReferences)
            {
                Item = item;
                InsertReferences = insertReferences;
            }

            public object Item { get; set; }
            public bool InsertReferences { get; set; }
            public int Retries { get; set; }
        }
    }
}
