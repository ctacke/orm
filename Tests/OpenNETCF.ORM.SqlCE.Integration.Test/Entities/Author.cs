using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlServerCe;
using System.Data;

namespace OpenNETCF.ORM.Test.Entities
{
    [Entity(KeyScheme.Identity)]
    public class Author
    {
        private static Author ORM_CreateProxy(FieldAttributeCollection fields, IDataReader results)
        {
            var item = new Author();

            foreach (var field in fields)
            {
                var value = results[field.Ordinal];

                switch (field.FieldName)
                {
                    case "Name":
                        item.Name = value == DBNull.Value ? null : (string)value;
                        break;
                    // fill in any additional properties here
                }
            }

            return item;
        }

        [Field(IsPrimaryKey = true)]
        public int AuthorID { get; set; }

        [Reference(typeof(Book), "AuthorID", Autofill = true)]
        public Book[] Books { get; set; }

        [Field(SearchOrder = FieldSearchOrder.Ascending)]
        public string Name { get; set; }
    }
}
