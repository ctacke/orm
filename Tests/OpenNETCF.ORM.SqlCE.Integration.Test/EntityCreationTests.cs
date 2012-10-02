using OpenNETCF.ORM;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System;
using OpenNETCF.ORM.Test;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using OpenNETCF.ORM.Test.Entities;

namespace OpenNETCF.ORM.SqlCE.Integration.Test
{
    [TestClass()]
    public class EntityCreationTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod()]
        public void DelegatePerfTest()
        {
            var iterations = 1000;
            var sw1 = new Stopwatch();
            var sw2 = new Stopwatch();

            var store = new SqlCeDataStore("test.sdf");
            store.AddType<TestItem>();
            store.AddType<TestItemD>();
            store.CreateStore();

            // populate test data
            var generator = new DataGenerator();
            var items = generator.GenerateTestItems(100);
            store.BulkInsert(items);
            foreach (var i in items)
            {
                store.Insert((TestItemD)i);
            }


            // no delegate
            sw1.Reset();
            sw1.Start();
            for (int i = 0; i < iterations; i++)
            {
                var list = store.Select<TestItem>();
            }
            sw1.Stop();
            // with delegate
            sw2.Reset();
            sw2.Start();
            for (int i = 0; i < iterations; i++)
            {
                var list = store.Select<TestItemD>();
            }
            sw2.Stop();

            var noDelegate = sw1.ElapsedMilliseconds;
            var withDelegate = sw2.ElapsedMilliseconds;

            Debug.WriteLine(string.Format("Delegate gave a {0}% improvement", ((float)(noDelegate - withDelegate) / withDelegate) * 100f));
        }

        [TestMethod()]
        public void SeekTest()
        {
            var iterations = 1000;
            var sw1 = new Stopwatch();

            var store = new SqlCeDataStore("test.sdf");
            store.AddType<SeekItem>();
            store.CreateStore();

            // populate test data
            var generator = new DataGenerator();
            var items = generator.GenerateSeekItems(100);
            store.BulkInsert(items);


            // no delegate
            sw1.Reset();
            sw1.Start();

            var item = store.Seek<SeekItem>(System.Data.SqlServerCe.DbSeekOptions.BeforeEqual, "SeekField", 11);
            sw1.Stop();

            // item should have a value of 10
        }
    }
}
