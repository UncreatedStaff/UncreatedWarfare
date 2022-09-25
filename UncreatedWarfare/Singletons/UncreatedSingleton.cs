using System;
using System.Reflection;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Gamemodes.Flags;
using UnityEngine;

namespace Uncreated.Warfare.Singletons;

public interface IUncreatedSingleton
{
    bool IsLoaded { get; }
    void Load();
    void Unload();
}
public interface ILevelStartListener
{
    void OnLevelReady();
}
public interface IDeclareWinListener
{
    void OnWinnerDeclared(ulong winner);
}
public interface IGameStartListener
{
    void OnGameStarting(bool isOnLoad);
}
public interface IFlagCapturedListener
{
    void OnFlagCaptured(Flag flag, ulong newOwner, ulong oldOwner);
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
public interface IPlayerAsyncInitListener
{
    void OnAsyncInitComplete(UCPlayer player);
}
public interface IJoinedTeamListener
{
    void OnJoinTeam(UCPlayer player, ulong team);
}
public interface IPlayerInitListener
{
    void OnPlayerInit(UCPlayer player, bool wasAlreadyOnline);
}
public interface IPlayerDeathListener
{
    void OnPlayerDeath(PlayerDied e);
}

public interface IReloadableSingleton : IUncreatedSingleton
{
    string? ReloadKey { get; }
    void Reload();
}
public abstract class BaseSingleton : IUncreatedSingleton
{
    public bool IsLoaded => _isLoaded;
    protected bool _isLoaded;
    /// <exception cref="SingletonUnloadedException"/>
    internal void AssertLoadedIntl()
    {
        if (!_isLoaded)
            throw new SingletonUnloadedException(this.GetType());
    }
    public abstract void Load();
    public abstract void Unload();
    void IUncreatedSingleton.Load()
    {
        Load();
        _isLoaded = true;
    }
    void IUncreatedSingleton.Unload()
    {
        _isLoaded = false;
        Unload();
    }
}
public abstract class BaseSingletonComponent : MonoBehaviour, IUncreatedSingleton
{
    public bool IsLoaded => _isLoaded;
    protected bool _isLoaded;
    /// <exception cref="SingletonUnloadedException"/>
    internal void AssertLoadedIntl()
    {
        if (!_isLoaded)
            throw new SingletonUnloadedException(this.GetType());
    }
    public abstract void Load();
    public abstract void Unload();
    void IUncreatedSingleton.Load()
    {
        Load();
        _isLoaded = true;
    }
    void IUncreatedSingleton.Unload()
    {
        _isLoaded = false;
        Unload();
    }
}
public abstract class BaseReloadSingleton : BaseSingleton, IReloadableSingleton
{
    public string? ReloadKey => reloadKey;
    private readonly string? reloadKey;
    public BaseReloadSingleton(string? reloadKey)
    {
        this.reloadKey = reloadKey;
    }
    public abstract void Reload();
    void IReloadableSingleton.Reload()
    {
        _isLoaded = false;
        Reload();
        _isLoaded = true;
    }
}
public abstract class ListSingleton<TData> : JSONSaver<TData>, IReloadableSingleton where TData : class, new()
{
    public string? ReloadKey => reloadKey;
    private readonly string? reloadKey;
    public bool IsLoaded => _isLoaded;
    protected bool _isLoaded;
    /// <exception cref="SingletonUnloadedException"/>
    internal void AssertLoadedIntl()
    {
        if (!_isLoaded)
            throw new SingletonUnloadedException(this.GetType());
    }
    protected ListSingleton(string? reloadKey, string file) : base(file, false)
    {
        this.reloadKey = reloadKey;
    }
    protected ListSingleton(string file) : this(null, file) { }
    protected ListSingleton(string? reloadKey, string file, CustomSerializer? serializer, CustomDeserializer? deserializer) : base(file, serializer, deserializer, false)
    {
        this.reloadKey = reloadKey;
    }
    protected ListSingleton(string file, CustomSerializer? serializer, CustomDeserializer? deserializer) : this(null, file, serializer, deserializer) { }
    public virtual void Reload() { }
    public abstract void Load();
    public abstract void Unload();
    void IReloadableSingleton.Reload()
    {
        _isLoaded = false;
        Init();
        Reload();
        _isLoaded = true;
    }
    void IUncreatedSingleton.Load()
    {
        Init();
        Load();
        _isLoaded = true;
    }
    void IUncreatedSingleton.Unload()
    {
        _isLoaded = false;
        Unload();
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