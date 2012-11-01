using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenNETCF.ORM.SQLite.Integration.Test
{
    [Entity(KeyScheme=KeyScheme.Identity, NameInStore="Ordini")]
    public class Order
    {
        [Field(FieldName = "idOrdine", IsPrimaryKey = true)]
        public int OrderID { get; set; }

        [Field(FieldName = "numeroOrdine")]
        public int OrderNumber { get; set; }

        [Field(FieldName = "numeroOrdineSede")]
        public string OrderNumberOffice { get; set; }

        [Field(FieldName = "dataOrdine")]
        public int OrderDate { get; set; }

        [Field(FieldName = "dataConsegna")]
        public int DeliveryDate { get; set; }

        [Field(FieldName = "riferimento")]
        public string Reference { get; set; }

        [Field(FieldName = "noteCliente")]
        public string ClientNote { get; set; }

        [Field(FieldName = "noteOrdine")]
        public string OrderNote { get; set; }

        [Field(FieldName = "idCliente")]
        public int CustomerID { get; set; }

        [Reference(typeof(Customer), "idCliente", ReferenceType = ReferenceType.ManyToOne)]
        public Customer Customer { get; set; }

        [Field(FieldName = "idFiliale")]
        public int DestinationID { get; set; }

        [Reference(typeof(Destination), "idFiliale", ReferenceType = ReferenceType.ManyToOne)]
        public Destination Destination { get; set; }

        [Field(FieldName = "stato")]
        public string Status { get; set; }
    }

    [Entity(KeyScheme = KeyScheme.Identity, NameInStore = "Filiali")]
    public class Destination
    {
        [Field(FieldName = "idFiliale", IsPrimaryKey=true)]
        public int DestinationID { get; set; }

        [Field(FieldName = "idFilialeRemoto")]
        public int RemoteCustomerID { get; set; }

        [Field(FieldName = "idCliente")]
        public int CustomerID { get; set; }

        [Field(FieldName = "codFiliale")]
        public string Name { get; set; }

        [Field(FieldName = "ragioneSociale")]
        public string Region { get; set; }

        [Field(FieldName = "indirizzo")]
        public string Address { get; set; }

        [Field(FieldName = "citta")]
        public string City { get; set; }

        [Field(FieldName = "provincia")]
        public string Province { get; set; }

        [Field(FieldName = "cap")]
        public string Cap { get; set; }

        [Field(FieldName = "frazione")]
        public string Frazione { get; set; }

        [Field(FieldName = "telefono")]
        public string Telephone { get; set; }

        [Field(FieldName = "cellulare")]
        public string Cell { get; set; }

        [Field(FieldName = "fax")]
        public string Fax { get; set; }

        [Field(FieldName = "email")]
        public string Email { get; set; }

        [Field(FieldName = "stato")]
        public string State { get; set; }

        [Field(FieldName = "attivo")]
        public int Active { get; set; }
    }

    [Entity(KeyScheme = KeyScheme.Identity, NameInStore = "Clienti")]
    public class Customer
    {
        [Field(FieldName = "idCliente", IsPrimaryKey=true)]
        public int CustomerID { get; set; }

        [Field(FieldName = "idClienteRemoto")]
        public int RemoteCustomerID { get; set; }

        [Field(FieldName = "codCliente")]
        public string Name { get; set; }

        [Field(FieldName = "indirizzo")]
        public string Address { get; set; }

        [Field(FieldName = "ragioneSociale")]
        public string Region { get; set; }

        [Field(FieldName = "citta")]
        public string City { get; set; }

        [Field(FieldName = "provincia")]
        public string Province { get; set; }

        [Field(FieldName = "cap")]
        public string Cap { get; set; }

        [Field(FieldName = "frazione")]
        public string Frazione { get; set; }

        [Field(FieldName = "telefono")]
        public string Telephone { get; set; }

        [Field(FieldName = "cellulare")]
        public string Cell { get; set; }

        [Field(FieldName = "fax")]
        public string Fax { get; set; }

        [Field(FieldName = "email")]
        public string Email { get; set; }

        [Field(FieldName = "partitaIVA")]
        public string PartitaIVA { get; set; }

        [Field(FieldName = "codiceFiscale")]
        public string TaxCode { get; set; }

        [Field(FieldName = "note")]
        public string Notes { get; set; }

        [Field(FieldName = "noteOrdine")]
        public string OrderNote { get; set; }

        [Field(FieldName = "stato")]
        public string State { get; set; }

        [Field(FieldName = "attivo")]
        public int Active { get; set; }
    }
}
