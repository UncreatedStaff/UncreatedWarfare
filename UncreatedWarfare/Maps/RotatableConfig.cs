using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Uncreated.Encoding;
using Uncreated.Framework;
using Uncreated.Json;

namespace Uncreated.Warfare.Maps;
[JsonConverter(typeof(RotatableConfigConverterFactory))]
public class RotatableConfig<T> : IReadWrite, INotifyValueUpdate
{
    private const byte VERSION = 1;
    private const string DEFAULT_VALUE_MAP_NAME = "_";
    private static readonly Type type = typeof(T);
    private static readonly List<MapValue> _mapValueCache = new List<MapValue>(8);
    private static readonly bool isNullableClass = !type.IsValueType;
    private static readonly bool isNullableStruct = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
    public IEnumerable<T?> Values => _vals.Select(x => x._val);
    private MapValue[] _vals;
    private bool _isNull;
    private bool _isDefaulted;
    private T _val;
    private int _current = -1;

    public event Action<object>? OnUpdate;
    public T Value
    {
        get
        {
            if (_isNull)
                throw new NullReferenceException("Value is null.");
            return _val;
        }
        set => SetCurrentMapValue(value);
    }
    public bool HasValue => !_isNull;
    public bool SelectedIsDefault => _isDefaulted;
    public void Read(ByteReader reader)
    {
        reader.ReadUInt8(); // version
        int len = reader.ReadUInt8();
        MapValue[] vals = new MapValue[len];
        for (int i = 0; i < len; ++i)
        {
            ref MapValue v = ref vals[i];
            int index = -1;
            if (!reader.ReadBool())
            {
                string map = reader.ReadShortString();
                lock (_mapValueCache)
                {
                    index = RotatableConfigConverterFactory._maps.Count;
                    for (int j = 0; j < RotatableConfigConverterFactory._maps.Count; ++j)
                    {
                        if (RotatableConfigConverterFactory._maps[j].Equals(map, StringComparison.OrdinalIgnoreCase))
                        {
                            index = j;
                            break;
                        }
                    }
                    if (index == RotatableConfigConverterFactory._maps.Count)
                        RotatableConfigConverterFactory._maps.Add(map);
                }
            }
            if (reader.ReadBool())
            {
                T val = ByteReader.ReaderHelper<T>.Reader!(reader);
                v = new MapValue(index, val, false);
            }
            else
            {
                v = new MapValue(index, default!, true);
            }
        }

        InitMain(-1, vals);
    }
    public void Write(ByteWriter writer)
    {
        writer.Write(VERSION);
        writer.Write((byte)_vals.Length);
        for (int i = 0; i < _vals.Length; ++i)
        {
            ref MapValue v = ref _vals[i];
            if (v._isDefault)
                writer.Write(true);
            else
            {
                writer.Write(false);
                writer.WriteShort(RotatableConfigConverterFactory._maps[v._mapInd]);
            }
            if (v._isNull)
                writer.Write(false);
            else
            {
                writer.Write(true);
                ByteWriter.WriterHelper<T>.Writer(writer, v._val);
            }
        }
    }
    public RotatableConfig()
    {
        _vals = Array.Empty<MapValue>();
        _isNull = true;
        _val = default!;
        _isDefaulted = true;
    }
    public RotatableConfig(T value)
    {
        _vals = new MapValue[1] { new MapValue(value, value == null) };
        _isDefaulted = true;
        _current = 0;
        _val = value;
        _isNull = value == null;
    }
    public RotatableConfig(T @default, RotatableDefaults<T> overrides)
    {
        if (overrides.Count > byte.MaxValue - 1)
            throw new ArgumentException("You may not have more than 254 values and a default!", nameof(overrides));
        if (overrides is null || overrides.Count == 0)
        {
            _vals = new MapValue[1] { new MapValue(@default, @default == null) };
            _isDefaulted = true;
            _val = @default;
            _isNull = @default == null;
            _current = 0;
            return;
        }
        lock (_mapValueCache)
        {
            _mapValueCache.Add(new MapValue(@default, @default == null));
            for (int j = 0; j < overrides.Count; j++)
            {
                KeyValuePair<string, T> value = overrides[j];
                string map = value.Key;
                if (map.Equals(DEFAULT_VALUE_MAP_NAME, StringComparison.Ordinal)) continue;
                int index = RotatableConfigConverterFactory._maps.Count;
                for (int i = 0; i < RotatableConfigConverterFactory._maps.Count; ++i)
                {
                    if (RotatableConfigConverterFactory._maps[i].Equals(map, StringComparison.OrdinalIgnoreCase))
                    {
                        index = i;
                        break;
                    }
                }
                if (index == RotatableConfigConverterFactory._maps.Count)
                    RotatableConfigConverterFactory._maps.Add(map);
                _mapValueCache.Add(new MapValue(index, value.Value, value.Value == null));
            }
            _vals = _mapValueCache.ToArray();
            _mapValueCache.Clear();
        }

        _current = RotatableConfigConverterFactory.CurrentMap;
        if (_current == -1)
        {
            _isDefaulted = true;

            for (int i = 0; i < _vals.Length; ++i)
            {
                ref MapValue v = ref _vals[i];
                if (v._isDefault)
                {
                    _current = i;
                    _isDefaulted = true;
                    _val = v._val;
                    _isNull = _val == null;
                    return;
                }
            }
        }
        int def = -1;
        for (int i = 0; i < _vals.Length; ++i)
        {
            ref MapValue v = ref _vals[i];
            if (v._isDefault)
                def = i;
            else if (v._mapInd == _current)
            {
                _current = i;
                _isNull = v._val == null;
                _val = v._val;
                return;
            }
        }
        _current = def;
        if (def != -1)
        {
            ref MapValue v = ref _vals[def];
            _val = v._val;
            _isDefaulted = true;
            _isNull = _val == null;
        }
        else
        {
            _val = default!;
            _isNull = true;
            _isDefaulted = true;
        }
    }
    private RotatableConfig(int current, RotatableConfig<T>.MapValue[] vals)
    {
        if (vals.Length > byte.MaxValue)
            throw new ArgumentException("You may not have more than 255 values!", nameof(vals));
        InitMain(current, vals);
    }
    private void InitMain(int current, RotatableConfig<T>.MapValue[] vals)
    {
        _vals = vals;
        if (current == -1)
        {
            for (int i = 0; i < _vals.Length; ++i)
            {
                ref MapValue v = ref _vals[i];
                if (v._isDefault)
                {
                    _isNull = v._isNull;
                    _val = v._val;
                    _isDefaulted = true;
                    _current = i;
                    return;
                }
            }
            _val = default!;
            _isNull = true;
            _isDefaulted = true;
        }
        else
        {
            ref MapValue v = ref _vals[current];
            _val = v._val;
            _isNull = v._isNull;
            _isDefaulted = v._isDefault;
            _current = current;

            // fall back to default value in case of null (is this even wanted?)
            if (_isNull && !_isDefaulted)
            {
                for (int i = 0; i < _vals.Length; ++i)
                {
                    v = ref _vals[i];
                    if (v._isDefault)
                    {
                        _isNull = v._isNull;
                        _val = v._val;
                        _current = i;
                        _isDefaulted = true;
                        break;
                    }
                }
            }
        }
    }
    internal void Write(Utf8JsonWriter writer)
    {
        if (_isNull && _vals.Length == 0)
        {
            writer.WriteNullValue();
            return;
        }
        if (_vals.Length == 1)
        {
            ref MapValue def = ref _vals[0];
            if (def._isDefault)
            {
                if (def._isNull)
                    writer.WriteNullValue();
                else
                    JsonSerializer.Serialize(writer, def._val, typeof(T), JsonEx.serializerSettings);
                return;
            }
        }

        writer.WriteStartObject();
        for (int i = 0; i < _vals.Length; ++i)
        {
            ref MapValue val = ref _vals[i];
            string mName = val._isDefault ? DEFAULT_VALUE_MAP_NAME : RotatableConfigConverterFactory._maps[val._mapInd];
            writer.WritePropertyName(mName);
            if (val._isNull)
                writer.WriteNullValue();
            else
                JsonSerializer.Serialize(writer, val._val, typeof(T), JsonEx.serializerSettings);
        }

        writer.WriteEndObject();
    }
    internal static RotatableConfig<T> Read(ref Utf8JsonReader reader)
    {
        JsonTokenType token = reader.TokenType;
        switch (token)
        {
            case JsonTokenType.Null:
                return new RotatableConfig<T>();
            case JsonTokenType.StartObject:
                JsonDocument doc = JsonDocument.ParseValue(ref reader);
                JsonElement root = doc.RootElement;
                if (root.TryGetProperty(DEFAULT_VALUE_MAP_NAME, out _))
                {
                    MapValue[] values;
                    int targetIndex;
                    lock (_mapValueCache)
                    {
                        foreach (JsonProperty prop in root.EnumerateObject())
                        {
                            string map = prop.Name;
                            T obj;
                            bool isNull = false;
                            if (prop.Value.ValueKind != JsonValueKind.Null)
                                obj = prop.Value.Deserialize<T>(JsonEx.serializerSettings)!;
                            else
                            {
                                isNull = true;
                                obj = default!;
                            }
                            if (map.Equals(DEFAULT_VALUE_MAP_NAME, StringComparison.Ordinal))
                            {
                                _mapValueCache.Insert(0, new MapValue(obj, isNull));
                                continue;
                            }
                            int index = RotatableConfigConverterFactory._maps.Count;
                            for (int i = 0; i < RotatableConfigConverterFactory._maps.Count; ++i)
                            {
                                if (RotatableConfigConverterFactory._maps[i].Equals(map, StringComparison.OrdinalIgnoreCase))
                                {
                                    index = i;
                                    break;
                                }
                            }
                            if (index == RotatableConfigConverterFactory._maps.Count)
                                RotatableConfigConverterFactory._maps.Add(map);
                            _mapValueCache.Add(new MapValue(index, obj, isNull));
                        }

                        targetIndex = RotatableConfigConverterFactory.CurrentMap;
                        values = _mapValueCache.ToArray();
                        _mapValueCache.Clear();
                    }

                    if (targetIndex == -1)
                        return new RotatableConfig<T>(targetIndex, values);
                    int def = -1;
                    for (int i = 0; i < values.Length; ++i)
                    {
                        ref MapValue v = ref values[i];
                        if (v._isDefault)
                            def = i;
                        else if (v._mapInd == targetIndex)
                        {
                            return new RotatableConfig<T>(i, values);
                        }
                    }
                    return new RotatableConfig<T>(def, values);
                }
                else
                {
                    T obj = doc.Deserialize<T>(JsonEx.serializerSettings)!;
                    if (obj == null)
                        return new RotatableConfig<T>();
                    else
                        return new RotatableConfig<T>(0, new MapValue[] { new MapValue(obj, false) });
                }
            default:
                T val = (T)JsonSerializer.Deserialize(ref reader, typeof(T), JsonEx.serializerSettings)!;
                if (val == null)
                    return new RotatableConfig<T>();
                else
                    return new RotatableConfig<T>(0, new MapValue[] { new MapValue(val, false) });

        }
    }

