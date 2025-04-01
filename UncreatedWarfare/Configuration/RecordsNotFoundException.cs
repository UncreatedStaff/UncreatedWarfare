using System;

namespace Uncreated.Warfare.Configuration;

/// <summary>
/// Thrown when certain information (usually in configuration) isn't available for use.
/// </summary>
public class RecordsNotFoundException : Exception
{
    public RecordsNotFoundException() : base("Unable to find a configured record assiciated with the given identifier.") { }
    public RecordsNotFoundException(string message) : base(message) { }
    public RecordsNotFoundException(string message, Exception inner) : base(message, inner) { }
}