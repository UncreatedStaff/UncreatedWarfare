using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using Uncreated.Warfare.Configuration;

namespace Uncreated.Warfare.Fobs;

/// <summary>
/// Home for storing FOB and buildable data.
/// </summary>
public class FobConfiguration : IConfiguration, IDisposable
{
    private readonly IConfiguration _configuration;
    public string FilePath { get; }
    public IConfiguration UnderlyingConfiguration { get; }

    public event Action<IConfiguration>? OnChange; 
    public FobConfiguration(WarfareModule module)
    {
        FilePath = Path.Combine(module.HomeDirectory, "Buildables.yml");

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