using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Exceptions;
using System.Diagnostics.CodeAnalysis;


namespace Uncreated.Warfare.Networking;

// implemented by Uncreated.Web (RpcConnectionService)
public interface IRpcConnectionService
{
    /// <summary>
    /// Get the connection for Warfare.
    /// </summary>
    IModularRpcRemoteConnection? TryGetWarfareConnection();

    /// <summary>
    /// Get the connection for Warfare.
    /// </summary>
    /// <exception cref="RpcNoConnectionsException"/>
    IModularRpcRemoteConnection GetWarfareConnection();

    /// <summary>
    /// Get the connection for Warfare.
    /// </summary>
    bool TryGetWarfareConnection([MaybeNullWhen(false)] out IModularRpcRemoteConnection connection);
}

public class NullRpcConnectionService : IRpcConnectionService
{
    /// <inheritdoc />
    public IModularRpcRemoteConnection? TryGetWarfareConnection()
    {
        return null;
    }

    /// <inheritdoc />
    public IModularRpcRemoteConnection GetWarfareConnection()
    {
        throw new RpcNoConnectionsException($"Unable to find connection of server type Warfare.");
    }

    /// <inheritdoc />
    public bool TryGetWarfareConnection([MaybeNullWhen(false)] out IModularRpcRemoteConnection connection)
    {
        connection = null;
        return false;
    }
}