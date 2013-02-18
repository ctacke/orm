using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenNETCF.ORM;
using ReferenceSample.Entities;
using System.Diagnostics;

namespace ReferenceSample
{
    class Program
    {
        static void Main(string[] args)
        {
            var service = new DataService();

            var p = new Program();
            p.TestOneToMany(service);
            p.TestManyToOne(service);
        }

        public void TestOneToMany(DataService service)
        {
            // one customer can have many orders

            // add a customer with no orders
            var acme = service.AddCustomer("Acme");
            PrintOrders(acme);
            
            // add a customer with orders not in the DB
            var orders = new ProductOrder[] 
            {
                new ProductOrder() { InvoiceNumber = "Invoice A" },
                new ProductOrder() { InvoiceNumber = "Invoice B" }
            };
            var newCo = service.AddCustomer("New Co", orders);

            foreach (var c in service.GetAllCustomers())
            {
                PrintOrders(c);
            }

            // add a new order to an existing customer
            service.AddOrderToCustomer(acme, new ProductOrder() { InvoiceNumber = "Invoice C" });

            foreach (var c in service.GetAllCustomers())
            {
                PrintOrders(c);
            }

            // create an order, then associate with a customer
            var order = service.CreateOrder("Invoice D");
            service.AddOrderToCustomer(newCo, order);

            foreach (var c in service.GetAllCustomers())
            {
                PrintOrders(c);
            }
        }

        public void TestManyToOne(DataService service)
        {
        }

        public void PrintOrders(Customer customer)
        {
            Debug.WriteLine("Customer: " + customer.Name);
            if ((customer.Orders == null) || (customer.Orders.Length == 0))
            {
                Debug.WriteLine("  No Orders");
            }
            else
            {
                foreach (var order in customer.Orders)
                {
                    Debug.WriteLine("  Order:" + order.InvoiceNumber);
                }
            }
        }

    }

    public class DataService
    {
        private SqlCeDataStore m_store;

        public DataService()
        {
            m_store = new SqlCeDataStore("MyData.sdf");

            m_store.DeleteStore();

            if (!m_store.StoreExists)
            {
                m_store.CreateStore();
            }

            m_store.AddType<Customer>();
            m_store.AddType<ProductOrder>();
            m_store.AddType<ShipAddress>();
            m_store.AddType<State>();

            m_store.EnsureCompatibility();
        }

        public Customer AddCustomer(string name)
        {
            var customer = new Customer()
            {
                 Name = name
            };

            m_store.Insert(customer, true);

            return customer;
        }

        public Customer AddCustomer(string name, ProductOrder[] orders)
        {
            var customer = new Customer()
            {
                Name = name,
                Orders = orders
            };

            m_store.Insert(customer, true);

            return customer;
        }

        public void AddOrderToCustomer(Customer customer, ProductOrder order)
        {
            var list = new List<ProductOrder>();
            if (customer.Orders != null)
            {
                list.AddRange(customer.Orders);
            }
            if (order.OrderID == 0)
            {
                // insert the order?
            }
            list.Add(order);
            customer.Orders = list.ToArray();
            m_store.Update(customer);
        }

        public ProductOrder CreateOrder(string invoiceNumber)
        {
            var order = new ProductOrder() { InvoiceNumber = invoiceNumber };
            m_store.Insert(order);
            return order;
        }

        public Customer[] GetAllCustomers()
        {
            return m_store.Select<Customer>().ToArray();
        }
    }
}
