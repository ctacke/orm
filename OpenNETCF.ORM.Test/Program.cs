using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using OpenNETCF.ORM.Xml;
using System.Reflection;
using OpenNETCF.ORM.Test.Entities;

namespace OpenNETCF.ORM.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            XmlDataStore store = new XmlDataStore("\\data.xml");

            if (store.StoreExists)
            {
                store.DeleteStore();
            }
            store.DiscoverTypes(Assembly.GetExecutingAssembly());
            store.CreateStore();

            Author a = new Author
            {
                AuthorID = 1,
                Name = "John Doe"
            };

            store.Insert(a);

            var authors = store.Select<Author>();

            var test = new SqlCeTest();
            test.RunTests();
        }
    }
}
