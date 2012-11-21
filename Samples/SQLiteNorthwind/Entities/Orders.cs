//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:2.0.50727.5456
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace OpenNETCF.ORM
{
    using System;
    using OpenNETCF.ORM;


    [Entity(KeyScheme.None)]
    public class Orders
    {

        private static Orders ORM_CreateProxy(OpenNETCF.ORM.FieldAttributeCollection fields, System.Data.IDataReader results)
        {
            var item = new Orders();
            foreach (var field in fields)
            {
                var value = results[field.Ordinal];
                switch (field.FieldName)
                {
                    case "OrderID":
                        // If this is a TimeSpan, use the commented line below
                        // item.OrderID = (value == DBNull.Value) ? TimeSpan.MinValue; : new TimeSpan((long)value);
                        item.OrderID = (value == DBNull.Value) ? 0 : (long)value;
                        break;
                    case "CustomerID":
                        item.CustomerID = (value == DBNull.Value) ? null : (string)value;
                        break;
                    case "EmployeeID":
                        // If this is a TimeSpan, use the commented line below
                        // item.EmployeeID = (value == DBNull.Value) ? null : new TimeSpan((long)value);
                        item.EmployeeID = (value == DBNull.Value) ? 0 : (long?)value;
                        break;
                    case "OrderDate":
                        item.OrderDate = (value == DBNull.Value) ? null : (DateTime?)value;
                        break;
                    case "RequiredDate":
                        item.RequiredDate = (value == DBNull.Value) ? null : (DateTime?)value;
                        break;
                    case "ShippedDate":
                        item.ShippedDate = (value == DBNull.Value) ? null : (DateTime?)value;
                        break;
                    case "Freight":
                        item.Freight = (value == DBNull.Value) ? 0 : (decimal?)value;
                        break;
                    case "ShipName":
                        item.ShipName = (value == DBNull.Value) ? null : (string)value;
                        break;
                    case "ShipAddress":
                        item.ShipAddress = (value == DBNull.Value) ? null : (string)value;
                        break;
                    case "ShipCity":
                        item.ShipCity = (value == DBNull.Value) ? null : (string)value;
                        break;
                    case "ShipRegion":
                        item.ShipRegion = (value == DBNull.Value) ? null : (string)value;
                        break;
                    case "ShipPostalCode":
                        item.ShipPostalCode = (value == DBNull.Value) ? null : (string)value;
                        break;
                    case "ShipCountry":
                        item.ShipCountry = (value == DBNull.Value) ? null : (string)value;
                        break;
                }
            }
            return item;
        }

        private long m_orderid;

        private string m_customerid;

        private System.Nullable<long> m_employeeid;

        private System.Nullable<System.DateTime> m_orderdate;

        private System.Nullable<System.DateTime> m_requireddate;

        private System.Nullable<System.DateTime> m_shippeddate;

        private System.Nullable<decimal> m_freight;

        private string m_shipname;

        private string m_shipaddress;

        private string m_shipcity;

        private string m_shipregion;

        private string m_shippostalcode;

        private string m_shipcountry;

        private Customers m_refCustomers;

        [Field(IsPrimaryKey = true)]
        public long OrderID
        {
            get
            {
                return this.m_orderid;
            }
            set
            {
                this.m_orderid = value;
            }
        }

        [Field(SearchOrder = FieldSearchOrder.Ascending)]
        public string CustomerID
        {
            get
            {
                return this.m_customerid;
            }
            set
            {
                this.m_customerid = value;
            }
        }

        [Field(SearchOrder = FieldSearchOrder.Ascending)]
        public System.Nullable<long> EmployeeID
        {
            get
            {
                return this.m_employeeid;
            }
            set
            {
                this.m_employeeid = value;
            }
        }

        [Field(SearchOrder = FieldSearchOrder.Ascending)]
        public System.Nullable<System.DateTime> OrderDate
        {
            get
            {
                return this.m_orderdate;
            }
            set
            {
                this.m_orderdate = value;
            }
        }

        [Field()]
        public System.Nullable<System.DateTime> RequiredDate
        {
            get
            {
                return this.m_requireddate;
            }
            set
            {
                this.m_requireddate = value;
            }
        }

        [Field(SearchOrder = FieldSearchOrder.Ascending)]
        public System.Nullable<System.DateTime> ShippedDate
        {
            get
            {
                return this.m_shippeddate;
            }
            set
            {
                this.m_shippeddate = value;
            }
        }

        [Field()]
        public System.Nullable<decimal> Freight
        {
            get
            {
                return this.m_freight;
            }
            set
            {
                this.m_freight = value;
            }
        }

        [Field()]
        public string ShipName
        {
            get
            {
                return this.m_shipname;
            }
            set
            {
                this.m_shipname = value;
            }
        }

        [Field()]
        public string ShipAddress
        {
            get
            {
                return this.m_shipaddress;
            }
            set
            {
                this.m_shipaddress = value;
            }
        }

        [Field()]
        public string ShipCity
        {
            get
            {
                return this.m_shipcity;
            }
            set
            {
                this.m_shipcity = value;
            }
        }

        [Field()]
        public string ShipRegion
        {
            get
            {
                return this.m_shipregion;
            }
            set
            {
                this.m_shipregion = value;
            }
        }

        [Field(SearchOrder = FieldSearchOrder.Ascending)]
        public string ShipPostalCode
        {
            get
            {
                return this.m_shippostalcode;
            }
            set
            {
                this.m_shippostalcode = value;
            }
        }

        [Field()]
        public string ShipCountry
        {
            get
            {
                return this.m_shipcountry;
            }
            set
            {
                this.m_shipcountry = value;
            }
        }

        [Reference(typeof(Customers), "CustomerID", ReferenceType = ReferenceType.ManyToOne)]
        public Customers Customer
        {
            get
            {
                return this.m_refCustomers;
            }
            set
            {
                this.m_refCustomers = value;
            }
        }
    }
}
