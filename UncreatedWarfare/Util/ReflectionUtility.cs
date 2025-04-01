using DanielWillett.ReflectionTools;
using Microsoft.Extensions.DependencyInjection;
using System;
using Uncreated.Warfare.Exceptions;

namespace Uncreated.Warfare.Util;
public static class ReflectionUtility
{
    /// <summary>
    /// Get an RPC from a game class.
    /// </summary>
    /// <remarks>Always returns null on non-Unturned environments.</remarks>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="UnturnedRpcNotFoundException">The RPC couldn't be found or isn't the right type.</exception>
    public static TRpcType FindRequiredRpc<TDeclaringType, TRpcType>(string name) where TRpcType : ClientMethodHandle
    {
        if (!WarfareModule.IsActive)
            return null!;
        return FindRpc<TDeclaringType, TRpcType>(name) ?? throw new UnturnedRpcNotFoundException(typeof(TDeclaringType), typeof(TRpcType), name);
    }

    /// <summary>
    /// Get an RPC from a game class, or <see langword="null"/> if it's not found or isn't the right type.
    /// </summary>
    /// <remarks>Always returns null on non-Unturned environments.</remarks>
    /// <exception cref="ArgumentException"/>
    public static TRpcType? FindRpc<TDeclaringType, TRpcType>(string name) where TRpcType : ClientMethodHandle
    {
        if (!WarfareModule.IsActive)
            return null!;

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is empty.", nameof(name));

        TRpcType? rpc = Variables.FindStatic<TDeclaringType, TRpcType>(name)?.GetValue();
        if (rpc == null)
        {
            WarfareModule.Singleton.GlobalLogger.LogWarning("RPC not found in {0}: \"{1}\" of type {2}.", Accessor.ExceptionFormatter.Format(typeof(TDeclaringType)), name, Accessor.ExceptionFormatter.Format(typeof(TRpcType)));
        }

        return rpc;
    }
    
    /// <summary>
    /// <see cref="ActivatorUtilities"/> throws an error if none of the parameters in the given object array are used for some reason. This catches that and falls back to just using the service provider.
    /// </summary>
    internal static object CreateInstanceFixed(IServiceProvider serviceProvider, Type instanceType, object[] parameters)
    {
        try
        {
            return ActivatorUtilities.CreateInstance(serviceProvider, instanceType, parameters);
        }
        catch (InvalidOperationException)
        {
            return ActivatorUtilities.CreateInstance(serviceProvider, instanceType, Array.Empty<object>());
        }
    }
    
    /// <summary>
    /// <see cref="ActivatorUtilities"/> throws an error if none of the parameters in the given object array are used for some reason. This catches that and falls back to just using the service provider.
    /// </summary>
    internal static TInstanceType CreateInstanceFixed<TInstanceType>(IServiceProvider serviceProvider, object[] parameters)
    {
        try
        {
            return ActivatorUtilities.CreateInstance<TInstanceType>(serviceProvider, parameters);
        }
        catch (InvalidOperationException)
        {
            return ActivatorUtilities.CreateInstance<TInstanceType>(serviceProvider, Array.Empty<object>());
        }
    }
}