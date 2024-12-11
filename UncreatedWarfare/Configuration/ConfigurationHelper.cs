using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Uncreated.Warfare.Configuration;
public static class ConfigurationHelper
{

    private static readonly HashSet<string> InvalidFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",                                                 // 1, 2, 3 superscripts
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "COM\u00B9", "COM\u00B2", "COM\u00B3",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9", "LPT\u00B9", "LPT\u00B2", "LPT\u00B3",
        ".", ".."
    };

    private static bool IsInvalidFileName(string name) => InvalidFileNames.Contains(name) || name.EndsWith(' ') || name.EndsWith('.');
    private static bool IsInvalidFileNameChar(char c) => c <= 31 || c is '/' or '\\' or '<' or '>' or ':' or '"' or '|' or '?' or '*';

    /// <summary>
    /// An empty configuration section.
    /// </summary>
    public static IConfigurationSection EmptySection { get; } = new EmptyConfigurationSection();

    /// <summary>
    /// Adds a new <see cref="FileConfigurationSource"/> at <paramref name="path"/> with an optional file source with the map name appended.
    /// </summary>
    /// <remarks>Example: Config.json and Config.Washington.json.</remarks>
    public static void AddSourceWithMapOverride(IConfigurationBuilder configBuilder, IFileProvider fileProvider, string path, bool optional = false)
    {
        MakeRelativePath(fileProvider, ref path);

        string ext = Path.GetExtension(path);

        // add default
        AddJsonOrYamlFile(configBuilder, fileProvider, path, optional: optional, reloadOnChange: true);

        // add map
        string mapName = CleanFileName(Provider.map);
        string rootPath = Path.Join(Path.GetDirectoryName(path.AsSpan()), Path.GetFileNameWithoutExtension(path.AsSpan()));

        string mapPath = $"{rootPath}.{mapName}{ext}";
        AddJsonOrYamlFile(configBuilder, fileProvider, mapPath, optional: true, reloadOnChange: true);
    }

    /// <summary>
    /// Watches for file updates on <paramref name="filePath"/>, and invokes <paramref name="onUpdated"/> when there is an update.
    /// </summary>
    /// <remarks><paramref name="filePath"/> must be in the Warfare folder.</remarks>
    [MustUseReturnValue]
    [Pure]
    public static IDisposable ListenForFileUpdate(IFileProvider fileProvider, string filePath, Action onUpdated)
    {
        MakeRelativePath(fileProvider, ref filePath);
        
        return ChangeToken.OnChange(
            () => fileProvider.Watch(filePath),
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
    /// Add a file source as either a JSON or YAML file, depending on the file extension.
    /// </summary>
    public static void AddJsonOrYamlFile(IConfigurationBuilder configBuilder, IFileProvider fileProvider, string path, bool optional = false, bool reloadOnChange = false)
    {
        MakeRelativePath(fileProvider, ref path);

        ReadOnlySpan<char> ext = Path.GetExtension(path.AsSpan());
        if (ext.Equals(".json", StringComparison.OrdinalIgnoreCase))
        {
            configBuilder.AddJsonFile(fileProvider, path, optional, reloadOnChange);
        }
        else if (ext.Equals(".yml", StringComparison.OrdinalIgnoreCase))
        {
            configBuilder.AddYamlFile(fileProvider, path, optional, reloadOnChange);
        }
        else
            throw new ArgumentException("Must provide a valid extension (.yml or .json).", nameof(path));
    }

    private static void MakeRelativePath(IFileProvider fileProvider, ref string path)
    {
        if (fileProvider is PhysicalFileProvider physicalFileProvider && Path.IsPathRooted(path))
        {
            path = Path.GetRelativePath(physicalFileProvider.Root, path);
        }
    }

    /// <summary>
    /// Fixes a file name (without its extension) to remove invalid characters.
    /// </summary>
    [return: NotNullIfNotNull(nameof(mapName))]
    public static string? CleanFileName(string? mapName)
    {
        if (mapName == null)
            return null;
        
        if (IsInvalidFileName(mapName))
        {
            if (mapName[^1] is ' ' or '.')
            {
                return mapName.Length == 1 ? "_" : mapName[..^1];
            }

            return "_" + mapName;
        }

        bool anyChanges = false;
        Span<char> newName = stackalloc char[mapName.Length];
        int index = 0;
        for (int i = 0; i < mapName.Length; ++i)
        {
            char c = mapName[i];
            if (IsInvalidFileNameChar(c))
            {
                anyChanges = true;
                continue;
            }

            newName[index] = c;
            ++index;
        }

        if (!anyChanges)
            return mapName;

        return index == 0 ? "_" : new string(newName[..index]);
    }

    /// <summary>
    /// Checks to see if <paramref name="longerPath"/> is a child folder or file of the directory <paramref name="shorterPath"/>.
    /// </summary>
    [Pure]
    public static bool IsChildOf(string? shorterPath, string longerPath, bool includeSubDirectories = true)
    {
        if (string.IsNullOrEmpty(shorterPath))
            return true;
        if (string.IsNullOrEmpty(longerPath))
            return false;
        DirectoryInfo parent = new DirectoryInfo(shorterPath);
        DirectoryInfo child = new DirectoryInfo(longerPath);
        return IsChildOf(parent, child, includeSubDirectories);
    }

    /// <summary>
    /// Checks to see if <paramref name="longerPath"/> is a child folder or file of the directory <paramref name="shorterPath"/>.
    /// </summary>
    [Pure]
    public static bool IsChildOf(DirectoryInfo shorterPath, DirectoryInfo longerPath, bool includeSubDirectories = true)
    {
        string shortFullname = shorterPath.FullName;
        if (!includeSubDirectories)
            return longerPath.Parent != null && longerPath.Parent.FullName.Equals(shortFullname, StringComparison.Ordinal);
        while (longerPath.Parent != null)
        {
            if (longerPath.Parent.FullName.Equals(shortFullname, StringComparison.Ordinal))
                return true;
            longerPath = longerPath.Parent;
        }

        return false;
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
        public IDisposable RegisterChangeCallback(Action<object> callback, object? state) => this;
        public void Dispose() { }
    }
}