using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Configuration;

namespace Uncreated.Warfare.Sync;

public class SyncConfig<TData> : IConfiguration<TData>, ISyncObject where TData : JSONConfigData, new()
{
    private readonly object sync = new object();
    private bool _regReload = false;
    private readonly int syncId;
    public TData Data { get; } = new TData();
    public virtual bool DontReplicate => Warfare.Data.IsInitialSyncRegistering;
    public string? ReloadKey { get; }
    public FileInfo File { get; }
    string IConfiguration.Directory => File.FullName;
    public SyncConfig(string path, string? reloadKey = null)
    {
        if (reloadKey is not null)
        {
            ReloadKey = reloadKey;
            _regReload = ReloadCommand.RegisterConfigForReload(this);
        }
        if (Attribute.GetCustomAttribute(typeof(TData), typeof(SyncAttribute)) == null)
            throw new ArgumentException("To use SyncConfig, you must apply the Sync attribute to the config data class (" + typeof(TData).Name + ") with a unique ID.", nameof(TData));
        File = new FileInfo(path);
        ConfigSync.ConfigSyncInst? data = ConfigSync.GetClassData(typeof(TData));
        if (data is null || !ConfigSync.RegisterSingleton(Data))
            throw new ArgumentException("Unable to find the associated class data with the type provided (" + typeof(TData).Name + ").", nameof(TData));
        syncId = data.SyncId;

        Read();
    }
    public void DeregisterReload()
    {
        if (_regReload && ReloadKey is not null)
        {
            ReloadCommand.DeregisterConfigForReload(ReloadKey);
            _regReload = false;
        }
    }
    public void LoadDefaults()
    {
        Data.SetDefaults();
        Write();
    }
    public void Reload()
    {
        Read();
    }
    public void Save()
    {
        Write();
    }
    private void Write()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking("Sync Config: " + typeof(TData).Name + " (Write)");
#endif
        lock (sync)
        {
            using FileStream stream = new FileStream(File.FullName, FileMode.Create, FileAccess.Write, FileShare.Read);
            using Utf8JsonWriter writer = new Utf8JsonWriter(stream, JsonEx.writerOptions);
            ConfigSync.ConfigSyncInst? data = ConfigSync.GetClassData(syncId);
            if (data is null)
            {
                L.LogWarning("Unable to find ConfigSync data for " + typeof(TData).Name + " when writing.");
                return;
            }

            writer.WriteStartObject();
            foreach (KeyValuePair<int, ConfigSync.ConfigSyncInst.Property> property in data.SyncedMembers)
            {
                writer.WritePropertyName(property.Value.JsonName);
                object? val = property.Value.Getter(Data);
                if (val is null)
                    writer.WriteNullValue();
                else
                {
                    try
                    {
                        JsonSerializer.Serialize(writer, val, property.Value.PropertyInfo.PropertyType, JsonEx.serializerSettings);
                    }
                    catch (Exception ex)
                    {
                        L.LogError("Error serializing " + property.Value.JsonName + " (" + property.Value.PropertyInfo.DeclaringType.Name + "." + property.Value.PropertyInfo.Name + ").");
                        L.LogError(ex);
                        throw;
                    }
                }
            }
            writer.WriteEndObject();
            writer.Flush();
        }
    }
    private void Read()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking("Sync Config: " + typeof(TData).Name + " (Read)");
#endif
        TData? defaults = null;
        lock (sync)
        {
            if (!File.Exists)
                goto def;

            ConfigSync.ConfigSyncInst? data = ConfigSync.GetClassData(syncId);
            if (data is null)
            {
                L.LogWarning("Unable to find ConfigSync data for " + typeof(TData).Name + " when reading.");
                return;
            }

            using (FileStream stream = new FileStream(File.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (stream.Length > int.MaxValue)
                    throw new IOException("File too long for Json Reader: " + File.FullName + ".");
                byte[] bytes = new byte[stream.Length];
                stream.Read(bytes, 0, bytes.Length);
                Utf8JsonReader reader = new Utf8JsonReader(bytes, JsonEx.readerOptions);
                if (!reader.Read())
                    goto def;

                List<int> vals = new List<int>(data.SyncedMembers.Count);
                if (reader.TokenType == JsonTokenType.StartObject)
                {
                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonTokenType.PropertyName)
                        {
                            string prop = reader.GetString()!;
                            if (prop != null && reader.Read())
                            {
                                bool found = false;
                                foreach (KeyValuePair<int, ConfigSync.ConfigSyncInst.Property> propData in data.SyncedMembers)
                                {
                                    if (propData.Value.JsonName.Equals(prop, StringComparison.Ordinal))
                                    {
                                        found = true;
                                        object? value;
                                        if (reader.TokenType == JsonTokenType.Null)
                                            value = null;
                                        else
                                        {
                                            try
                                            {
                                                value = JsonSerializer.Deserialize(ref reader, propData.Value.PropertyInfo.PropertyType, JsonEx.serializerSettings);
                                            }
                                            catch (Exception ex)
                                            {
                                                L.LogError("Error deserializing " + propData.Value.JsonName + " (" + propData.Value.PropertyInfo.DeclaringType.Name + "." + propData.Value.PropertyInfo.Name + ").");
                                                L.LogError(ex);
                                                throw;
                                            }
                                        }
                                        ConfigSync.SetPropertySilent(data, propData.Value, value, false, applyToSave: !Warfare.Data.IsInitialSyncRegistering, pushAsNew: true);
                                        vals.Add(propData.Key);
                                        break;
                                    }
                                }
                                if (!found)
                                    L.LogWarning("Unknown property \"" + prop + "\" in config for " + typeof(TData).Name + ": " + File.FullName + ".");
                            }
                        }
                    }
                }
                else
                    throw new JsonException("Unexpected token in config file " + typeof(TData).Name + ": " + File.FullName + ".");
                foreach (KeyValuePair<int, ConfigSync.ConfigSyncInst.Property> propData in data.SyncedMembers)
                {
                    for (int i = 0; i < vals.Count; i++)
                    {
                        if (vals[i] == propData.Key)
                            goto c;
                    }
                    if (defaults is null)
                    {
                        defaults = new TData();
                        defaults.SetDefaults();
                    }
                    ConfigSync.SetPropertySilent(data, propData.Value, propData.Value.Getter(defaults), false, applyToSave: !Warfare.Data.IsInitialSyncRegistering, pushAsNew: true);
                c:;
                }
            }
        }
        if (defaults is not null)
        {
            Save();
            if (defaults is IDisposable disp)
                disp.Dispose();
        }
        return;
    def:
        LoadDefaults();
    }
}

public enum ConfigSyncId : ushort
{
    Invalid,
    GamemodeConfig = 1
}