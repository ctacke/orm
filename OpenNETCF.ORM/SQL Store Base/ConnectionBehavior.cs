using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace OpenNETCF.ORM
{
    public enum ConnectionBehavior
    {
        /// <summary>
        /// Creates a new connection for every access to the database
        /// </summary>
        AlwaysNew,
        /// <summary>
        /// The store keeps a separate connection to the database open for life.  This connection is only used for maintenenace-type calls.  All data access calls are still done with a new connection every time.
        /// </summary>
        HoldMaintenance,
        /// <summary>
        /// The store holds a single connection for life.  It uses that one connection for all database access.
        /// </summary>
        Persistent
    }
}
