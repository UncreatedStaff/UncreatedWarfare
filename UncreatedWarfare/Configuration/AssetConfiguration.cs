using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.IO;

namespace Uncreated.Warfare.Configuration;

/// <summary>
/// Home for storing asset GUIDs, etc.
/// </summary>
/// <remarks>Use <see cref="AssetLink.GetAssetLink(IConfiguration,string)"/> or <see cref="ConfigurationBinder.GetValue{T}(IConfiguration,string)"/> with <see cref="IAssetLink{TAsset}"/> to get assets.</remarks>
public class AssetConfiguration : IConfiguration, IDisposable
{
    private readonly IConfiguration _configuration;
    public string FilePath { get; }
    public IConfiguration UnderlyingConfiguration { get; }

    public event Action<IConfiguration>? OnChange; 
    public AssetConfiguration(WarfareModule module)
    {
        FilePath = Path.Combine(module.HomeDirectory, "Assets.yml");

        ConfigurationBuilder builder = new ConfigurationBuilder();
        ConfigurationHelper.AddSourceWithMapOverride(builder, FilePath);
        _configuration = builder.Build();

        _configuration.GetReloadToken().RegisterChangeCallback(_ =>
        {
            UniTask.Create(async () =>
            {
                await UniTask.SwitchToMainThread();
                OnChange?.Invoke(this);
            });
        }, null);

        UnderlyingConfiguration = _configuration;
    }

    public string this[string key] { get => _configuration[key]; set => _configuration[key] = value; }
    public IConfigurationSection GetSection(string key) => _configuration.GetSection(key);
    public IEnumerable<IConfigurationSection> GetChildren() => _configuration.GetChildren();
    public IChangeToken GetReloadToken() => _configuration.GetReloadToken();
    public void Dispose()
    {
        if (_configuration is IDisposable disp)
            disp.Dispose();
    }
}