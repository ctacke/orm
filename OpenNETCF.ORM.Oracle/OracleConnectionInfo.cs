using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenNETCF.ORM
{
    public class OracleConnectionInfo
    {
        public OracleConnectionInfo(string serverAddress, int serverPort, string serviceName, string userName, string password)
        {
            ServerAddress = serverAddress;
            ServerPort = serverPort;
            ServiceName = serviceName;
            UserName = userName;
            Password = password;
        }

        public string ServerAddress { get; private set; }
        public int ServerPort { get; private set; }
        public string ServiceName { get; private set; }
        public string UserName { get; private set; }
        public string Password { get; private set; }
    }
}
