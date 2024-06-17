using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Uncreated.Json;
using Uncreated.Warfare.Commands;

namespace Uncreated.Warfare.Configuration;

public class Config<TData> : IConfigurationHolder<TData> where TData : JSONConfigData, new()
{
    public delegate TData CustomDeserializer(ref Utf8JsonReader reader);
    public delegate void CustomSerializer(TData obj, Utf8JsonWriter writer);

    readonly string _dir;
    public Type Type = typeof(TData);
    private readonly CustomDeserializer? customDeserializer;
    private readonly bool useCustomDeserializer;
    private readonly CustomSerializer? customSerializer;
    private readonly bool useCustomSerializer;
    private readonly bool _regReload;
    private readonly string? reloadKey;
    public string Directory => _dir;
    public string? ReloadKey => reloadKey;
    public TData Data { get; private set; }
    public Config(string directory, string filename, string reloadKey)
        : this(directory, filename)
    {
        if (reloadKey is not null)
        {
            this.reloadKey = reloadKey;
            _regReload = ReloadCommand.RegisterConfigForReload(this);
        }
    }
    private readonly bool isFirst = true;
    public Config(string directory, string filename)
    {
        _dir = Path.Combine(directory, filename);
        customDeserializer = null;
        useCustomDeserializer = false;
        customSerializer = null;
        useCustomSerializer = false;
        if (!System.IO.Directory.Exists(directory))
        {
            try
            {
                System.IO.Directory.CreateDirectory(directory);
            }
            catch (Exception ex)
            {
                L.LogError("Unable to create data directory " + directory + ". Check permissions: " + ex.Message);
                return;
            }
        }

        if (!File.Exists(_dir))
            LoadDefaults();
        else
            Reload();
        isFirst = false;
    }
    public void DeregisterReload()
    {
        if (_regReload && reloadKey is not null)
            ReloadCommand.DeregisterConfigForReload(reloadKey);
    }
    public Config(string directory, string filename, CustomDeserializer deserializer, CustomSerializer serializer)
    {
        _dir = directory + filename;
        customDeserializer = deserializer;
        useCustomDeserializer = deserializer != null;
        customSerializer = serializer;
        useCustomSerializer = serializer != null;
        if (!System.IO.Directory.Exists(directory))
        {
            try
            {
                System.IO.Directory.CreateDirectory(directory);
            }
            catch (Exception ex)
            {
                L.LogError("Unable to create data directory " + directory + ". Check permissions: " + ex.Message);
                return;
            }
        }
        if (!File.Exists(_dir))
            LoadDefaults();
        else
            Reload();
    }
    public void Save()
    {
        if (!File.Exists(_dir))
            File.Create(_dir)?.Close();
        using (FileStream stream = new FileStream(_dir, FileMode.Truncate, FileAccess.Write, FileShare.None))
        {
            if (useCustomSerializer)
            {
                Utf8JsonWriter? writer = null;
                try
                {
                    writer = new Utf8JsonWriter(stream, JsonEx.writerOptions);
                    customSerializer!.Invoke(Data, writer);
                }
                catch (Exception ex)
                {
                    L.LogError($"Failed to run {Type.Name} custom serializer, running auto-serialzier...");
                    L.LogError(ex);
                    goto other;
                }
                finally
                {
                    writer?.Dispose();
                }
                return;
            }
        other:
            try
            {
                byte[] utf8 = System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(Data, JsonEx.serializerSettings));
                stream.Write(utf8, 0, utf8.Length);
            }
            catch (Exception ex)
            {
                L.LogError($"Failed to run {Type.Name} auto-serializer.");
                L.LogError(ex);
            }
        }
    }
    /// <summary>Not implemented</summary>
    protected virtual void OnReload() { }
    public void Reload()
    {
        if (!File.Exists(_dir))
        {
            LoadDefaults();
            if (!isFirst)
                OnReload();
        }
        else
        {
            using (FileStream stream = new FileStream(_dir, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read))
            {
                long len = stream.Length;
                if (len > int.MaxValue)
                {
                    L.LogError($"Config file for {Type.Name} config.");
                    return;
                }
                if (stream.Length == 0) goto loadDefaults;
                byte[] bytes = new byte[len];
                stream.Read(bytes, 0, (int)len);
                try
                {
                    Utf8JsonReader reader = new Utf8JsonReader(bytes, JsonEx.readerOptions);
                    if (useCustomDeserializer)
                    {
                        if (reader.Read() && reader.TokenType == JsonTokenType.StartObject)
                        {
                            Data = customDeserializer!.Invoke(ref reader);
                        }
                        if (Data is not null)
                        {
                            if (!isFirst)
                                OnReload();
                            return;
                        }
                    }

                    Data = JsonSerializer.Deserialize<TData>(ref reader, JsonEx.serializerSettings)!;
                    if (!isFirst)
                        OnReload();
                }
                catch (Exception ex)
                {
                    L.LogError($"Failed to run auto-deserializer for {Type.Name}.");
                    L.LogError(ex);
                }
            }

            return;
        loadDefaults:
            LoadDefaults();
            if (!isFirst)
                OnReload();
        }
    }
    public void TryUpgrade()
    {
        try
        {
            TData defaultConfig = new TData();
            defaultConfig.SetDefaults();
            FieldInfo[] fields = Type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            bool needsSaving = false;
            for (int i = 0; i < fields.Length; i++)
            {
                try
                {
                    if (fields[i].IsStatic ||  // if the field is static or it contains [JsonIgnore] in its attributes.
                        fields[i].CustomAttributes.Count(x => x.AttributeType == typeof(JsonIgnoreAttribute) ||
                        x.AttributeType == typeof(PreventUpgradeAttribute)) > 0) continue;
                    object currentvalue = fields[i].GetValue(Data);
                    object defaultvalue = fields[i].GetValue(defaultConfig);
                    if (currentvalue == defaultvalue) continue;
                    else if (currentvalue != fields[i].FieldType.getDefaultValue()) continue;
                    else
                    {
                        fields[i].SetValue(Data, defaultvalue);
                        needsSaving = true;
                    }
                }
                catch (Exception ex)
                {
                    L.LogError($"Error upgrading config for field {fields[i].FieldType.Name}::{fields[i].Name} in {Type.Name} config.");
                    L.LogError(ex);
                }
            }
            if (needsSaving)
            {
                Save();
                if (!isFirst)
                    OnReload();
            }
        }
        catch (Exception ex)
        {
            L.LogError("Error upgrading config: " + Type.Name);
            L.LogError(ex);
        }
    }
    public void LoadDefaults()
    {
        Data = new TData();
        Data.SetDefaults();
        using (FileStream stream = new FileStream(_dir, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            if (useCustomSerializer)
            {
                Utf8JsonWriter? writer = null;
                try
                {
                    writer = new Utf8JsonWriter(stream, JsonEx.writerOptions);
                    customSerializer!.Invoke(Data, writer);
                }
                catch (Exception ex)
                {
                    L.LogError("Failed to run custom serializer, running auto-serialzier...");
                    L.LogError(ex);
                    goto other;
                }
                finally
                {
                    writer?.Dispose();
                }
                return;
            }

        other:
            byte[] utf8 = System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(Data, JsonEx.serializerSettings));
            stream.Write(utf8, 0, utf8.Length);
        }
    }
}

public abstract class JSONConfigData
{
    public JSONConfigData() => SetDefaults();
    public abstract void SetDefaults();
}
public interface IConfigurationHolder
{
    string Directory { get; }
    string? ReloadKey { get; }
    void LoadDefaults();
    void Reload();
    void Save();
}

public interface IConfigurationHolder<TData> : IConfigurationHolder where TData : JSONConfigData, new()
{
    TData Data { get; }
}
[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
sealed class PreventUpgradeAttribute : Attribute
{
    public PreventUpgradeAttribute() { }
}