    public static implicit operator T(RotatableConfig<T>? config)
    {
        if (config is null || config._isNull)
        {
            if (isNullableClass) return default!;
            if (isNullableStruct) return (T)Activator.CreateInstance(type);

            return default!;
        }
        return config.Value;
    }

    public static implicit operator RotatableConfig<T>(T def)
    {
        return new RotatableConfig<T>(def);
    }
    public static bool operator ==(RotatableConfig<T>? left, RotatableConfig<T>? right) => left is null ? right is null : left.Equals(right);
    public static bool operator !=(RotatableConfig<T>? left, RotatableConfig<T>? right) => !(left == right);
    public override bool Equals(object obj) => obj is RotatableConfig<T> t && Equals(t);
    public bool Equals(RotatableConfig<T>? other)
    {
        if (other is null)
            return this is null;
        if (this is null)
            return false;
        if (_vals.Length != other._vals.Length)
            return false;
        for (int i = 0; i < _vals.Length; ++i)
        {
            ref MapValue v1 = ref _vals[i];
            bool found = false;
            for (int j = 0; j < _vals.Length; ++j)
            {
                ref MapValue v2 = ref other._vals[j];
                if (v1._isNull == v2._isNull && v1._isDefault == v2._isDefault && v1._mapInd == v2._mapInd)
                {
                    if (v1._isNull || v1._val == null && v2._val == null || v1._val != null && v1._val.Equals(v2._val))
                        found = true;
                    else continue;
                    break;
                }
            }
            if (!found)
                return false;
        }
        return true;
    }
    public override int GetHashCode()
    {
        return _isNull ? 0 : this.Value!.GetHashCode();
    }
    public override string ToString()
    {
        string mname;
        if (_isDefaulted || _current == -1)
        {
            mname = "DEFAULT";
        }
        else
        {
            ref MapValue v = ref _vals[_current];
            mname = RotatableConfigConverterFactory._maps[v._mapInd];
        }

        return type.Name + " config, using setting for \"" + mname + "\" map: \"" + (_isNull ? "null" : _val!.ToString()) + "\"";
    }
    public void SetCurrentMapValue(T val)
    {
        if (val == null)
        {
            SetCurrentMapValueNull();
            return;
        }

        if (val.Equals(_val))
            return;
        if (_current != -1)
        {
            ref MapValue v = ref _vals[_current];
            v = new MapValue(v._isDefault ? -1 : v._mapInd, val, false);
            _isNull = false;
            SetValueIntl(val, true);
        }
        else
        {
            SetValueIntl(val, true);
        }
    }
    public void SetCurrentMapValueNull()
    {
        if (_isNull) return;
        if (_current != -1)
        {
            ref MapValue v = ref _vals[_current];
            v = new MapValue(v._isDefault ? -1 : v._mapInd, default!, true);
            _isNull = true;
            SetValueIntl(default!, true);
        }
        else
        {
            _isNull = true;
            SetValueIntl(default!, true);
        }
    }
    public void SetMapValue(T val, string map)
    {
        if (val == null)
        {
            SetMapValueNull(map);
            return;
        }
        int mapind = GetOrAddMap(map);
        if (mapind == RotatableConfigConverterFactory.CurrentMap)
        {
            SetCurrentMapValue(val);
            return;
        }

        for (int i = 0; i < _vals.Length; ++i)
        {
            ref MapValue v = ref _vals[i];
            if (v._mapInd == mapind)
            {
                if (val.Equals(v._val))
                    return;
                v = new MapValue(mapind, val, false);
                OnUpdate?.Invoke(this);
                return;
            }
        }
        MapValue[] old = _vals;
        _vals = new MapValue[_vals.Length + 1];
        Array.Copy(old, _vals, old.Length);
        _vals[old.Length] = new MapValue(mapind, val, false);
        OnUpdate?.Invoke(this);
    }
    public void SetMapValueNull(string map)
    {
        int mapind = GetOrAddMap(map);
        if (mapind == RotatableConfigConverterFactory.CurrentMap)
        {
            SetCurrentMapValueNull();
            return;
        }
        for (int i = 0; i < _vals.Length; ++i)
        {
            ref MapValue v = ref _vals[i];
            if (v._mapInd == mapind)
            {
                if (v._isNull)
                    return;
                v = new MapValue(mapind, default!, true);
                OnUpdate?.Invoke(this);
                return;
            }
        }
        MapValue[] old = _vals;
        _vals = new MapValue[_vals.Length + 1];
        Array.Copy(old, _vals, old.Length);
        _vals[old.Length] = new MapValue(mapind, default!, true);
        OnUpdate?.Invoke(this);
    }
    public void SetDefaultValue(T val)
    {
        if (val == null)
        {
            SetDefaultValueNull();
            return;
        }

        for (int i = 0; i < _vals.Length; ++i)
        {
            ref MapValue v = ref _vals[i];
            if (v._isDefault)
            {
                if (val.Equals(v._val))
                    return;
                v = new MapValue(-1, val, false);
                if (_isDefaulted)
                    SetValueIntl(val, false);
                OnUpdate?.Invoke(this);
                return;
            }
        }
        MapValue[] old = _vals;
        if (_current != -1)
            ++_current;
        else
        {
            _current = 0;
            if (_isDefaulted)
                SetValueIntl(val, false);
        }
        _vals = new MapValue[_vals.Length + 1];
        Array.Copy(old, 0, _vals, 1, old.Length);
        _vals[0] = new MapValue(-1, val, false);
        OnUpdate?.Invoke(this);
    }
    public void SetDefaultValueNull()
    {
        for (int i = 0; i < _vals.Length; ++i)
        {
            ref MapValue v = ref _vals[i];
            if (v._isDefault)
            {
                if (v._isNull)
                    return;
                v = new MapValue(-1, default!, true);
                OnUpdate?.Invoke(this);
                if (_isDefaulted)
                    SetValueIntl(default!, false);
                return;
            }
        }
        MapValue[] old = _vals;
        if (_current != -1)
            ++_current;
        else
        {
            _current = 0;
            if (_isDefaulted)
                SetValueIntl(default!, false);
        }
        _vals = new MapValue[_vals.Length + 1];
        Array.Copy(old, 0, _vals, 1, old.Length);
        _vals[0] = new MapValue(-1, default!, true);
        OnUpdate?.Invoke(this);
    }
    private static int GetOrAddMap(string map)
    {
        lock (_mapValueCache)
        {
            int index = RotatableConfigConverterFactory._maps.Count;
            for (int j = 0; j < RotatableConfigConverterFactory._maps.Count; ++j)
            {
                if (RotatableConfigConverterFactory._maps[j].Equals(map, StringComparison.OrdinalIgnoreCase))
                {
                    index = j;
                    break;
                }
            }
            if (index == RotatableConfigConverterFactory._maps.Count)
                RotatableConfigConverterFactory._maps.Add(map);
            return index;
        }
    }
    internal struct MapValue
    {
        public readonly bool _isDefault;
        public readonly int _mapInd;
        public readonly T _val;
        public readonly bool _isNull;

