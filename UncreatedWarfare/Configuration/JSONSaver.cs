using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using Uncreated.Warfare.Singletons;

namespace Uncreated.Warfare.Configuration;

public abstract class JSONSaver<T> : List<T> where T : class, new()
{
    protected const string EMPTY_LIST = "[]";
    protected string file;
    protected string directory;
    public readonly Type Type = typeof(T);
    private readonly FieldInfo[] fields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance);
    private readonly SemaphoreSlim _threadLocker = new SemaphoreSlim(1, 5);
    private readonly CustomSerializer? _serializer;
    private readonly bool useSerializer;
    private readonly CustomDeserializer? _deserializer;
    private readonly bool useDeserializer;
    private bool isInited = false;
    public delegate void CustomSerializer(T obj, Utf8JsonWriter writer);
    public delegate T CustomDeserializer(ref Utf8JsonReader reader);

    protected JSONSaver(string directory, bool loadNow = true)
    {
        this.directory = Path.GetDirectoryName(directory) ?? string.Empty;
        file = directory;

        useSerializer = false;
        useDeserializer = false;
        if (loadNow)
            Init();
    }

    protected JSONSaver(string directory, CustomSerializer? serializer, CustomDeserializer? deserializer, bool loadNow = true)
    {
        this.directory = Path.GetDirectoryName(directory) ?? string.Empty;
        file = directory;

        _serializer = serializer;
        useSerializer = serializer is not null;
        _deserializer = deserializer;
        useDeserializer = deserializer is not null;
        if (loadNow)
            Init();
    }
    protected void Init()
    {
        CreateFileIfNotExists(LoadDefaults());
        Read();
        isInited = true;
        TryUpgrade();
    }
    protected virtual void OnRead() { }
    protected abstract string LoadDefaults();
    /// <exception cref="SingletonLoadException"/>
    protected void CreateFileIfNotExists(string text = "[]")
    {
        F.CheckDir(directory, out _, true);
        if (!File.Exists(file))
        {
            using StreamWriter creator = File.CreateText(file);
            creator.WriteLine(text);
        }
    }
    protected T AddObjectToSave(T item, bool save = true)
    {
        if (!isInited) Init();
        if (item == null) throw new ArgumentNullException(nameof(item));
        Add(item);
        if (save) Save();
        return item;
    }
    protected void RemoveWhere(Predicate<T> match, bool save = true)
    {
        if (!isInited) Init();
        if (match == default) return;
        RemoveAll(match);
        if (save) Save();
    }
    protected void RemoveAllObjectsFromSave(bool save = true)
    {
        if (!isInited) Init();
        Clear();
        if (save) Save();
    }
    //private static readonly JsonSerializer serializer = new JsonSerializer() { Formatting = Formatting.Indented, Culture = Data.Locale };

    public void Save()
    {
        if (!isInited) Init();
        _threadLocker.Wait();
        if (useSerializer)
        {
            try
            {
                if (!File.Exists(file))
                    File.Create(file)?.Close();

                using (FileStream rs = new FileStream(file, FileMode.Truncate, FileAccess.Write, FileShare.None))
                {
                    Utf8JsonWriter writer = new Utf8JsonWriter(rs, JsonEx.writerOptions);
                    writer.WriteStartArray();
                    for (int i = 0; i < Count; i++)
                    {
                        writer.WriteStartObject();
                        _serializer!.Invoke(this[i], writer);
                        writer.WriteEndObject();
                    }

                    writer.WriteEndArray();
                    writer.Dispose();
                    rs.Close();
                    rs.Dispose();
                }
                _threadLocker.Release();
                return;
            }
            catch (Exception ex)
            {
                L.LogError("Failed to run custom serializer for " + typeof(T).Name);
                L.LogError(ex);
            }
        }
        try
        {
            using (StreamWriter file = File.CreateText(this.file))
            {
                file.Write(JsonSerializer.Serialize(this as List<T>, JsonEx.serializerSettings));
            }
        }
        catch (Exception ex)
        {
            L.LogError("Failed to run automatic serializer for " + typeof(T).Name);
            L.LogError(ex);
        }
        _threadLocker.Release();
    }
    public void Read()
    {
        _threadLocker.Wait();
        if (!File.Exists(file))
            CreateFileIfNotExists(LoadDefaults());
        if (useDeserializer)
        {
            FileStream? rs = null;
            try
            {
                using (rs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    long len = rs.Length;
                    if (len > int.MaxValue)
                    {
                        L.LogError("File " + file + " is too large.");
                        return;
                    }
                    byte[] buffer = new byte[len];
                    rs.Read(buffer, 0, (int)len);
                    Utf8JsonReader reader = new Utf8JsonReader(buffer.AsSpan(), JsonEx.readerOptions);
                    if (reader.Read() && reader.TokenType == JsonTokenType.StartArray)
                    {
                        Clear();
                        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                        {
                            if (reader.TokenType == JsonTokenType.StartObject)
                            {
                                T next = _deserializer!.Invoke(ref reader);
                                Add(next);
                                while (reader.TokenType != JsonTokenType.EndObject && reader.Read()) ;
                            }
                        }
                    }
                    rs.Close();
                    rs.Dispose();
                }
                _threadLocker.Release();
                OnRead();
                return;
            }
            catch (Exception e)
            {
                L.LogError("Failed to run custom deserializer for " + Type.Name);
                L.LogError(e);
                if (rs != null)
                {
                    rs.Close();
                    rs.Dispose();
                }
            }
        }
        bool clsd = false;
        StreamReader? r = null;
        try
        {
            r = File.OpenText(file);
            string json = r.ReadToEnd();
            r.Close();
            r.Dispose();
            clsd = true;
            _threadLocker.Release();
            T[]? vals = JsonSerializer.Deserialize<T[]>(json, JsonEx.serializerSettings);
            if (vals != null)
            {
                Clear();
                AddRange(vals);
                OnRead();
            }
        }
        catch (Exception ex)
        {
            if (r != null && !clsd)
            {
                r.Close();
                r.Dispose();
            }

            L.LogError("Failed to auto-deserialize " + Type.Name);
            L.LogError(ex);
            _threadLocker.Release();
        }
    }
    protected List<T> GetObjectsWhereAsList(Func<T, bool> predicate)
    {
        if (!isInited) Init();
        return this.Where(predicate).ToList();
    }

    protected IEnumerable<T> GetObjectsWhere(Func<T, bool> predicate)
    {
        if (!isInited) Init();
        return this.Where(predicate);
    }

    protected T? GetObject(Func<T, bool> predicate)
    {
        if (!isInited) Init();
        return this.FirstOrDefault(predicate);
    }

    protected bool ObjectExists(Func<T, bool> match, [NotNullWhen(true)] out T? item)
    {
        if (!isInited) Init();
        item = GetObject(match);
        return item != null;
    }
    public void UpdateObjectsWhere(Func<T, bool> selector, Action<T> operation, bool save = true)
    {
        IEnumerator<T> results = this.Where(selector).GetEnumerator();
        while (results.MoveNext())
            operation.Invoke(results.Current);
        results.Dispose();
        if (save) Save();
    }
    public bool IsPropertyValid<TEnum>(object name, out TEnum property) where TEnum : struct, Enum
    {
        if (Enum.TryParse<TEnum>(name.ToString(), out TEnum p))
        {
            property = p;
            return true;
        }
        property = p;
        return false;
    }
    public void TryUpgrade()
    {
        try
        {
            bool needsSaving = false;
            T defaultConfig = new T();
            for (int t = 0; t < Count; t++)
            {
                T th = this[t];
                if (th == null)
                {
                    th = new T();
                    needsSaving = true;
                    continue;
                }
                FieldInfo[] fields = Type.GetFields(BindingFlags.Public | BindingFlags.Instance);
                for (int i = 0; i < fields.Length; i++)
                {
                    if (fields[i].IsStatic ||  // if the field is static or it contains [JsonIgnore] in its attributes.
                        fields[i].CustomAttributes.Count(x => x.AttributeType == typeof(JsonIgnoreAttribute)) > 0) continue;
                    object currentvalue = fields[i].GetValue(th);
                    object defaultvalue = fields[i].GetValue(defaultConfig);
                    if (currentvalue == defaultvalue) continue;
                    else if (currentvalue != fields[i].FieldType.getDefaultValue()) continue;
                    else
                    {
                        fields[i].SetValue(th, defaultvalue);
                        needsSaving = true;
                    }
                }
            }
            if (needsSaving) Save();
        }
        catch (Exception ex)
        {
            L.LogError("Error upgrading in JsonSaver:");
            L.LogError(ex);
        }
    }
}