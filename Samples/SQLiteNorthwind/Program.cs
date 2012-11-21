using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenNETCF.ORM;
using System.Windows.Forms;
using System.IO;
using System.Reflection;

namespace SQLiteNorthwind
{
    class Program
    {
        static void Main(string[] args)
        {
            new Program().Run();
        }

        public void Run()
        {
            var store = new SQLiteDataStore(Path.Combine( Application.StartupPath, "northwind.db"));

            // get all orders, with the associated (reference) customer for each
            var orders = store.Select<Orders>(true).ToArray();

            var oldOrder = orders.Last();

            // create a new order for the same customer as the last in the list
            var newOrder = new Orders()
            {
                OrderID = oldOrder.OrderID + 1, // this database does not use auto-incrementing keys
                CustomerID = oldOrder.CustomerID,
                ShipName = "ATTN: John Steinbeck",
                ShipAddress = "7 Rue de M",
                ShipCity = "Paris",
                ShipCountry = "France",
                ShippedDate = new DateTime(1955, 6, 1)
            };

            // insert that order
            store.Insert(newOrder);

            // select that order back out by PK
            var order = store.Select<Orders>(o => o.ShipName.Contains("Steinbeck")).First();

            // now delete that order by PK value
            store.Delete<Orders>(order.OrderID);
        }
    }
}
