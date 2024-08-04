using DanielWillett.ReflectionTools;
using System;

namespace Uncreated.Warfare.Exceptions;

/// <summary>
/// Thrown when a required Unturned RPC couldn't be found or wasn't the right type.
/// </summary>
public class UnturnedRpcNotFoundException : Exception
{
    public UnturnedRpcNotFoundException(Type declaringType, Type rpcType, string name)
        : base($"Unturned RPC not found in {Accessor.ExceptionFormatter.Format(declaringType)}: \"{name}\" of type {Accessor.ExceptionFormatter.Format(rpcType)}.") { }
}