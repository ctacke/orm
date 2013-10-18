using OpenNETCF.ORM;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System;
using System.Text;
using OpenNETCF.ORM.Replication;
using System.Threading;
using System.Diagnostics;

namespace OpenNETCF.ORM.SqlCE.Integration.Test
{
    [TestClass()]
    public class ReplicationTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod()]
        public void BasicLocalReplicationTest()
        {
            var source = new SqlCeDataStore("source.sdf");
            if (!source.StoreExists)
            {
                source.CreateStore();
            }
            source.AddType<TestItem>();

            var destination = new SqlCeDataStore("dest.sdf");
            if (!destination.StoreExists)
            {
                destination.CreateStore();
            }

            // build a replictor to send data to the destiantion store
            var replicator = new Replicator(destination, ReplicationBehavior.ReplicateAndDelete);

            // replication is opt-in, so tell it what type(s) we want to replicate
            replicator.RegisterEntity<TestItem>();

            // add the replicator to the source
            source.Replicators.Add(replicator);

            // watch an event for when data batches go out
            replicator.DataReplicated += delegate
            {
                // get a count
                Debug.WriteLine(string.Format("Sent {0} rows", replicator.GetCount<TestItem>()));
            };

            var rows = 200;

            // put some data in the source
            for (int i = 0; i < rows; i++)
            {
                var item = new TestItem(string.Format("Item {0}", i));
                source.Insert(item);
            }

            int remaining = 0;
            // loop until the source table is empty
            do
            {
                Thread.Sleep(500);
                remaining = source.Count<TestItem>();
            } while(remaining > 0);

            // make sure the destination has all rows
            Assert.AreEqual(rows, destination.Count<TestItem>());
        }
    }

}
