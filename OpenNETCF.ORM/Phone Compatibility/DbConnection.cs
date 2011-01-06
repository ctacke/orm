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
    public interface DbConnection : IDisposable
    {
        void Open();
        void Close();
    }
#endif
}
