using DanielWillett.ReflectionTools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using System;
using System.IO;
using Uncreated.Warfare.Maps;
using UnityEngine.SceneManagement;

namespace Uncreated.Warfare.Configuration;

/// <summary>
/// Allows deriving types to reference a configuration file.
/// </summary>
public abstract class BaseAlternateConfigurationFile : IConfiguration, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly MapScheduler? _mapScheduler;

    private IConfiguration _configuration;
    private IDisposable? _reloadToken;

    private readonly WarfareModule _module;

    private readonly string _fileName;
    private readonly bool _optional, _reloadable;

    /// <summary>
    /// Full path to the configuratin file.
    /// </summary>
    public string? FilePath { get; private set; }

    /// <summary>
    /// The underlying configuration object.
    /// </summary>
    public IConfiguration UnderlyingConfiguration { get; private set; }

    /// <summary>
    /// Invoked when this configuration file is updated.
    /// </summary>
    public event Action<IConfiguration>? OnChange;

    /// <summary>
    /// Create a new configuration file reference.
    /// </summary>
    /// <param name="mapSpecific">Will go in a "Maps/[map name]/" folder.</param>
    protected BaseAlternateConfigurationFile(IServiceProvider serviceProvider, string fileName, bool mapSpecific = false, bool optional = false, bool reload = true)
    {
        _serviceProvider = serviceProvider;
        _module = serviceProvider.GetRequiredService<WarfareModule>();
        _fileName = fileName;
        _optional = optional;
        _reloadable = reload;
        _mapScheduler = mapSpecific ? serviceProvider.GetService<MapScheduler>() : null;

        _configuration = ConfigurationHelper.EmptySection;
        UnderlyingConfiguration = _configuration;

        // flagData is the first thing to load when the scene is loaded
        if (_mapScheduler == null || LevelNavigation.flagData != null)
        {
            TryInit(mapSpecific);
        }
        else
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            _hasSceneLoaded = 1;
        }
    }

    private void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        if (arg0.buildIndex != Level.BUILD_INDEX_GAME)
            return;

        if (Interlocked.Exchange(ref _hasSceneLoaded, 0) == 1)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
        TryInit(true);
    }

    private bool TryInit(bool mapSpecific)
    {
        // for map-specific configs, this needs to happen after ServerWorkshopLoading executes (which is just before the scene loads)
        if (_mapScheduler is { HasSelectedMap: false })
        {
            return false;
        }

        lock (this)
        {
            if (FilePath != null)
                return true;

            string homeDir = _module.HomeDirectory;
            string path;
            if (mapSpecific)
            {
                string mapName = ConfigurationHelper.CleanFileName(Provider.map);
                path = Path.Combine(homeDir, "Maps", mapName);
            }
            else
            {
                path = homeDir;
            }

            string filePath = Path.Combine(path, _fileName);

            if (!_optional && !File.Exists(filePath))
            {
                throw new FileNotFoundException($"Missing required configuration file for {Accessor.ExceptionFormatter.Format(GetType())}: \"{filePath}\".");
            }

            ConfigurationBuilder builder = new ConfigurationBuilder();
            ConfigurationHelper.AddSourceWithMapOverride(builder, WarfareModule.Singleton.FileProvider, filePath, optional: _optional, reloadOnChange: _reloadable);
            _configuration = builder.Build();

            if (_reloadable)
            {
                _reloadToken = ChangeToken.OnChange(
                    _configuration.GetReloadToken,
                    () =>
                    {
                        UniTask.Create(async () =>
                        {
                            await UniTask.SwitchToMainThread();
                            WarfareModule.Singleton.GlobalLogger.LogInformation($"Configuration file reloaded: {Path.GetFileName(filePath)}");
                            HandleChange();
                            OnChange?.Invoke(this);
                        });
                    });
            }

            UnderlyingConfiguration = _configuration;
            FilePath = filePath;
        }

        return true;
    }

    /// <summary>
    /// Invoked by the base class when a change occurs.
    /// </summary>
    protected virtual void HandleChange() { }

    /// <inheritdoc />
    public string? this[string key]
    {
        get
        {
            CheckInit();
            return _configuration[key];
        }
        set
        {
            CheckInit();
            _configuration[key] = value;
        }
    }

    private void CheckInit()
    {
        if (FilePath != null)
            return;

        if (!TryInit(true))
            throw new InvalidOperationException("Not initialized yet. Wait until after the level is selected by the MapScheduler.");
    }

    /// <inheritdoc />
    public IConfigurationSection GetSection(string key)
    {
        CheckInit();
        return _configuration.GetSection(key);
    }

    /// <inheritdoc />
    public IEnumerable<IConfigurationSection> GetChildren()
    {
        CheckInit();
        return _configuration.GetChildren();
    }

    /// <inheritdoc />
    public IChangeToken GetReloadToken()
    {
        CheckInit();
        return _configuration.GetReloadToken();
    }

    /// <inheritdoc />
    void IDisposable.Dispose()
    {
        if (Interlocked.Exchange(ref _hasSceneLoaded, 0) == 1)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
        Interlocked.Exchange(ref _reloadToken, null)?.Dispose();
        IConfiguration oldConfig = Interlocked.Exchange(ref _configuration, ConfigurationHelper.EmptySection);
        if (oldConfig != ConfigurationHelper.EmptySection && oldConfig is IDisposable disp)
        {
            disp.Dispose();
        }
    }

    private int _hasSceneLoaded;
}
