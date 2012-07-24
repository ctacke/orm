#if WINDOWS_PHONE

namespace System.Data.Common
{
    public interface IDataParameter
    {
        //DbType DbType { get; set; }
        //ParameterDirection Direction { get; set; }
        //bool IsNullable { get; }
        string ParameterName { get; set; }
        //string SourceColumn { get; set; }
        //DataRowVersion SourceVersion { get; set; }
        object Value { get; set; }
    }

    public interface DbDataReader : IDisposable
    {
        bool Read();
        object this[int ordinal] { get; }
        int GetOrdinal(string fieldname);
    }
}

#endif
