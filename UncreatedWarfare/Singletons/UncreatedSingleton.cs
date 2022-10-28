using System;
using System.Reflection;
using System.Threading.Tasks;
using Uncreated.SQL;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Items;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Gamemodes.Flags;
using UnityEngine;

namespace Uncreated.Warfare.Singletons;

public interface IUncreatedSingleton
{
    bool LoadAsynchrounously { get; }
    bool AwaitLoad { get; }
    bool IsLoaded { get; }
    bool IsLoading { get; }
    bool IsUnloading { get; }
    Task LoadAsync();
    Task UnloadAsync();
    void Load();
    void Unload();
}
public interface ILevelStartListener
{
    void OnLevelReady();
}
public interface ILevelStartListenerAsync
{
    Task OnLevelReady();
}
public interface IDeclareWinListener
{
    void OnWinnerDeclared(ulong winner);
}
public interface IDeclareWinListenerAsync
{
    Task OnWinnerDeclared(ulong winner);
}
public interface IGameStartListener
{
    void OnGameStarting(bool isOnLoad);
}
public interface IGameStartListenerAsync
{
    Task OnGameStarting(bool isOnLoad);
}
public interface IFlagCapturedListener
{
    void OnFlagCaptured(Flag flag, ulong newOwner, ulong oldOwner);
}
public interface ICraftingSettingsOverride
{
    void OnCraftRequested(CraftRequested e);
}

public interface IFlagNeutralizedListener
{
    void OnFlagNeutralized(Flag flag, ulong newOwner, ulong oldOwner);
}
public interface IPlayerDisconnectListener
{
    void OnPlayerDisconnecting(UCPlayer player);
}
public interface IPlayerConnectListener
{
    void OnPlayerConnecting(UCPlayer player);
}
public interface IPlayerConnectListenerAsync
{
    Task OnPlayerConnecting(UCPlayer player);
}
public interface IPlayerPostInitListener
{
    void OnPostPlayerInit(UCPlayer player);
}
public interface IPlayerPostInitListenerAsync
{
    Task OnPostPlayerInit(UCPlayer player);
}
public interface IJoinedTeamListener
{
    void OnJoinTeam(UCPlayer player, ulong team);
}
public interface IPlayerPreInitListener
{
    void OnPrePlayerInit(UCPlayer player, bool wasAlreadyOnline);
}
public interface IPlayerPreInitListenerAsync
{
    Task OnPrePlayerInit(UCPlayer player, bool wasAlreadyOnline);
}
public interface IPlayerDeathListener
{
    void OnPlayerDeath(PlayerDied e);
}

public interface IReloadableSingleton : IUncreatedSingleton
{
    string? ReloadKey { get; }
    void Reload();
    Task ReloadAsync();
}
public abstract class BaseAsyncSingleton : IUncreatedSingleton
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
    public abstract Task LoadAsync();
    public abstract Task UnloadAsync();
    async Task IUncreatedSingleton.LoadAsync()
    {
        _isLoading = true;
        await LoadAsync();
        _isLoading = false;
        _isLoaded = true;
    }
    async Task IUncreatedSingleton.UnloadAsync()
    {
        _isUnloading = true;
        _isLoaded = false;
        await UnloadAsync();
        _isUnloading = false;
    }
}
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
    public abstract Task LoadAsync();
    public abstract Task UnloadAsync();
    async Task IUncreatedSingleton.LoadAsync()
    {
        _isLoading = true;
        await LoadAsync();
        _isLoading = false;
        _isLoaded = true;
    }
    async Task IUncreatedSingleton.UnloadAsync()
    {
        _isUnloading = true;
        _isLoaded = false;
        await UnloadAsync();
        _isUnloading = false;
    }
}
public abstract class BaseAsyncReloadSingleton : BaseAsyncSingleton, IReloadableSingleton
{
    public string? ReloadKey { get; }
    protected BaseAsyncReloadSingleton(string? reloadKey)
    {
        this.ReloadKey = reloadKey;
    }
    public void Reload() => throw new NotImplementedException();
    public abstract Task ReloadAsync();
    async Task IReloadableSingleton.ReloadAsync()
    {
        _isLoaded = false;
        await ReloadAsync();
    }
}

