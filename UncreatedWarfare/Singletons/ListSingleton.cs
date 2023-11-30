using System;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Warfare.Configuration;

namespace Uncreated.Warfare.Singletons;

public abstract class ListSingleton<TData> : JSONSaver<TData>, IReloadableSingleton where TData : class, new()
{
    protected bool _isLoaded;
    protected bool _isLoading;
    protected bool _isUnloading;
    public bool IsLoading => _isLoading;
    public bool IsLoaded => _isLoaded;
    public bool IsUnloading => _isUnloading;
    public string? ReloadKey { get; }
    public bool AwaitLoad => false;
    public bool LoadAsynchrounously => false;
    /// <exception cref="SingletonUnloadedException"/>
    internal void AssertLoadedIntl()
    {
        if (!_isLoaded)
            throw new SingletonUnloadedException(GetType());
    }
    protected ListSingleton(string? reloadKey, string file) : base(file, false)
    {
        ReloadKey = reloadKey;
    }
    protected ListSingleton(string file) : this(null, file) { }
    protected ListSingleton(string? reloadKey, string file, CustomSerializer? serializer, CustomDeserializer? deserializer) : base(file, serializer, deserializer, false)
    {
        ReloadKey = reloadKey;
    }
    protected ListSingleton(string file, CustomSerializer? serializer, CustomDeserializer? deserializer) : this(null, file, serializer, deserializer) { }
    public virtual void Reload() { }
    public abstract void Load();
    public abstract void Unload();
    public Task LoadAsync(CancellationToken token) => throw new NotImplementedException();
    public Task UnloadAsync(CancellationToken token) => throw new NotImplementedException();
    public Task ReloadAsync(CancellationToken token) => throw new NotImplementedException();
    void IReloadableSingleton.Reload()
    {
        _isLoading = true;
        _isLoaded = false;
        Init();
        _isUnloading = true;
        Reload();
        _isLoaded = true;
        _isLoading = false;
        _isUnloading = false;
    }
    void IUncreatedSingleton.Load()
    {
        _isLoading = true;
        Init();
        Load();
        _isLoaded = true;
        _isLoading = false;
    }
    void IUncreatedSingleton.Unload()
    {
        _isUnloading = true;
        _isLoaded = false;
        Unload();
        _isUnloading = false;
    }
}