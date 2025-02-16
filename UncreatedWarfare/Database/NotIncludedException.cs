using System;
using System.Runtime.Serialization;

namespace Uncreated.Warfare.Database;

/// <summary>
/// Thrown when data is not included in a query.
/// </summary>
public class NotIncludedException : Exception
{
    public NotIncludedException(string propertyName) : base(GetMessage(propertyName)) { }
    public NotIncludedException(string propertyName, Exception inner) : base(GetMessage(propertyName), inner) { }
    protected NotIncludedException(SerializationInfo info, StreamingContext context) : base(info, context) { }

    private static string GetMessage(string propertyName) => $"Property {propertyName} was not included in the query.";
}