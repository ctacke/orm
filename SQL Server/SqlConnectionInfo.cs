using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenNETCF.ORM
{
    public class SqlConnectionInfo : ICloneable
    {
        public SqlConnectionInfo()
        {
        }

        public string ServerName { get; set; }
        public int ServerPort { get; set; }
        public string InstanceName { get; set; }
        public string DatabaseName { get; set; }
        public string UserDomain { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }

        public object Clone()
        {
            return new SqlConnectionInfo()
            {
                ServerName = this.ServerName,
                ServerPort = this.ServerPort,
                DatabaseName = this.DatabaseName,
                InstanceName = this.InstanceName,
                UserDomain = this.UserDomain,
                UserName = this.UserName,
                Password = this.Password
            };
        }
    }
}
