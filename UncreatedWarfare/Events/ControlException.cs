using System;

namespace Uncreated.Warfare.Events;

/// <summary>Meant purely to break execution.</summary>
public class ControlException : Exception
{
    public ControlException() { }
    public ControlException(string message) : base(message) { }
}