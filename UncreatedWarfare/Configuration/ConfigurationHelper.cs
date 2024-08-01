using DanielWillett.ReflectionTools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Uncreated.Warfare.Configuration;
public static class ConfigurationHelper
{
    private static readonly IFileProvider FileProvider = new PhysicalFileProvider(Environment.CurrentDirectory, ExclusionFilters.Sensitive);

    /// <summary>
    /// An empty configuration section.
    /// </summary>
    public static IConfigurationSection EmptySection { get; } = new EmptyConfigurationSection();

    /// <summary>
    /// Adds a new <see cref="FileConfigurationSource"/> at <paramref name="path"/> with an optional file source with the map name appended.
    /// </summary>
    /// <remarks>Example: Config.json and Config.Washington.json.</remarks>
    public static void AddSourceWithMapOverride(IConfigurationBuilder configBuilder, string path)
    {
        string ext = Path.GetExtension(path);

        // add default
        AddJsonOrYamlFile(configBuilder, path, optional: false, reloadOnChange: true);

        // add map
        string mapName = CleanMapNameForFileName(Provider.map);
        string rootPath = Path.Join(Path.GetDirectoryName(path.AsSpan()), Path.GetFileNameWithoutExtension(path.AsSpan()));

        AddJsonOrYamlFile(configBuilder, $"{rootPath}.{mapName}.{ext}", optional: true, reloadOnChange: true);
    }

    /// <summary>
    /// Watches for file updates on <paramref name="filePath"/>, and invokes <paramref name="onUpdated"/> when there is an update.
    /// </summary>
    /// <remarks><paramref name="filePath"/> must be in the Warfare folder.</remarks>
    [MustUseReturnValue]
    [System.Diagnostics.Contracts.Pure]
    public static IDisposable ListenForFileUpdate(string filePath, System.Action onUpdated)
    {
        return ChangeToken.OnChange(
            () => FileProvider.Watch(filePath),
            () =>
            {
                UniTask.Create(async () =>
                {
                    await UniTask.SwitchToMainThread();
                    onUpdated();
                });
            }
        );
    }

    /// <summary>
    /// Bind config data to a <see cref="IConfiguration"/> instance.
    /// </summary>
    [System.Diagnostics.Contracts.Pure]
    public static TConfigData ParseConfigData<TConfigData>(this IConfiguration config) where TConfigData : JSONConfigData, new()
    {
        TConfigData? data;
        try
        {
            data = config.Get<TConfigData>();
            if (data == null)
            {
                L.LogWarning($"Couldn't parse {Accessor.Formatter.Format(typeof(TConfigData))} file.");
            }
        }
        catch (Exception ex)
        {
            data = null;
            L.LogError(ex);
            L.LogError($"Errored while parsing {Accessor.Formatter.Format(typeof(TConfigData))} file.");
        }

        if (data != null)
            return data;
        
        data = new TConfigData();
        data.SetDefaults();
        return data;
    }

    private static void AddJsonOrYamlFile(IConfigurationBuilder configBuilder, string path, bool optional = false, bool reloadOnChange = false)
    {
        ReadOnlySpan<char> ext = Path.GetExtension(path.AsSpan());
        if (ext.Equals(".json", StringComparison.OrdinalIgnoreCase))
        {
            configBuilder.AddJsonFile(Path.GetFullPath(path), optional, reloadOnChange);
        }
        else if (ext.Equals(".yml", StringComparison.OrdinalIgnoreCase))
        {
            configBuilder.AddYamlFile(Path.GetFullPath(path), optional, reloadOnChange);
        }
        else
            throw new ArgumentException("Must provide a valid extension (.yml or .json).", nameof(path));
    }

    [return: NotNullIfNotNull(nameof(mapName))]
    private static string? CleanMapNameForFileName(string? mapName)
    {
        return mapName?
            .Replace("\0", string.Empty)
            .Replace(@"\", string.Empty)
            .Replace("/",  string.Empty)
            .Replace(":",  string.Empty)
            .Replace("*",  string.Empty)
            .Replace("?",  string.Empty)
            .Replace("|",  string.Empty)
            .Replace("<",  string.Empty)
            .Replace(' ', '_')
            .Replace(">",  string.Empty);
    }
    private class EmptyConfigurationSection : IConfigurationSection, IChangeToken, IDisposable
    {
        public string Key => string.Empty;
        public string Path => string.Empty;
        public bool HasChanged => false;
        public bool ActiveChangeCallbacks => false;
        public string? Value
        {
            get => null;
            set => throw new NotSupportedException();
        }
        public string? this[string key]
        {
            get => null;
            set => throw new NotSupportedException();
        }

        public IConfigurationSection GetSection(string key) => this;
        public IEnumerable<IConfigurationSection> GetChildren() => Array.Empty<IConfigurationSection>();
        public IChangeToken GetReloadToken() => this;
        public IDisposable RegisterChangeCallback(Action<object> callback, object state) => this;
        public void Dispose() { }
    }
}