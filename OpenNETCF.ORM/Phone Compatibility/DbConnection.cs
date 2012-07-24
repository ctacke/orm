#if WINDOWS_PHONE

namespace System.Data.Common
{
    public interface IDbConnection : IDisposable
    {
        void Open();
        void Close();
    }
}

#endif
