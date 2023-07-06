using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Uncreated.Json;
using Uncreated.Warfare.Locations;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes.Insurgency;
public class CacheLocations
{
    private readonly List<CacheLocation> _locationsIntl;
    public string FileName { get; set; }
    public IReadOnlyList<CacheLocation> Locations { get; }
    public CacheLocations()
    {
        FileName = Path.Combine(Data.Paths.MapStorage, "insurgency_caches.json");
        _locationsIntl = new List<CacheLocation>(0);
        Locations = _locationsIntl.AsReadOnly();
    }
    /// <exception cref="NotSupportedException"/>
    public bool AddCacheLocation(CacheLocation location, bool save = true)
    {
        ThreadUtil.assertIsGameThread();

        if (_locationsIntl.Contains(location))
            return false;

        _locationsIntl.Add(location);

        if (save)
            Save();
        return true;
    }
    /// <exception cref="NotSupportedException"/>
    public bool RemoveCacheLocation(CacheLocation location, bool save = true)
    {
        ThreadUtil.assertIsGameThread();

        if (!_locationsIntl.Remove(location))
            return false;

        if (save)
            Save();
        return true;
    }
    /// <exception cref="NotSupportedException"/>
    public void Reload()
    {
        ThreadUtil.assertIsGameThread();

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
                Utf8JsonReader reader = new Utf8JsonReader(bytes, JsonEx.readerOptions);
                _locationsIntl.Clear();
                if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
                {
                    L.LogError("Failed to read cache locations.");
                    return;
                }
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    if (reader.TokenType != JsonTokenType.StartObject)
                        continue;
                    CacheLocation? cache = JsonSerializer.Deserialize<CacheLocation>(ref reader, JsonEx.serializerSettings);
                    if (cache != null)
                        _locationsIntl.Add(cache);
                }
            }
            catch (Exception ex)
            {
                L.LogError("Error reading cache locations.");
                L.LogError(ex);
                _locationsIntl.Clear();
            }
        }
    }
    /// <exception cref="NotSupportedException"/>
    public void Save()
    {
        ThreadUtil.assertIsGameThread();

        string? dir = Path.GetDirectoryName(FileName);
        if (dir != null)
            Directory.CreateDirectory(dir);

        try
        {
            using FileStream stream = new FileStream(FileName, FileMode.Create, FileAccess.Write, FileShare.Read);
            using Utf8JsonWriter writer = new Utf8JsonWriter(stream, JsonEx.writerOptions);
            JsonSerializer.Serialize(writer, _locationsIntl, JsonEx.serializerSettings);
            writer.Flush();
        }
        catch (Exception ex)
        {
            L.LogError("Error writing cache locations.");
            L.LogError(ex);
        }
    }
}

public class CacheLocation : IEquatable<CacheLocation>
{
    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; set; }

    [JsonPropertyName("position")]
    public Vector3 Position { get; set; }

    [JsonPropertyName("euler_angles")]
    public Vector3 Rotation { get; set; }

    [JsonPropertyName("placer")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public ulong Placer { get; set; }

    [JsonPropertyName("disabled")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsDisabled { get; set; }

    public Quaternion GetBarricadeAngle() => Quaternion.Euler(Rotation);
    public override bool Equals(object obj) => obj is CacheLocation cacheLocation && Equals(cacheLocation);
    public bool Equals(CacheLocation? location)
    {
        if (location is null)
            return this is null;
        return location.Position.AlmostEquals(Position) && location.Rotation.AlmostEquals(Rotation);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            // ReSharper disable NonReadonlyMemberInGetHashCode
            return (Position.GetHashCode() * 397) ^ Rotation.GetHashCode();
            // ReSharper restore NonReadonlyMemberInGetHashCode
        }
    }

    public static bool operator ==(CacheLocation? left, CacheLocation? right) => left is null ? right is null : left.Equals(right);
    public static bool operator !=(CacheLocation? left, CacheLocation? right) => !(left == right);

    public override string ToString() => Name ?? ("[" + new GridLocation(Position) + "] " + F.GetClosestLocationName(Position, true, true));
}