using Cysharp.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.Primitives;
using SDG.Unturned;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Uncreated.Warfare.Configuration;
public static class ConfigurationHelper
{
    private static readonly IFileProvider FileProvider = new PhysicalFileProvider(Environment.CurrentDirectory, ExclusionFilters.Sensitive);

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
}
