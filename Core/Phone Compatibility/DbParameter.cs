#if WINDOWS_PHONE

namespace System.Data.Common
{

    public interface DbParameterCollection
    {
        int Count { get; }

        DbParameter this[int index] { get; set; }

        int Add(object value);
        void AddRange(Array values);
    }

    public interface DbParameter
    {
        object Value { get; set; }
    }
}

#endif
