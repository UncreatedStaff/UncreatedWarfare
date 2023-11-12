using System;
using System.Threading;
using System.Threading.Tasks;

namespace Uncreated.Warfare.Singletons;
public abstract class BaseSingleton : IUncreatedSingleton
{
    protected bool _isLoaded;
    protected bool _isLoading;
    protected bool _isUnloading;
    public bool IsLoading => _isLoading;
    public bool IsLoaded => _isLoaded;
    public bool IsUnloading => _isUnloading;
    public bool AwaitLoad => false;
    public bool LoadAsynchrounously => false;

    /// <exception cref="SingletonUnloadedException"/>
    internal void AssertLoadedIntl()
    {
        if (!_isLoaded)
            throw new SingletonUnloadedException(GetType());
    }
    public abstract void Load();
    public abstract void Unload();
    public Task LoadAsync(CancellationToken token) => throw new NotImplementedException();
    public Task UnloadAsync(CancellationToken token) => throw new NotImplementedException();
    void IUncreatedSingleton.Load()
    {
        _isLoading = true;
        Load();
        _isLoading = false;
        _isLoaded = true;
    }
    void IUncreatedSingleton.Unload()
    {
        _isUnloading = true;
        _isLoaded = false;
        Unload();
        _isUnloading = false;
    }
}