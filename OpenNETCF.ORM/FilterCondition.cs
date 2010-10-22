using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace OpenNETCF.ORM
{
    public class FilterCondition
    {
        public string FieldName { get; set; }
        public object Value { get; set; }
        public FilterOperator Operator { get; set; }

        public enum FilterOperator
        {
            Equals,
            Like,
            LessThan,
            GreaterThan
        }
    }
}
