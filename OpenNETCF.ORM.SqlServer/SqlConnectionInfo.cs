using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenNETCF.ORM.SqlServer
{
    public class SqlConnectionInfo : ICloneable
    {
        public SqlConnectionInfo(string serverName, string databaseName)
        {
            ServerName = serverName;
            DatabaseName = databaseName;
        }

        public string ServerName { get; set; }
        public string InstanceName { get; set; }
        public string DatabaseName { get; set; }
        public string UserDomain { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }

        public object Clone()
        {
            return new SqlConnectionInfo(this.ServerName, this.DatabaseName)
            {
                InstanceName = this.InstanceName,
                UserDomain = this.UserDomain,
                UserName = this.UserName,
                Password = this.Password
            };
        }
    }
}
