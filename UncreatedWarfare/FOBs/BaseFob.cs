using System;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.FOBs;

/// <summary>
/// Base class for all FOB types that use the standard registration behavior.
/// </summary>
public abstract class BaseFob : IDisposable
{
    private int _isRegistered = 1;

    public bool IsRegistered => _isRegistered != 0;

    public event Action? Deregistered;

    protected virtual void Dispose(bool disposing)
    {
        if (Interlocked.Exchange(ref _isRegistered, 0) == 0)
        {
            return;
        }

        if (GameThread.IsCurrent)
        {
            InvokeDeregistered();
        }
        else
        {
            UniTask.Create(async () =>
            {
                await UniTask.SwitchToMainThread();
                InvokeDeregistered();
            });
        }
    }

    private void InvokeDeregistered()
    {
        try
        {
            Deregistered?.Invoke();
        }
        catch (Exception ex)
        {
            WarfareModule.Singleton.GlobalLogger.LogError(ex, "Event handler threw an exception in IFob.Deregistered event.");
        }
    }

    public void Dispose()
    {
        Dispose(true);
    }
}