public abstract class ListSqlSingleton<TItem> : ListSqlConfig<TItem>, IReloadableSingleton where TItem : class, IListItem, new()
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
    public async Task ReloadAsync()
    {
        if (_isLoading || _isUnloading)
            throw new InvalidOperationException("Already loading or unloading.");
        if (_isLoaded)
        {
            _isLoaded = false;
            _isUnloading = true;
            await UnloadAll().ConfigureAwait(false);
            _isUnloading = false;
        }
        _isLoading = true;
        await Init().ConfigureAwait(false);
        _isLoading = false;
        _isLoaded = true;
    }
    public async Task LoadAsync()
    {
        if (_isLoading || _isUnloading)
            throw new InvalidOperationException("Already loading or unloading.");
        _isLoading = true;
        await Init().ConfigureAwait(false);
        _isLoading = false;
        _isLoaded = true;
    }
    public async Task UnloadAsync()
    {
        if (_isLoading || _isUnloading)
            throw new InvalidOperationException("Already loading or unloading.");
        _isLoaded = false;
        _isUnloading = true;
        await UnloadAll().ConfigureAwait(false);
        _isUnloading = false;
    }
    public void Reload() => throw new NotImplementedException();
    public void Load() => throw new NotImplementedException();
    public void Unload() => throw new NotImplementedException();
}
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
            throw new SingletonUnloadedException(this.GetType());
    }
    public abstract void Load();
    public abstract void Unload();
    public Task LoadAsync() => throw new NotImplementedException();
    public Task UnloadAsync() => throw new NotImplementedException();
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
public abstract class BaseSingletonComponent : MonoBehaviour, IUncreatedSingleton
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
            throw new SingletonUnloadedException(this.GetType());
    }
    public abstract void Load();
    public abstract void Unload();
    public Task LoadAsync() => throw new NotImplementedException();
    public Task UnloadAsync() => throw new NotImplementedException();
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
public abstract class BaseReloadSingleton : BaseSingleton, IReloadableSingleton
{
    public string? ReloadKey { get; }
    protected BaseReloadSingleton(string? reloadKey)
    {
        this.ReloadKey = reloadKey;
    }
    public abstract void Reload();
    public Task ReloadAsync() => throw new NotImplementedException();
    void IReloadableSingleton.Reload()
    {
        _isLoaded = false;
        Reload();
        _isLoaded = true;
    }
}
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
            throw new SingletonUnloadedException(this.GetType());
    }
    protected ListSingleton(string? reloadKey, string file) : base(file, false)
    {
        this.ReloadKey = reloadKey;
    }
    protected ListSingleton(string file) : this(null, file) { }
    protected ListSingleton(string? reloadKey, string file, CustomSerializer? serializer, CustomDeserializer? deserializer) : base(file, serializer, deserializer, false)
    {
        this.ReloadKey = reloadKey;
    }
    protected ListSingleton(string file, CustomSerializer? serializer, CustomDeserializer? deserializer) : this(null, file, serializer, deserializer) { }
    public virtual void Reload() { }
    public abstract void Load();
    public abstract void Unload();
    public Task LoadAsync() => throw new NotImplementedException();
    public Task UnloadAsync() => throw new NotImplementedException();
    public Task ReloadAsync() => throw new NotImplementedException();
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
public abstract class ConfigSingleton<TConfig, TData> : BaseReloadSingleton where TConfig : Config<TData> where TData : JSONConfigData, new()
{
    private readonly string? _folder;
    private readonly string? _file;
    private TConfig _config;
    public TConfig ConfigurationFile => _config;
    public TData Config => _config.Data;
    protected ConfigSingleton(string reloadKey) : base(reloadKey) { }
    protected ConfigSingleton(string? reloadKey, string folder, string fileName) : base(reloadKey)
    {
        _folder = folder;
        _file = fileName;
    }
    protected ConfigSingleton(string folder, string fileName) : this(null, folder, fileName)
    {
        _folder = folder;
        _file = fileName;
    }
    private void CreateConfig()
    {
        ConstructorInfo[] ctors = typeof(TConfig).GetConstructors();
        bool hasEmpty = false;
        bool hasDefault = true;
        for (int i = 0; i < ctors.Length; ++i)
        {
            ParameterInfo[] ctorParams = ctors[i].GetParameters();
            if (ctorParams.Length == 0)
            {
                hasEmpty = true;
                break;
            }
            else if (ctorParams.Length == 2 && ctorParams[0].ParameterType == typeof(string) && ctorParams[1].ParameterType == typeof(string))
                hasDefault = true;
        }

        if (hasEmpty)
        {
            _config = (TConfig)Activator.CreateInstance(typeof(TConfig), Array.Empty<object>());
        }
        else if (hasDefault)
        {
            if (_folder is null || _file is null)
                throw new InvalidOperationException(typeof(TConfig).Name + " with " + typeof(TData).Name + " must have either an empty constructor or the ConfigSingleton constructor with folder and fileName should be used.");
            _config = (TConfig)Activator.CreateInstance(typeof(TConfig), new object[] { _folder, _file });
        }
        if (_config is null)
            throw new InvalidOperationException(typeof(TConfig).Name + " with " + typeof(TData).Name + " must have either an empty constructor or a " +
                "(string folder, string fileName) constuctor and the ConfigSingleton constructor with folder and fileName should be used.");
        if (ReloadKey is not null)
            ReloadCommand.DeregisterConfigForReload(ReloadKey);
    }
    public override void Load()
    {
        CreateConfig();
    }
    public override void Reload()
    {
        CreateConfig();
    }
    public override void Unload()
    {
        _config = null!;
    }
}

