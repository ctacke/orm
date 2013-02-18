using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenNETCF.ORM;

namespace ReferenceSample.Entities
{
    [Entity(KeyScheme.Identity)]
    public class Customer
    {
        [Field(IsPrimaryKey = true)]
        public int CustomerID { get; set; }

        [Field]
        public string Name { get; set; }

        [Reference(typeof(ProductOrder), "CustomerID", ReferenceType=ReferenceType.OneToMany)]
        public ProductOrder[] Orders { get; set; }
    }

    [Entity(KeyScheme.Identity)]
    public class ProductOrder
    {
        public ProductOrder()
        {
            // this is required for cascading inserts to work
            OrderID = -1;
        }

        [Field(IsPrimaryKey = true)]
        public int OrderID { get; set; }

        [Field] // this is the foreign key
        public int CustomerID { get; set; }

        [Field]
        public string InvoiceNumber { get; set; }
    }

    [Entity(KeyScheme.Identity)]
    public class ShipAddress
    {
        [Field(IsPrimaryKey = true)]
        public int AddressID { get; set; }

        [Field]
        public string Street { get; set; }

        [Reference(typeof(State), "StateID", ReferenceType = ReferenceType.ManyToOne)]
        public State State { get; set; }
    }

    [Entity(KeyScheme.Identity)]
    public class State
    {
        [Field(IsPrimaryKey = true)]
        public int StateID { get; set; }

        [Field]
        public string Name { get; set; }

        [Field]
        public string Abbreviation { get; set; }
    }
}
