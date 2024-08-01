using System;

namespace Uncreated.Warfare.Singletons;
public abstract class ListSqlSingleton<TItem> : ListSqlConfig<TItem>, IReloadableSingleton where TItem : class, IListItem
{
    private volatile bool _isLoading;
    private volatile bool _isUnloading;
    private volatile bool _isLoaded;
    public abstract bool AwaitLoad { get; }
    public bool LoadAsynchrounously => true;
    public bool IsLoaded => _isLoaded;
    public bool IsLoading => _isLoading;
    public bool IsUnloading => _isUnloading;
    string IReloadableSingleton.ReloadKey => ReloadKey!;
    protected ListSqlSingleton(Schema[] schemas) : base(schemas) { }
    protected ListSqlSingleton(string reloadKey, Schema[] schemas) : base(reloadKey, schemas) { }
    public async Task ReloadAsync(CancellationToken token)
    {
        Task task;
        if (_isLoading || _isUnloading)
            throw new InvalidOperationException("Already loading or unloading.");
        if (_isLoaded)
        {
            _isLoaded = false;
            _isUnloading = true;
            task = PreUnload(token);
            if (!task.IsCompleted)
                await task;
            await UnloadAll(token).ConfigureAwait(false);
            task = PostUnload(token);
            if (!task.IsCompleted)
                await task;
            _isUnloading = false;
        }
        _isLoading = true;
        task = PreLoad(token);
        if (!task.IsCompleted)
            await task;
        await Init(token).ConfigureAwait(false);
        task = PostLoad(token);
        if (!task.IsCompleted)
            await task;
        task = PostReload(token);
        if (!task.IsCompleted)
            await task;
        _isLoading = false;
        _isLoaded = true;
    }
    public async Task LoadAsync(CancellationToken token)
    {
        if (_isLoading || _isUnloading)
            throw new InvalidOperationException("Already loading or unloading.");
        _isLoading = true;
        Task task = PreLoad(token);
        if (!task.IsCompleted)
            await task;
        await Init(token).ConfigureAwait(false);
        task = PostLoad(token);
        if (!task.IsCompleted)
            await task;
        _isLoading = false;
        _isLoaded = true;
    }
    public async Task UnloadAsync(CancellationToken token)
    {
        if (_isLoading || _isUnloading)
            throw new InvalidOperationException("Already loading or unloading.");
        _isLoaded = false;
        _isUnloading = true;
        Task task = PreUnload(token);
        if (!task.IsCompleted)
            await task;
        await UnloadAll(token).ConfigureAwait(false);
        task = PostUnload(token);
        if (!task.IsCompleted)
            await task;
        _isUnloading = false;
    }
    public void Reload() => throw new NotImplementedException();
    public void Load() => throw new NotImplementedException();
    public void Unload() => throw new NotImplementedException();
    /// <remarks>No base.</remarks>
    public virtual Task PreLoad(CancellationToken token) => Task.CompletedTask;
    /// <remarks>No base.</remarks>
    public virtual Task PostLoad(CancellationToken token) => Task.CompletedTask;
    /// <remarks>No base.</remarks>
    public virtual Task PreUnload(CancellationToken token) => Task.CompletedTask;
    /// <remarks>No base.</remarks>
    public virtual Task PostUnload(CancellationToken token) => Task.CompletedTask;
    /// <remarks>No base.</remarks>
    public virtual Task PostReload(CancellationToken token) => Task.CompletedTask;
    /// <exception cref="SingletonUnloadedException"/>
    internal void AssertLoadedIntl()
    {
        if (!_isLoaded)
            throw new SingletonUnloadedException(GetType());
    }
}