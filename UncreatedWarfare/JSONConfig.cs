using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Uncreated.Warfare;

namespace Uncreated
{
    public class Config<TData> : IConfiguration where TData : ConfigData, new()
    {
        readonly string _dir;
        public TData data { get; private set; }
        public Type Type = typeof(TData);
        public string Directory => _dir;

        private readonly CustomDeserializer customDeserializer;
        private readonly bool useCustomDeserializer;
        private readonly CustomSerializer customSerializer;
        private readonly bool useCustomSerializer;

        public delegate TData CustomDeserializer(ref Utf8JsonReader reader);
        public delegate void CustomSerializer(TData obj, Utf8JsonWriter writer);
        public Config(string directory, string filename)
        {
            F.CheckDir(directory, out bool success);
            if (!success)
            {
                L.LogError("Failed to create directory for config of " + Type.Name);
            }
            else
            {
                this._dir = directory + filename;
                if (!File.Exists(this._dir))
                    LoadDefaults();
                else
                    Reload();
            }
            customDeserializer = null;
            useCustomDeserializer = false;
        }
        public Config(string directory, string filename, CustomDeserializer deserializer, CustomSerializer serializer)
        {
            F.CheckDir(directory, out bool success);
            if (!success)
            {
                L.LogError("Failed to create directory for config of " + Type.Name);
            }
            else
            {
                this._dir = directory + filename;
                customDeserializer = deserializer;
                useCustomDeserializer = deserializer != null;
                customSerializer = serializer;
                useCustomSerializer = serializer != null;
                if (!File.Exists(this._dir))
                    LoadDefaults();
                else
                    Reload();
            }
        }
        public void Save()
        {
            if (!File.Exists(_dir))
                File.Create(_dir)?.Close();
            using (FileStream stream = new FileStream(_dir, FileMode.Truncate, FileAccess.Write, FileShare.None))
            {
                if (useCustomSerializer)
                {
                    Utf8JsonWriter writer = null;
                    try
                    {
                        writer = new Utf8JsonWriter(stream, JsonEx.writerOptions);
                        writer.WriteStartObject();
                        customSerializer.Invoke(data, writer);
                        writer.WriteEndObject();
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
                byte[] utf8 = System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(data, JsonEx.serializerSettings));
                stream.Write(utf8, 0, utf8.Length);
            }
        }
        public void Reload()
        {
            using (FileStream stream = new FileStream(_dir, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read))
            {
                long len = stream.Length;
                if (len > int.MaxValue)
                {
                    L.LogError($"Config file for {Type.Name} config.");
                    return;
                }
                byte[] bytes = new byte[len];
                stream.Read(bytes, 0, (int)len);
                try
                {
                    Utf8JsonReader reader = new Utf8JsonReader(bytes, JsonEx.readerOptions);
                    if (useCustomDeserializer)
                    {
                        if (reader.Read() && reader.TokenType == JsonTokenType.StartObject)
                        {
                            data = customDeserializer.Invoke(ref reader);
                        }
                        if (data != null) return;
                    }

                    data = JsonSerializer.Deserialize<TData>(ref reader, JsonEx.serializerSettings);
                }
                catch (JsonException ex)
                {
                    L.LogError("Failed to run serializer");
                    L.LogError(ex);
                }
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
                        object currentvalue = fields[i].GetValue(data);
                        object defaultvalue = fields[i].GetValue(defaultConfig);
                        if (currentvalue == defaultvalue) continue;
                        else if (currentvalue != fields[i].FieldType.getDefaultValue()) continue;
                        else
                        {
                            fields[i].SetValue(data, defaultvalue);
                            needsSaving = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        L.LogError($"Error upgrading config for field {fields[i].FieldType.Name}::{fields[i].Name} in {Type.Name} config.");
                        L.LogError(ex);
                    }
                }
                if (needsSaving) Save();
            }
            catch (Exception ex)
            {
                L.LogError("Error upgrading config: " + Type.Name);
                L.LogError(ex);
            }
        }
        public void LoadDefaults()
        {
            data = new TData();
            data.SetDefaults();
            using (FileStream stream = new FileStream(_dir, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
            {
                if (useCustomSerializer)
                {
                    Utf8JsonWriter writer = null;
                    try
                    {
                        writer = new Utf8JsonWriter(stream, JsonEx.writerOptions);
                        writer.WriteStartObject();
                        customSerializer.Invoke(data, writer);
                        writer.WriteEndObject();
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
                byte[] utf8 = System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(data, JsonEx.serializerSettings));
                stream.Write(utf8, 0, utf8.Length);
            }
        }
    }

    public abstract class ConfigData
    {
        public ConfigData() => SetDefaults();
        public abstract void SetDefaults();
    }
    public interface IConfiguration
    {
        string Directory { get; }
        void LoadDefaults();
        void Reload();
        void Save();
    }
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    sealed class PreventUpgradeAttribute : Attribute
    {
        public PreventUpgradeAttribute() { }
    }
}
