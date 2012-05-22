using System;
using System.Linq;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using OpenNETCF.ORM.SQLite;
using System.IO;
using System.Diagnostics;

namespace OpenNETCF.ORM.TestHarness.Android
{
    [Activity(Label = "OpenNETCF.ORM.TestHarness.Android", MainLauncher = true, Icon = "@drawable/icon")]
    public class Activity1 : Activity
    {
        int count = 1;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            // Get our button from the layout resource,
            // and attach an event to it
            Button button = FindViewById<Button>(Resource.Id.MyButton);

            button.Click += delegate { button.Text = string.Format("{0} clicks!", count++); };
//            GuidTest();
            SimpleCRUDTest();
        }

        public void GuidTest()
        {
            var item = new TestItem();

            var gpi = item.GetType().GetProperty("UUID", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            var tpi = item.GetType().GetProperty("Test", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

            var guid = Guid.NewGuid();
            var test = 5L;

            gpi.SetValue(item, guid, null);
            tpi.SetValue(item, test, null);

            var val = item.UUID.HasValue;
            var g2 = item.UUID.Value;

            item.Test = 3;
        }

        public void SimpleCRUDTest()
        {
            var path = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "test.db");
            var store = new SQLiteDataStore(path);
            store.AddType<TestItem>();

            TestItem itemA;
            TestItem itemB;
            TestItem itemC;

            if (store.StoreExists)
            {
                store.DeleteStore();
            }

            store.CreateStore();

            itemA = new TestItem("ItemA");
            itemA.UUID = Guid.NewGuid();
            itemA.Test = 5;

            itemB = new TestItem("ItemB");
            itemC = new TestItem("ItemC");

            // INSERT
            store.Insert(itemA);
            store.Insert(itemB);
            store.Insert(itemC);

            // COUNT
            var count = store.Count<TestItem>();

            Assert.AreEqual(3, count);

            // SELECT
            var items = store.Select<TestItem>();
            if (items.Count() != 3) Debugger.Break();

            var item = store.Select<TestItem>("Name", itemB.Name).FirstOrDefault();
            Assert.IsTrue(item.Equals(itemB));

            item = store.Select<TestItem>(3);
            Assert.IsTrue(item.Equals(itemC));

            // FETCH

            // UPDATE
            itemC.Name = "NewItem";
            itemC.Address = "Changed Address";
            itemC.TS = new TimeSpan(8, 23, 30);
            store.Update(itemC);

            item = store.Select<TestItem>("Name", "ItemC").FirstOrDefault();
            Assert.IsNull(item);
            item = store.Select<TestItem>("Name", itemC.Name).FirstOrDefault();
            Assert.IsTrue(item.Equals(itemC));

            // CONTAINS
            var exists = store.Contains(itemA);
            Assert.IsTrue(exists);

            // DELETE
            store.Delete(itemA);
            item = store.Select<TestItem>("Name", itemA.Name).FirstOrDefault();
            Assert.IsNull(item);

            // CONTAINS
            exists = store.Contains(itemA);
            Assert.IsFalse(exists);

            // COUNT
            count = store.Count<TestItem>();
            Assert.AreEqual(2, count);
        }

        [Entity(KeyScheme = KeyScheme.Identity)]
        public class TestItem : IEquatable<TestItem>
        {
            public TestItem()
            {
            }

            public TestItem(string name)
            {
                Name = name;
            }

            [Field(IsPrimaryKey = true)]
            public int ID { get; set; }

            [Field]
            public string Name { get; set; }

            [Field]
            public Guid? UUID { get; set; }

            [Field]
            public uint Test { get; set; }

            [Field]
            public string Address { get; set; }

            [Field]
            public TimeSpan TS { get; set; }

            public bool Equals(TestItem other)
            {
                return this.ID == other.ID;
            }
        }
    }

    public static class Assert
    {
        public static void IsNull(object item)
        {
            if (item != null) throw new Exception();
        }

        public static void IsFalse(bool b)
        {
            if (b) throw new Exception();
        }

        public static void IsFalse(Func<bool> f)
        {
            if (f()) throw new Exception();
        }

        public static void IsTrue(bool b)
        {
            if (!b) throw new Exception();
        }

        public static void IsTrue(Func<bool> f)
        {
            if (!f()) throw new Exception();
        }

        public static void AreEqual(object expected, object actual)
        {
            if (!(expected.Equals(actual))) throw new Exception();
        }
    }
}

