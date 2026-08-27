using SDG.NetTransport;
using System;
using System.Diagnostics;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Patches;

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
        LastEffectSpawnedOnDedicatedServerPatch.LastEffectSpawned = null;
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
        LastEffectSpawnedOnDedicatedServerPatch.LastEffectSpawned = null;
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
        LastEffectSpawnedOnDedicatedServerPatch.LastEffectSpawned = null;
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
        LastEffectSpawnedOnDedicatedServerPatch.LastEffectSpawned = null;
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
        LastEffectSpawnedOnDedicatedServerPatch.LastEffectSpawned = null;
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
        LastEffectSpawnedOnDedicatedServerPatch.LastEffectSpawned = null;
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
        LastEffectSpawnedOnDedicatedServerPatch.LastEffectSpawned = null;
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
        LastEffectSpawnedOnDedicatedServerPatch.LastEffectSpawned = null;
    }

    private static EffectAsset? _debugEffect;

    private static bool CheckDebugEffect([NotNullWhen(true)] out EffectAsset? debugEffect)
    {
        _debugEffect ??= Assets.find<EffectAsset>(new Guid("6093290a7ce049b8a418be7fd79e89a0"));

        debugEffect = _debugEffect;
        return debugEffect != null;
    }

    [Conditional("DEBUG")]
    public static void ClearDebugEffect(ITransportConnection? connection = null)
    {
        GameThread.AssertCurrent();

        if (!CheckDebugEffect(out EffectAsset? debugEffect))
            return;

        if (connection == null)
            EffectManager.ClearEffectByGuid_AllPlayers(debugEffect.GUID);
        else
            EffectManager.ClearEffectByGuid(debugEffect.GUID, connection);
    }

    [Conditional("DEBUG")]
    public static void TriggerDebugEffect(Vector3 position, Vector3 direction, Vector3 left, bool clear = true, float scale = 1f)
    {
        TriggerDebugEffect(null, position, Quaternion.LookRotation(direction, Vector3.Cross(left, direction)), clear);
    }

    [Conditional("DEBUG")]
    public static void TriggerDebugEffect(Vector3 position, Quaternion rotation, bool clear = true, float scale = 1f)
    {
        TriggerDebugEffect(null, position, rotation, clear, scale);
    }

    [Conditional("DEBUG")]
    public static void TriggerDebugEffect(ITransportConnection? connection, Vector3 position, Vector3 direction, Vector3 left, bool clear = true, float scale = 1f)
    {
        TriggerDebugEffect(connection, position, Quaternion.LookRotation(direction, Vector3.Cross(left, direction)), clear);
    }

    [Conditional("DEBUG")]
    public static void TriggerDebugEffect(ITransportConnection? connection, Vector3 position, Quaternion rotation, bool clear = true, float scale = 1f)
    {
        GameThread.AssertCurrent();

        if (!CheckDebugEffect(out EffectAsset? debugEffect))
            return;

        if (clear)
        {
            EffectManager.ClearEffectByGuid(debugEffect.GUID, connection);
        }

        TriggerEffectParameters p = new TriggerEffectParameters(debugEffect)
        {
            position = position,
            reliable = false
        };

        p.SetRotation(rotation);

        if (connection != null)
        {
            p.SetRelevantPlayer(connection);
        }
        else
        {
            p.relevantDistance = EffectManager.INSANE;
        }

        if (!Mathf.Approximately(scale, 1f))
        {
            p.SetUniformScale(scale);
        }
        EffectManager.triggerEffect(p);
        LastEffectSpawnedOnDedicatedServerPatch.LastEffectSpawned = null;
    }

    [Conditional("DEBUG")]
    public static void TriggerDebugEffectBox(Vector3 center, Vector3 extents, Quaternion rotation, bool clear, float effectScale = 1f)
    {
        TriggerDebugEffectBox(null, center, extents, rotation, clear, effectScale);
    }

    [Conditional("DEBUG")]
    public static void TriggerDebugEffectBox(ITransportConnection? connection, Vector3 center, Vector3 extents, Quaternion rotation, bool clear, float effectScale = 1f)
    {
        GameThread.AssertCurrent();

        if (!CheckDebugEffect(out EffectAsset? debugEffect))
            return;

        if (clear)
        {
            EffectManager.ClearEffectByGuid(debugEffect.GUID, connection);
        }

        TriggerDebugEffect(center + extents, rotation * Quaternion.Euler(0f, 270f, 180f), clear: false, scale: effectScale);
        TriggerDebugEffect(center - extents, rotation, clear: false, scale: effectScale);
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