using System;
using System.Net;

namespace System.Data.Common
{
#if WINDOWS_PHONE
    public interface IDataParameter
    {
        DbType DbType { get; set; }
        ParameterDirection Direction { get; set; }
        bool IsNullable { get; }
        string ParameterName { get; set; }
        string SourceColumn { get; set; }
        DataRowVersion SourceVersion { get; set; }
        object Value { get; set; }
    }

    public enum ParameterDirection
    {
        Input = 1,
        Output = 2,
        InputOutput = 3,
        ReturnValue = 6,
    }

    public enum DataRowVersion
    {
        Original = 256,
        Current = 512,
        Proposed = 1024,
        Default = 1536,
    }
#endif
}
