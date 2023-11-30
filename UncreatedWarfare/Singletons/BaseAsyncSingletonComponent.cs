using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Uncreated.Warfare.Singletons;

public abstract class BaseAsyncSingletonComponent : MonoBehaviour, IUncreatedSingleton
{
    protected bool _isLoaded;
    protected bool _isLoading;
    protected bool _isUnloading;
    public bool IsLoading => _isLoading;
    public bool IsLoaded => _isLoaded;
    public bool IsUnloading => _isUnloading;
    public bool LoadAsynchrounously => true;
    public abstract bool AwaitLoad { get; }
    public void Load() => throw new NotImplementedException();
    public void Unload() => throw new NotImplementedException();
    public abstract Task LoadAsync(CancellationToken token);
    public abstract Task UnloadAsync(CancellationToken token);
    async Task IUncreatedSingleton.LoadAsync(CancellationToken token)
    {
        _isLoading = true;
        await LoadAsync(token);
        _isLoading = false;
        _isLoaded = true;
    }
    async Task IUncreatedSingleton.UnloadAsync(CancellationToken token)
    {
        _isUnloading = true;
        _isLoaded = false;
        await UnloadAsync(token);
        _isUnloading = false;
    }
    /// <exception cref="SingletonUnloadedException"/>
    internal void AssertLoadedIntl()
    {
        if (!_isLoaded)
            throw new SingletonUnloadedException(GetType());
    }
}