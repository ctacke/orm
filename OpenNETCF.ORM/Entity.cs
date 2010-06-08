using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace OpenNETCF.ORM
{
    public class Entity<T>
        where T : IEntity
    {
        private IDataStore m_database;

        public Entity(IDataStore database)
        {
            m_database = database;
        }
    }
}
