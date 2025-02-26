using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;
using System;

namespace Uncreated.Warfare;

/// <summary>
/// A component that is alive as long as the plugin is active.
/// </summary>
/// <remarks>Can be used as the target for coroutines, UniTask functions, etc.</remarks>
public class WarfareLifetimeComponent : MonoBehaviour
{
    private WarfareModule _module = null!;
    private float _shutdownTime = -1f;

    private const string DefaultShutdownReason = "Unknown reason";

    public ShutdownMode QueuedShutdownType { get; private set; }
    public DateTime ShutdownTime { get; private set; }
    public string? ShutdownReason { get; private set; }

    private void Awake()
    {
        _module = WarfareModule.Singleton;
    }

    private void Update()
    {
        if (_shutdownTime > 0 && Time.realtimeSinceStartup >= _shutdownTime)
        {
            _ = _module.ShutdownAsync(ShutdownReason ?? DefaultShutdownReason, CancellationToken.None);
        }
    }

    [RpcSend]
    protected virtual RpcTask SendShutdownUpdate(string str, bool isShuttingDownNow) => RpcTask.NotImplemented;

    [RpcReceive]
    public string GetShutdownReason()
    {
        switch (QueuedShutdownType)
        {
            case ShutdownMode.Time:
                if (ShutdownReason == null)
                    return "Time|" + ShutdownTime.ToString("O");
                return "Time|" + ShutdownTime.ToString("O") + ShutdownReason;

            case ShutdownMode.OnLayoutEnd:
                if (ShutdownReason == null)
                    return "OnLayoutEnd";
                return "OnLayoutEnd|" + ShutdownReason;

            case ShutdownMode.Now:
                if (ShutdownReason == null)
                    return "Now";
                return "Now|" + ShutdownReason;

            default:
                return "None";
        }
    }

    [RpcReceive]
    public void QueueShutdownInTime(TimeSpan time, string? shutdownReason = null)
    {
        DateTime dt = DateTime.UtcNow.Add(time);
        if (QueuedShutdownType == ShutdownMode.Time && ShutdownTime > dt)
            return;

        ShutdownTime = dt;
        ShutdownReason = shutdownReason;
        QueuedShutdownType = ShutdownMode.Time;
        _shutdownTime = (float)(Time.realtimeSinceStartup + time.TotalSeconds);
        UpdateShutdownState();
    }

    [RpcReceive]
    public void QueueShutdownAtLayoutEnd(string? shutdownReason = null)
    {
        if (QueuedShutdownType == ShutdownMode.OnLayoutEnd)
            return;

        QueuedShutdownType = ShutdownMode.OnLayoutEnd;
        ShutdownReason = shutdownReason;
        ShutdownTime = default;
        _shutdownTime = -1;
        UpdateShutdownState();
    }

    [RpcReceive]
    public void CancelShutdown()
    {
        if (QueuedShutdownType == ShutdownMode.None)
            return;

        QueuedShutdownType = ShutdownMode.None;
        ShutdownTime = default;
        ShutdownReason = null;
        _shutdownTime = -1;
        UpdateShutdownState();
    }

    internal async Task NotifyShutdownNow(string? reason)
    {
        if (QueuedShutdownType != ShutdownMode.None && string.Equals(ShutdownReason, reason))
        {
            try
            {
                await SendShutdownUpdate(GetShutdownReason(), true);
            }
            catch
            {
                /* ignored */
            }
            return;
        }

        QueuedShutdownType = ShutdownMode.Now;
        ShutdownReason = reason ?? DefaultShutdownReason;
        ShutdownTime = default;
        _shutdownTime = -1;
        try
        {
            await SendShutdownUpdate(GetShutdownReason(), true);
        }
        catch
        {
            /* ignored */
        }
    }

    internal void UpdateShutdownState()
    {
        try
        {
            _ = SendShutdownUpdate(GetShutdownReason(), false);
        }
        catch
        {
            /* ignored */
        }
    }

    internal UniTask NotifyLayoutEnding(CancellationToken token = default)
    {
        if (QueuedShutdownType == ShutdownMode.OnLayoutEnd || QueuedShutdownType == ShutdownMode.Time && (ShutdownTime - DateTime.UtcNow).TotalSeconds < 30)
        {
            return _module.ShutdownAsync(ShutdownReason ?? DefaultShutdownReason, token);
        }

        return UniTask.CompletedTask;
    }
}

public enum ShutdownMode
{
    None,
    Time,
    OnLayoutEnd,
    Now
}