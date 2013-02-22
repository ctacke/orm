using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace OpenNETCF.ORM
{
    public abstract class DataStoreValidator
    {
        protected IDataStore Store { get; private set; }

        protected abstract IDataStore CreateStoreFile();

        private TestItem[] TestItems { get; set; }

        public DataStoreValidator()
        {
            CreateTestItems();

            Store = CreateStoreFile();
            if (Store.StoreExists)
            {
                Store.DeleteStore();
            }
        }

        public string StoreType
        {
            get { return Store.GetType().Name; }
        }

        private void CreateTestItems()
        {
            TestItems = new TestItem[] 
            {
                new TestItem("ItemA")
                {
                    UUID = Guid.NewGuid(),
                    ITest = 5,
                    FTest = 3.14F,
                    DBTest = 1.4D,
                    DETest = 2.678M,
                },
                new TestItem("ItemB"),
                new TestItem("ItemC")
            };
        }

        public virtual bool DoStoreSpecificValidation()
        {
            return true;
        }

        public bool CheckDefaults()
        {
            // don't use defaults
            var date = new DateTime(1989, 5, 1);
            var d = new TestItem("ItemD")
            {
                DefString = "not default",
                CreateDate = date
            };

            // insert
            Store.Insert(d);

            // pull and check
            var check = Store.Select<TestItem>(i => i.Name == d.Name).FirstOrDefault();
            if (check.DefString != "not default") return false;
            if (!check.CreateDate.Equals(date)) return false;

            // use defaults
            var e = new TestItem("ItemE");

            // insert
            Store.Insert(e);

            // pull and check
            check = Store.Select<TestItem>(i => i.Name == e.Name).FirstOrDefault();
            if (check.DefString != "Default") return false;
            if ((DateTime.Now - check.CreateDate).TotalMinutes > 1) return false;

            return true;
        }

        public bool DoCreateStore()
        {
            try
            {
                Store.AddType<TestItem>();
                Store.CreateStore();
            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        }

        public bool DoInserts()
        {
            try
            {
                for (int i = 0; i < TestItems.Length; i++)
                {
                    Store.Insert(TestItems[i]);
                }

                var count = Store.Count<TestItem>();

                if(count != TestItems.Length) return false;
            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        }

        public bool DoSelects()
        {
            try
            {
                var items = Store.Select<TestItem>();
                if(items.Count() != TestItems.Length) return false;

                // pull by field
                for (int i = 0; i < TestItems.Length; i++)
                {
                    var item = Store.Select<TestItem>("Name", TestItems[i].Name).FirstOrDefault();
                    if (!item.Equals(TestItems[i])) return false;
                }

                // pull by PK
                for (int i = 0; i < TestItems.Length; i++)
                {
                    var item = Store.Select<TestItem>(TestItems[i].ID);
                    if (!item.Equals(TestItems[i])) return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        }

        public bool DoUpdates()
        {
            try
            {
                // rename item 2
                TestItems[2].Name = "NewItem";
                TestItems[2].Address = "Changed Address";
                TestItems[2].TS = new TimeSpan(8, 23, 30);
                Store.Update(TestItems[2]);

                // pulling by the old name should return null
                var item = Store.Select<TestItem>("Name", "ItemC").FirstOrDefault();
                if (item != null) return false;
                // check by the new name - it should return a value that is equivalent to item 2
                item = Store.Select<TestItem>("Name", TestItems[2].Name).FirstOrDefault();
                if (!item.Equals(TestItems[2])) return false;

                // validate the Contains method
                foreach (var i in TestItems)
                {
                    if (!Store.Contains(i)) return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        }

        public bool DoDeletes()
        {
            try
            {
                // delete the first item from the store
                Store.Delete(TestItems[0]);

                // make sure it's gone by selecting a field
                var item = Store.Select<TestItem>("Name", TestItems[0].Name).FirstOrDefault();
                if(item != null) return false;

                // make sure Contains returns false
                if(Store.Contains(TestItems[0])) return false;

                // make sure the count decremented
                var count = Store.Count<TestItem>();
                if(count != TestItems.Length - 1) return false;
            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        }

        public bool DoReferentialInserts()
        {
            try
            {
                Store.AddType<Author>();
                Store.AddType<Book>();
                Store.CreateOrUpdateStore();

                // insert an author
                var dumas = new Author() { Name = "Alexadre Dumas" };
                Store.Insert(dumas);

                // insert a couple books.
                // note that we're inserting the foreign key value
                Store.Insert(
                    new Book()
                    {
                        AuthorID = dumas.ID,
                        Title = "The Count of Monte Cristo"
                    });

                Store.Insert(
                    new Book()
                    {
                        AuthorID = dumas.ID,
                        Title = "The Three Musketeers"
                    });

                // now get the authors back, telling ORM to fill the references
                var authors = Store.Select<Author>(true).ToArray();

                if (authors.Length != 1) return false;
                if (authors[0].Books.Length != 2) return false;

                return true;
            }
            catch(Exception ex)
            {
                return false;
            }
        }
    }
}
