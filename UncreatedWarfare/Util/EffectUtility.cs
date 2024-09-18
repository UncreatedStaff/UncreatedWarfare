using SDG.NetTransport;
using System;
using Uncreated.Warfare.Configuration;

namespace Uncreated.Warfare.Util;

/// <summary>
/// Utilities for spawning effects easier.
/// </summary>
public static class EffectUtility
{
    /// <summary>
    /// Allows modifying <see cref="TriggerEffectParameters"/> using a callback.
    /// </summary>
    public delegate void ModifyTriggerEffectParameters(ref TriggerEffectParameters p);

    /// <summary>
    /// Trigger an effect with a custom parameter modifier.
    /// </summary>
    public static void TriggerEffect(IAssetLink<EffectAsset> asset, bool reliable, ModifyTriggerEffectParameters callback)
    {
        GameThread.AssertCurrent();

        TriggerEffect(asset.GetAsset()!, reliable, callback);
    }

    /// <summary>
    /// Trigger an effect with a custom parameter modifier.
    /// </summary>
    public static void TriggerEffect(EffectAsset asset, bool reliable, ModifyTriggerEffectParameters callback)
    {
        GameThread.AssertCurrent();

        if (asset == null)
            return;

        TriggerEffectParameters parameters = new TriggerEffectParameters(asset)
        {
            reliable = reliable
        };

        callback(ref parameters);

        EffectManager.triggerEffect(parameters);
    }

    /// <summary>
    /// Trigger an effect for one player.
    /// </summary>
    public static void TriggerEffect(IAssetLink<EffectAsset> asset, ITransportConnection connection, Vector3 position, bool reliable)
    {
        GameThread.AssertCurrent();

        TriggerEffect(asset.GetAsset()!, connection, position, reliable);
    }

    /// <summary>
    /// Trigger an effect for one player.
    /// </summary>
    public static void TriggerEffect(EffectAsset asset, ITransportConnection connection, Vector3 position, bool reliable)
    {
        GameThread.AssertCurrent();

        if (asset == null)
            return;

        TriggerEffectParameters parameters = new TriggerEffectParameters(asset);

        parameters.SetRelevantPlayer(connection);

        parameters.position = position;
        parameters.reliable = reliable;

        EffectManager.triggerEffect(parameters);
    }

    /// <summary>
    /// Trigger an effect for multiple players.
    /// </summary>
    public static void TriggerEffect(IAssetLink<EffectAsset> asset, PooledTransportConnectionList connections, Vector3 position, bool reliable)
    {
        GameThread.AssertCurrent();

        TriggerEffect(asset.GetAsset()!, connections, position, reliable);
    }

    /// <summary>
    /// Trigger an effect for multiple players.
    /// </summary>
    public static void TriggerEffect(EffectAsset asset, PooledTransportConnectionList connections, Vector3 position, bool reliable)
    {
        GameThread.AssertCurrent();

        if (asset == null)
            return;

        TriggerEffectParameters parameters = new TriggerEffectParameters(asset);

        parameters.SetRelevantTransportConnections(connections);

        parameters.position = position;
        parameters.reliable = reliable;

        EffectManager.triggerEffect(parameters);
    }

    /// <summary>
    /// Trigger an effect for multiple players.
    /// </summary>
    public static void TriggerEffect(IAssetLink<EffectAsset> asset, float range, Vector3 position, bool reliable)
    {
        GameThread.AssertCurrent();

        TriggerEffect(asset.GetAsset()!, range, position, reliable);
    }

    /// <summary>
    /// Trigger an effect for multiple players.
    /// </summary>
    public static void TriggerEffect(EffectAsset asset, float range, Vector3 position, bool reliable)
    {
        GameThread.AssertCurrent();

        if (asset == null || range < 0)
            return;

        TriggerEffectParameters parameters = new TriggerEffectParameters(asset);

        parameters.SetRelevantTransportConnections(Provider.GatherRemoteClientConnectionsWithinSphere(position, range));

        parameters.position = position;
        parameters.relevantDistance = range;
        parameters.reliable = reliable;

        EffectManager.triggerEffect(parameters);
    }

    /// <summary>
    /// Trigger an effect for multiple players with a color. It must use a shader that converts rotation and scale to color.
    /// </summary>
    public static void TriggerEffect(IAssetLink<EffectAsset> asset, ITransportConnection connection, Vector3 position, Color color, bool reliable)
    {
        GameThread.AssertCurrent();

        TriggerEffect(asset.GetAsset()!, connection, position, color, reliable);
    }

