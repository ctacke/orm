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

namespace System.Data.Common
{
#if WINDOWS_PHONE
    public interface DbCommand : IDisposable
    {
        string CommandText { get;  set; }
        DbConnection Connection { set; }

        int ExecuteNonQuery();
        object ExecuteScalar();
        DbDataReader ExecuteReader();
    }
#endif
}
