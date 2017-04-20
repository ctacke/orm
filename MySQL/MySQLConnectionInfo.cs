using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenNETCF.ORM
{
    public class MySQLConnectionInfo
    {
        public MySQLConnectionInfo(string serverAddress, int serverPort, string databaseName, string userName, string password)
        {
            ServerAddress = serverAddress;
            ServerPort = serverPort;
            DatabaseName = databaseName;
            UserName = userName;
            Password = password;
        }

        public string ServerAddress { get; private set; }
        public int ServerPort { get; private set; }
        public string DatabaseName { get; private set; }
        public string UserName { get; private set; }
        public string Password { get; private set; }
    }
}