    /// <summary>
    /// Trigger an effect for multiple players with a color. It must use a shader that converts rotation and scale to color.
    /// </summary>
    public static void TriggerEffect(EffectAsset asset, ITransportConnection connection, Vector3 position, Color color, bool reliable)
    {
        GameThread.AssertCurrent();

        if (asset == null)
            return;

        TriggerEffectParameters parameters = new TriggerEffectParameters(asset);

        parameters.SetRelevantPlayer(connection);
        SetColorIntl(in color, ref parameters);
        parameters.position = position;
        parameters.reliable = reliable;

        EffectManager.triggerEffect(parameters);
    }

    /// <summary>
    /// Trigger an effect with a custom parameter modifier.
    /// </summary>
    public static void TriggerEffect(IAssetLink<EffectAsset> asset, Color color, bool reliable, ModifyTriggerEffectParameters callback)
    {
        GameThread.AssertCurrent();

        TriggerEffect(asset.GetAsset()!, color, reliable, callback);
    }

    /// <summary>
    /// Trigger an effect with a custom parameter modifier.
    /// </summary>
    public static void TriggerEffect(EffectAsset asset, Color color, bool reliable, ModifyTriggerEffectParameters callback)
    {
        GameThread.AssertCurrent();

        if (asset == null)
            return;

        TriggerEffectParameters parameters = new TriggerEffectParameters(asset);

        SetColorIntl(in color, ref parameters);
        parameters.reliable = reliable;
        callback(ref parameters);

        EffectManager.triggerEffect(parameters);
    }

    /// <summary>
    /// Trigger an effect for one player with a color. It must use a shader that converts rotation and scale to color.
    /// </summary>
    public static void TriggerEffect(IAssetLink<EffectAsset> asset, PooledTransportConnectionList connections, Vector3 position, Color color, bool reliable)
    {
        GameThread.AssertCurrent();

        TriggerEffect(asset.GetAsset()!, connections, position, color, reliable);
    }

    /// <summary>
    /// Trigger an effect for one player with a color. It must use a shader that converts rotation and scale to color.
    /// </summary>
    public static void TriggerEffect(EffectAsset asset, PooledTransportConnectionList connections, Vector3 position, Color color, bool reliable)
    {
        GameThread.AssertCurrent();

        if (asset == null)
            return;

        TriggerEffectParameters parameters = new TriggerEffectParameters(asset);

        parameters.SetRelevantTransportConnections(connections);
        SetColorIntl(in color, ref parameters);
        parameters.position = position;
        parameters.reliable = reliable;

        EffectManager.triggerEffect(parameters);
    }

    /// <summary>
    /// Trigger an effect for one player with a color. It must use a shader that converts rotation and scale to color.
    /// </summary>
    public static void TriggerEffect(IAssetLink<EffectAsset> asset, float range, Vector3 position, Color color, bool reliable)
    {
        GameThread.AssertCurrent();

        TriggerEffect(asset.GetAsset()!, range, position, color, reliable);
    }

    /// <summary>
    /// Trigger an effect for one player with a color. It must use a shader that converts rotation and scale to color.
    /// </summary>
    public static void TriggerEffect(EffectAsset asset, float range, Vector3 position, Color color, bool reliable)
    {
        GameThread.AssertCurrent();

        if (asset == null || range < 0)
            return;

        TriggerEffectParameters parameters = new TriggerEffectParameters(asset);

        parameters.SetRelevantTransportConnections(Provider.GatherRemoteClientConnectionsWithinSphere(position, range));
        SetColorIntl(in color, ref parameters);
        parameters.position = position;
        parameters.reliable = reliable;

        EffectManager.triggerEffect(parameters);
    }

    private static void SetColorIntl(in Color color, ref TriggerEffectParameters parameters)
    {
        Vector3 forward = default;
        forward.x = color.r;
        forward.y = color.g;
        forward.z = color.b;

        float scale = MathF.Sqrt(color.r * color.r + color.g * color.g + color.b * color.b);
        forward.x /= scale;
        forward.y /= scale;
        forward.z /= scale;

        parameters.SetDirection(forward);

        parameters.scale.x = scale;
        parameters.scale.y = scale;
        parameters.scale.z = scale;
    }
}
