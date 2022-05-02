using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Gamemodes.Flags;

namespace Uncreated.Warfare;

[Serializable]
public class ZoneReadException : Exception
{
    internal new ZoneModel Data;
    public ZoneReadException() { }
    public ZoneReadException(string message) : base(message) { }
    public ZoneReadException(string message, Exception inner) : base(message, inner) { }
    protected ZoneReadException(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    public override string ToString() => "Zone read exception (name = " + (Data.Name ?? "null") + "\n" + base.ToString();
}


[Serializable]
public class ZoneAPIException : Exception
{
    public ZoneAPIException() { }
    public ZoneAPIException(string message) : base(message) { }
    public ZoneAPIException(string message, Exception inner) : base(message, inner) { }
    protected ZoneAPIException(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}