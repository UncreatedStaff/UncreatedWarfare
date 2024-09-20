using DanielWillett.ReflectionTools;
using System;
using Uncreated.Warfare.Exceptions;
using Uncreated.Warfare.Logging;

namespace Uncreated.Warfare.Util;
public static class ReflectionUtility
{
    /// <summary>
    /// Get an RPC from a game class.
    /// </summary>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="UnturnedRpcNotFoundException">The RPC couldn't be found or isn't the right type.</exception>
    public static TRpcType FindRequiredRpc<TDeclaringType, TRpcType>(string name) where TRpcType : ClientMethodHandle
    {
        return FindRpc<TDeclaringType, TRpcType>(name) ?? throw new UnturnedRpcNotFoundException(typeof(TDeclaringType), typeof(TRpcType), name);
    }

    /// <summary>
    /// Get an RPC from a game class, or <see langword="null"/> if it's not found or isn't the right type.
    /// </summary>
    /// <exception cref="ArgumentException"/>
    public static TRpcType? FindRpc<TDeclaringType, TRpcType>(string name) where TRpcType : ClientMethodHandle
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is empty.", nameof(name));

        TRpcType? rpc = Variables.FindStatic<TDeclaringType, TRpcType>(name)?.GetValue();
        if (rpc == null)
        {
            L.Logger.LogWarning("RPC not found in {0}: \"{1}\" of type {2}.", Accessor.ExceptionFormatter.Format(typeof(TDeclaringType)), name, Accessor.ExceptionFormatter.Format(typeof(TRpcType)));
        }

        return rpc;
    }
}