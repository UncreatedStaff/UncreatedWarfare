using DanielWillett.ReflectionTools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.IO;

namespace Uncreated.Warfare.Configuration;

/// <summary>
/// Allows deriving types to reference a configuration file.
/// </summary>
public abstract class BaseAlternateConfigurationFile : IConfiguration, IDisposable
{
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Full path to the configuratin file.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// The underlying configuration object.
    /// </summary>
    public IConfiguration UnderlyingConfiguration { get; }

    /// <summary>
    /// Invoked when this configuration file is updated.
    /// </summary>
    public event Action<IConfiguration>? OnChange;

    /// <summary>
    /// Create a new configuration file reference.
    /// </summary>
    /// <param name="mapSpecific">Will go in a "Maps/[map name]/" folder.</param>
    protected BaseAlternateConfigurationFile(IServiceProvider serviceProvider, string fileName, bool mapSpecific = false)
    {
        WarfareModule module = serviceProvider.GetRequiredService<WarfareModule>();
        string homeDir = module.HomeDirectory;

        if (mapSpecific)
        {
            string mapName = ConfigurationHelper.CleanFileName(Provider.map);
            homeDir = Path.Combine(homeDir, "Maps", mapName);
        }

        FilePath = Path.Combine(homeDir, fileName);

        if (!File.Exists(FilePath))
        {
            throw new FileNotFoundException($"Missing required configuration file for {Accessor.ExceptionFormatter.Format(GetType())}: \"{FilePath}\".");
        }

        ConfigurationBuilder builder = new ConfigurationBuilder();
        ConfigurationHelper.AddSourceWithMapOverride(builder, module.FileProvider, FilePath);
        _configuration = builder.Build();

        _configuration.GetReloadToken().RegisterChangeCallback(_ =>
        {
            UniTask.Create(async () =>
            {
                await UniTask.SwitchToMainThread();
                HandleChange();
                OnChange?.Invoke(this);
            });
        }, null);

        UnderlyingConfiguration = _configuration;
    }

    /// <summary>
    /// Invoked by the base class when a change occurs.
    /// </summary>
    protected virtual void HandleChange() { }

    /// <inheritdoc />
    public string this[string key] { get => _configuration[key]; set => _configuration[key] = value; }

    /// <inheritdoc />
    public IConfigurationSection GetSection(string key) => _configuration.GetSection(key);

    /// <inheritdoc />
    public IEnumerable<IConfigurationSection> GetChildren() => _configuration.GetChildren();

    /// <inheritdoc />
    public IChangeToken GetReloadToken() => _configuration.GetReloadToken();

    /// <inheritdoc />
    void IDisposable.Dispose()
    {
        if (_configuration is IDisposable disp)
            disp.Dispose();
    }
}
