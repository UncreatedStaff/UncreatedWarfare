﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Layouts.Insurgency;

public class CacheLocationStore : ILayoutHostedService
{
    private readonly ILogger<CacheLocationStore> _logger;

    private readonly List<CacheLocation> _locationsIntl;

    public string? FileName { get; private set; }
    public IReadOnlyList<CacheLocation> Locations { get; private set; }

    public CacheLocationStore(ILogger<CacheLocationStore> logger)
    {
        _logger = logger;
        _locationsIntl = new List<CacheLocation>(0);
        Locations = _locationsIntl.AsReadOnly();
    }

    async UniTask ILayoutHostedService.StartAsync(CancellationToken token)
    {
        await UniTask.SwitchToMainThread(token);

        FileName = Path.Combine(
            UnturnedPaths.RootDirectory.FullName,
            ServerSavedata.directoryName,
            Provider.serverID,
            "Level",
            Level.info.name,
            "Insurgency Caches.json"
        );
    }

    UniTask ILayoutHostedService.StopAsync(CancellationToken token)
    {
        return UniTask.CompletedTask;
    }

    /// <summary>
    /// Add a new location for caches to spawn in Insurgency.
    /// </summary>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    public bool AddCacheLocation(CacheLocation location, bool save = true)
    {
        GameThread.AssertCurrent();

        if (_locationsIntl.Contains(location))
            return false;

        _locationsIntl.Add(location);

        if (save)
            Save();
        return true;
    }

    /// <summary>
    /// Remove a location for caches to spawn in Insurgency.
    /// </summary>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    public bool RemoveCacheLocation(CacheLocation location, bool save = true)
    {
        GameThread.AssertCurrent();

        if (!_locationsIntl.Remove(location))
            return false;

        if (save)
            Save();
        return true;
    }

    /// <summary>
    /// Re-read the caches list from file, or write an empty file if needed.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    /// <exception cref="InvalidOperationException">Not loaded.</exception>
    public void Reload()
    {
        GameThread.AssertCurrent();

        if (FileName == null)
        {
            throw new InvalidOperationException("FileName not set up.");
        }

        if (!File.Exists(FileName))
        {
            _locationsIntl.Clear();
            using FileStream stream = new FileStream(FileName, FileMode.Create, FileAccess.Write, FileShare.Write);
            byte[] bytes = "[]"u8.ToArray();
            stream.Write(bytes, 0, bytes.Length);
        }
        else
        {
            try
            {
                using FileStream stream = new FileStream(FileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                int len = (int)Math.Min(int.MaxValue, stream.Length);
                byte[] bytes = new byte[len];
                _ = stream.Read(bytes, 0, len);
                Utf8JsonReader reader = new Utf8JsonReader(bytes, ConfigurationSettings.JsonReaderOptions);
                _locationsIntl.Clear();
                if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
                {
                    _logger.LogError("Failed to read cache locations. Invalid JSON.");
                    return;
                }

                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    if (reader.TokenType != JsonTokenType.StartObject)
                        continue;
                    CacheLocation? cache = JsonSerializer.Deserialize<CacheLocation>(ref reader, ConfigurationSettings.JsonSerializerSettings);
                    if (cache != null)
                        _locationsIntl.Add(cache);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading cache locations.");
                _locationsIntl.Clear();
            }
        }
    }

    /// <exception cref="GameThreadException"/>
    /// <exception cref="InvalidOperationException">Not loaded.</exception>
    public void Save()
    {
        GameThread.AssertCurrent();

        if (FileName == null)
        {
            throw new InvalidOperationException("FileName not set up.");
        }

        string? dir = Path.GetDirectoryName(FileName);
        if (dir != null)
            Directory.CreateDirectory(dir);

        try
        {
            using FileStream stream = new FileStream(FileName, FileMode.Create, FileAccess.Write, FileShare.Read);
            using Utf8JsonWriter writer = new Utf8JsonWriter(stream, ConfigurationSettings.JsonWriterOptions);
            JsonSerializer.Serialize(writer, _locationsIntl, ConfigurationSettings.JsonSerializerSettings);
            writer.Flush();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing cache locations.");
        }
    }
}