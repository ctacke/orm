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
        private static Dictionary<string, int> m_nameToOrdinalMap;

        private static Author ORM_CreateProxy(FieldAttributeCollection fields, IDataReader results)
        {
            if (m_nameToOrdinalMap == null)
            {
                m_nameToOrdinalMap = new Dictionary<string, int>();

                foreach (var field in fields)
                {
                    m_nameToOrdinalMap.Add(field.FieldName, results.GetOrdinal(field.FieldName));
                }
            }

            var item = new Author();

            foreach (var field in fields)
            {
                var value = results[field.Ordinal];
                // var value = results[results.GetOrdinal(field.FieldName)];
                var val = results[m_nameToOrdinalMap[field.FieldName]];

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
