using System;
using System.Threading;
using System.Threading.Tasks;

namespace Uncreated.Warfare.Singletons;
public abstract class BaseAsyncReloadSingleton : BaseAsyncSingleton, IReloadableSingleton
{
    public string? ReloadKey { get; }
    protected BaseAsyncReloadSingleton(string? reloadKey)
    {
        ReloadKey = reloadKey;
    }
    public void Reload() => throw new NotImplementedException();
    public abstract Task ReloadAsync(CancellationToken token);
    async Task IReloadableSingleton.ReloadAsync(CancellationToken token)
    {
        _isLoaded = false;
        await ReloadAsync(token);
    }
}
