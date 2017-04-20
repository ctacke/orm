using System;
using System.Net;

namespace System.Data.Common
{
#if WINDOWS_PHONE
    public interface DbDataReader : IDisposable
    {
        bool Read();
        object this[int ordinal] { get; }
    }
#endif
}
