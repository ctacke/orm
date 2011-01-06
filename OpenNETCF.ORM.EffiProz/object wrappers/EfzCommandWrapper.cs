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
    internal class EfzCommandWrapper : DbCommand
    {
        private EfzCommand m_command;

        public EfzCommandWrapper()
        {
            m_command = new EfzCommand();
        }

        public void Dispose()
        {
            m_command.Dispose();
        }

        public string CommandText
        {
            get { return m_command.CommandText; }
            set { m_command.CommandText = value; }
        }

        public DbConnection Connection
        {
            set { m_command.Connection = ((EfzConnectionWrapper)value).m_connection; }
        }

        public int ExecuteNonQuery()
        {
            return m_command.ExecuteNonQuery();
        }

        public object ExecuteScalar()
        {
            return m_command.ExecuteScalar();
        }

        public DbDataReader ExecuteReader()
        {
            var reader = m_command.ExecuteReader();
            return new EfzDataReaderWrapper(reader);
        }
    }
}
