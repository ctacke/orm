using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenNETCF.ORM
{
    partial class MySQLDataStore
    {
        public override IEnumerable<DynamicEntity> Select(string entityName)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<DynamicEntity> Fetch(string entityName, int fetchCount)
        {
            throw new NotImplementedException();
        }

        public override void DiscoverDynamicEntity(string entityName)
        {
            throw new NotImplementedException();
        }

        public override DynamicEntity Select(string entityName, object primaryKey)
        {
            throw new NotImplementedException();
        }
    }
}
