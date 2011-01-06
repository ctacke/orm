using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Data.Common;
using System.Data.EffiProz;

namespace OpenNETCF.ORM.EffiProz
{
    // this class is necessary since WinPhone doesn't support System.Data.Common base classes
    internal class EfzConnectionWrapper : DbConnection
    {
        internal EfzConnection m_connection;

        public EfzConnectionWrapper(string connectionString)
        {
            m_connection = new EfzConnection(connectionString);
        }

        public void Dispose()
        {
            m_connection.Dispose();
        }

        public void Open()
        {
            m_connection.Open();
        }

        public void Close()
        {
            m_connection.Close();
        }
    }
}
