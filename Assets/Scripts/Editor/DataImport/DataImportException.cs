#if UNITY_EDITOR
using System;

public sealed class DataImportException : Exception
{
    public DataImportException(string message)
        : base(message)
    {
    }

    public DataImportException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
#endif
