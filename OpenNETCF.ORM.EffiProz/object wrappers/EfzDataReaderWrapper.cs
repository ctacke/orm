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
    internal class EfzDataReaderWrapper : DbDataReader
    {
        private EfzDataReader m_reader;

        internal EfzDataReaderWrapper(EfzDataReader internalReader)
        {
            m_reader = internalReader;
        }

        public void Dispose()
        {
            m_reader.Dispose();
        }

        public bool Read()
        {
            return m_reader.Read();
        }

        public object this[int ordinal]
        {
            get { return m_reader[ordinal]; }
        }
    }
}
