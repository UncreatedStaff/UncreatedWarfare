﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;
using SDG.Framework.IO.Serialization;
using ISerializer = YamlDotNet.Serialization.ISerializer;
using System.Data;
using YamlDotNet.Core;
using Uncreated.Warfare.Configuration;

namespace Uncreated.Warfare.Database;
public class YamlDataStore<T> : IDisposable
{
    public delegate T DefaultData();

    private T? _data;
    private readonly ILogger _logger;
    private readonly bool _reloadOnFileChanged;
    private readonly FileInfo SourceFile;
    private readonly ISerializer _serializer;
    private readonly IDeserializer _deserializer;
    private readonly Func<T>? _loadDefault;
    private FileSystemWatcher _watcher;

    object _lock = new Object();

    public T Data => _data ?? throw new InvalidOperationException("YamlDataStore data has not yet been loaded from its source file, and no LoadDefault value has been defined.");
    public DateTime LastLoaded { get; private set; }
    public Action<YamlDataStore<T>>? OnFileReload { get; set; }

    public YamlDataStore(string filePath, ILogger logger, bool reloadOnFileChanged, Func<T>? loadDefault = null)
    {
        SourceFile = new FileInfo(filePath);

        _logger = logger;
        _reloadOnFileChanged = reloadOnFileChanged;
        if (loadDefault != null)
        {
            _data = loadDefault();
            _loadDefault = loadDefault;
        }

        _serializer = new SerializerBuilder()
            .DisableAliases()
            .WithTypeConverter(new AssetLinkYamlConverter()) // add more type converters as we go
            .Build();

        _deserializer = new DeserializerBuilder()
            .WithTypeConverter(new AssetLinkYamlConverter())
            .Build();

        _watcher = new FileSystemWatcher();

        _watcher.Filter = SourceFile.Name;
        _watcher.NotifyFilter = NotifyFilters.LastWrite;
        _watcher.Changed += OnChanged;

        LastLoaded = DateTime.MinValue;
    }
    private void CreateFileIfNotExists()
    {
        if (!File.Exists(SourceFile.FullName))
        {
            _logger.LogInformation($"YamlDataStore has not yet been loaded, so default values be loaded and saved to disk.");

            Directory.CreateDirectory(SourceFile.Directory.FullName);

            if (_data == null)
            {
                LoadDefault();
            }

            string yamlToWrite = _serializer.Serialize(_data);
            File.WriteAllText(SourceFile.FullName, yamlToWrite);
        }
    }
    private void LoadDefault()
    {
        if (_loadDefault != null)
            _data = _loadDefault();
    }
    
    public void Reload()
    {
        lock(_lock)
        {
            CreateFileIfNotExists();

            _watcher.Path = SourceFile.Directory.FullName;
            _watcher.EnableRaisingEvents = _reloadOnFileChanged;

            LoadIntl();
        }
    }
    private void LoadIntl()
    {
        string yamlFromFile = File.ReadAllText(SourceFile.FullName);

        try
        {
            _data = _deserializer.Deserialize<T>(yamlFromFile);

            if (_data == null)
            {
                LoadDefault();
                Save();
            }

            LastLoaded = File.GetLastWriteTime(SourceFile.FullName);

            _logger.LogDebug($"Loaded yaml data from file {SourceFile.FullName}.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Yaml data from file '{SourceFile.FullName}' could not be deserialized into an instance of {typeof(T)}. Data will not change. \nCause: {ex}");
        }
    }
    public void Save()
    {
        lock (_lock)
        {
            _watcher.EnableRaisingEvents = false;
            CreateFileIfNotExists();

            string yamlToWrite = _serializer.Serialize(_data);

            File.WriteAllText(SourceFile.FullName, yamlToWrite);

            LastLoaded = File.GetLastWriteTime(SourceFile.FullName);

            _watcher.Path = SourceFile.Directory.FullName;
            _watcher.EnableRaisingEvents = _reloadOnFileChanged;
        }

        _logger.LogDebug($"Saved yaml data to file {SourceFile.FullName}.");

    }
    private void OnChanged(object source, FileSystemEventArgs e)
    {
        UniTask.Create(async () =>
        {
            await UniTask.SwitchToMainThread();

            lock (_lock)
            {
                // FileSystemWatcher often fires events twice (some apps and text editors write to files in batches), so it's good to sanity check them.
                DateTime lastWriteTime = File.GetLastWriteTime(e.FullPath);
                if (lastWriteTime == LastLoaded)
                    return;

                LoadIntl();
            }

            LastLoaded = File.GetLastWriteTime(SourceFile.FullName);

            _logger.LogInformation($"File change detected. Yaml data has been reloaded from file '{e.Name}'");

            try
            {
                OnFileReload?.Invoke(this);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception was thrown while invoking FileReloaded action:\n{ex}");
            }
        });
    }

    public void Dispose()
    {
        _watcher.Dispose();
    }
}