public static class SingletonEx
{
    /// <exception cref="SingletonUnloadedException"/>
    public static void AssertLoaded<T>() where T : class, IUncreatedSingleton
    {
        if (!Data.Singletons.IsLoaded<T>())
            throw new SingletonUnloadedException(typeof(T));
    }
    /// <exception cref="SingletonUnloadedException"/>
    public static void AssertLoaded<T>(bool check) where T : class, IUncreatedSingleton
    {
        if (!check)
            throw new SingletonUnloadedException(typeof(T));
    }
    /// <exception cref="SingletonUnloadedException"/>
    public static void AssertLoaded<T>(this T? singleton) where T : BaseSingleton
    {
        if (singleton is null)
            throw new SingletonUnloadedException(typeof(T));
        singleton.AssertLoadedIntl();
    }
    /// <exception cref="SingletonUnloadedException"/>
    public static void AssertLoaded<T, TData>(this T? singleton) where T : ListSingleton<TData> where TData : class, new()
    {
        if (singleton is null)
            throw new SingletonUnloadedException(typeof(T));
        singleton.AssertLoadedIntl();
    }
    public static bool IsLoaded<T, TData>(this T? singleton) where T : ListSingleton<TData> where TData : class, new()
    {
        if (singleton is null)
            return false;
        return singleton.IsLoaded;
    }
    public static bool IsLoaded<T>(this T? singleton) where T : BaseSingleton
    {
        if (singleton is null)
            return false;
        return singleton.IsLoaded;
    }
    public static bool IsLoaded2<T>(this T? singleton) where T : BaseSingletonComponent
    {
        if (singleton is null)
            return false;
        return singleton.IsLoaded;
    }
    /// <exception cref="SingletonUnloadedException"/>
    public static T AssertAndGet<T>() where T : class, IUncreatedSingleton
    {
        T? singleton = Data.Singletons.GetSingleton<T>();
        if (singleton is null)
            throw new SingletonUnloadedException(typeof(T));
        return singleton;
    }
}