using Microsoft.Extensions.DependencyInjection;
using System;

namespace Uncreated.Warfare.Util.DependencyInjection;

/// <summary>
/// Passes a transient service and ensures it won't be added to the dispose list of the IoC container.
/// </summary>
public readonly struct DontDispose<TService>
{
    /// <summary>
    /// The actual service.
    /// </summary>
    public readonly TService Value;
    public DontDispose(IServiceProvider serviceProvider)
    {
        Value = ActivatorUtilities.CreateInstance<TService>(serviceProvider);
    }
    
    public static implicit operator TService(DontDispose<TService> container) => container.Value;

    public void TryDispose()
    {
        if (Value is IDisposable disposable)
            disposable.Dispose();
    }

    public ValueTask TryDisposeAsync()
    {
        switch (Value)
        {
            case IAsyncDisposable disposable:
                return disposable.DisposeAsync();

            case IDisposable disposable:
                disposable.Dispose();
                break;
        }

        return default;
    }
}