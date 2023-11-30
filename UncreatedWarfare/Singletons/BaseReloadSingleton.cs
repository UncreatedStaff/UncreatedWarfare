using System;
using System.Threading;
using System.Threading.Tasks;

namespace Uncreated.Warfare.Singletons;
public abstract class BaseReloadSingleton : BaseSingleton, IReloadableSingleton
{
    public string? ReloadKey { get; }
    protected BaseReloadSingleton(string? reloadKey)
    {
        ReloadKey = reloadKey;
    }
    public abstract void Reload();
    public Task ReloadAsync(CancellationToken token) => throw new NotImplementedException();
    void IReloadableSingleton.Reload()
    {
        _isLoaded = false;
        Reload();
        _isLoaded = true;
    }
}