        public MapValue(T val, bool isNull)
        {
            _isDefault = true;
            _mapInd = -1;
            _val = val;
            _isNull = isNull;
        }
        public MapValue(int mapInd, T val, bool isNull)
        {
            _isDefault = mapInd == -1;
            _mapInd = mapInd;
            _val = val;
            _isNull = isNull;
        }
    }
    private void SetValueIntl(T newValue, bool callEvent)
    {
        if (_val is not null)
        {
            if (_val is INotifyValueUpdate notify)
                notify.OnUpdate -= OnValueUpdated;
        }
        if (newValue is INotifyValueUpdate notify2)
            notify2.OnUpdate += OnValueUpdated;

        if (callEvent)
            OnUpdate?.Invoke(this);
    }

    private void OnValueUpdated(object obj)
    {
        OnUpdate?.Invoke(this);
    }
}

public class RotatableDefaults<T> : List<KeyValuePair<string, T>>
{
    public void Add(string map, T value)
    {
        this.Add(new KeyValuePair<string, T>(map, value));
    }
}
internal class RotatableConfigConverterFactory : JsonConverterFactory
{
    internal static readonly List<string> _maps = new List<string>(8);

    internal static int CurrentMap
    {
        get
        {
            if (_cmap != -1) return _cmap;
            for (int i = 0; i < _maps.Count; ++i)
            {
                if (_maps[i].Equals(Provider.map, StringComparison.OrdinalIgnoreCase))
                {
                    _cmap = i;
                    return i;
                }
            }

            return -1;
        }
    }

    private static int _cmap = -1;

    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(RotatableConfig<>);
    public RotatableConfigConverterFactory()
    {

    }
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        Type[] types = typeToConvert.GetGenericArguments();
        if (types.Length != 1)
            throw new NotSupportedException("Invalid type given to " + nameof(RotatableConfigConverterFactory));
        Type assetType = types[0];
        return (JsonConverter)Activator.CreateInstance(typeof(RotatableConfigConverter<>).MakeGenericType(assetType));
    }
}

internal class RotatableConfigConverter<T> : JsonConverter<RotatableConfig<T>>
{
    public RotatableConfigConverter()
    {

    }
    public override RotatableConfig<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return RotatableConfig<T>.Read(ref reader);
    }

    public override void Write(Utf8JsonWriter writer, RotatableConfig<T> value, JsonSerializerOptions options)
    {
        value.Write(writer);
    }
}