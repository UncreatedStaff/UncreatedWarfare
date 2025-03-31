using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;
using System;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare;

/// <summary>
/// A component that is alive as long as the plugin is active.
/// </summary>
/// <remarks>Can be used as the target for coroutines, UniTask functions, etc.</remarks>
public class WarfareLifetimeComponent : MonoBehaviour
{
    // seconds
    private readonly float[] _shutdownSteps = [ 1, 2, 3, 4, 5, 15, 30, 60, 300, 900 ];
    private const float SecondsBetweenNextGameWarnings = 120f;           //  2 mins
    private const float SecondsUntilFallbackShutdownAfterUpdate = 2520f; // 45 mins

    private WarfareModule _module = null!;
    private ShutdownTranslations? _shutdownTranslations;
    private ChatService _chatService = null!;
    private float _shutdownTime = -1f;
    private float _lastLayoutShutdownWarning;
    private int _shutdownStep = -1;

    private const string DefaultShutdownReason = "unknown reason";

    public ShutdownMode QueuedShutdownType { get; private set; }
    public DateTime ShutdownTime { get; private set; }
    public string? ShutdownReason { get; private set; }

    // todo: start throws an error
    [UsedImplicitly]
    private void Awake()
    {
        _module = WarfareModule.Singleton;
    }

    [UsedImplicitly]
    private void Update()
    {
        if (_chatService == null)
        {
            if (_module.ServiceProvider == null)
                return;

            _chatService = _module.ServiceProvider.Resolve<ChatService>();
        }

        if (_shutdownTime < 0)
            return;
        
        float rt = Time.realtimeSinceStartup;

        if (rt >= _shutdownTime)
        {
            _ = _module.ShutdownAsync(ShutdownReason ?? DefaultShutdownReason, CancellationToken.None);
        }
        else if (_shutdownStep >= 0 && rt >= _shutdownTime - _shutdownSteps[_shutdownStep])
        {
            SendShutdownStep();
            --_shutdownStep;
        }
        else if (QueuedShutdownType == ShutdownMode.OnLayoutEnd && rt - _lastLayoutShutdownWarning > SecondsBetweenNextGameWarnings)
        {
            CheckTranslations();
            if (_shutdownTranslations != null)
                _chatService.Broadcast(_shutdownTranslations.ShutdownBroadcastAfterGameReminder, ShutdownReason ?? DefaultShutdownReason, TimeSpan.FromSeconds(Math.Round((_shutdownTime - rt) / 5)) * 5);
            _lastLayoutShutdownWarning = rt;
        }
    }

    private void CheckTranslations()
    {
        _shutdownTranslations ??= _module.ServiceProvider?.Resolve<TranslationInjection<ShutdownTranslations>>().Value;
    }

    private static void FixShutdownReason(ref string? shutdownReason)
    {
        if (shutdownReason != null && shutdownReason.EndsWith('.'))
            shutdownReason = shutdownReason[..^1];
    }

    private void SendShutdownStep()
    {
        CheckTranslations();
        if (_shutdownTranslations != null)
            _chatService.Broadcast(_shutdownTranslations.ShutdownBroadcastTimeReminder, TimeSpan.FromSeconds(_shutdownSteps[_shutdownStep]), ShutdownReason ?? DefaultShutdownReason);
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
                return "Time|" + ShutdownTime.ToString("O") + "|" + ShutdownReason;

            case ShutdownMode.OnLayoutEnd:
                if (ShutdownReason == null)
                    return "OnLayoutEnd|" + ShutdownTime.ToString("O");
                return "OnLayoutEnd|" + ShutdownTime.ToString("O") + "|" + ShutdownReason;

            case ShutdownMode.Now:
                if (ShutdownReason == null)
                    return "Now";
                return "Now|" + ShutdownReason;

            default:
                return "None";
        }
    }

    [RpcReceive]
    public void QueueShutdownInstant(string? shutdownReason = null)
    {
        FixShutdownReason(ref shutdownReason);
        CancelShutdown();
        _ = _module.ShutdownAsync(shutdownReason ?? DefaultShutdownReason);
    }

    private int GetPassedShutdownStep()
    {
        DateTime dt = DateTime.UtcNow;

        for (int i = _shutdownSteps.Length - 1; i >= 0; --i)
        {
            if (dt + TimeSpan.FromSeconds(_shutdownSteps[i]) > ShutdownTime)
                continue;

            return i;
        }

        return -1;
    }

    [RpcReceive]
    public void QueueShutdownInTime(TimeSpan time, string? shutdownReason = null)
    {
        DateTime dt = DateTime.UtcNow.Add(time);
        if (QueuedShutdownType == ShutdownMode.Time && ShutdownTime > dt)
            return;

        FixShutdownReason(ref shutdownReason);
        ShutdownTime = dt;
        ShutdownReason = shutdownReason;
        QueuedShutdownType = ShutdownMode.Time;
        _shutdownTime = (float)(Time.realtimeSinceStartup + time.TotalSeconds);
        _shutdownStep = GetPassedShutdownStep();
        UpdateShutdownState();

        CheckTranslations();
        if (_shutdownTranslations != null)
            _chatService.Broadcast(_shutdownTranslations.ShutdownBroadcastTime, time, shutdownReason ?? DefaultShutdownReason);
    }

    [RpcReceive]
    public void QueueShutdownAtLayoutEnd(string? shutdownReason = null)
    {
        if (QueuedShutdownType == ShutdownMode.OnLayoutEnd)
            return;

        FixShutdownReason(ref shutdownReason);
        QueuedShutdownType = ShutdownMode.OnLayoutEnd;
        ShutdownReason = shutdownReason ;
        ShutdownTime = DateTime.UtcNow.AddHours(1d);
        _shutdownTime = Time.realtimeSinceStartup + SecondsUntilFallbackShutdownAfterUpdate;
        _shutdownStep = GetPassedShutdownStep();
        UpdateShutdownState();

        CheckTranslations();
        if (_shutdownTranslations != null)
            _chatService.Broadcast(_shutdownTranslations.ShutdownBroadcastAfterGame, shutdownReason ?? DefaultShutdownReason, TimeSpan.FromHours(1d).Subtract(TimeSpan.FromSeconds(1d)));
        _lastLayoutShutdownWarning = Time.realtimeSinceStartup;
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
        _shutdownStep = -1;
        UpdateShutdownState();

        CheckTranslations();
        if (_shutdownTranslations != null)
            _chatService.Broadcast(_shutdownTranslations.ShutdownBroadcastCancelled);
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
        ShutdownReason = reason;
        ShutdownTime = default;
        _shutdownTime = -1;
        _shutdownStep = -1;
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