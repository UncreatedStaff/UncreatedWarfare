using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Uncreated.Warfare.Maps;
[JsonConverter(typeof(RotatableConfigConverterFactory))]
public class RotatableConfig<T>
{
    private const string DEFAULT_VALUE_MAP_NAME = "_";
    private static readonly Type type = typeof(T);
    private static readonly List<MapValue> _mapValueCache = new List<MapValue>(8);
    private static readonly bool isNullableClass = !type.IsValueType;
    private static readonly bool isNullableStruct = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
    private readonly MapValue[] _vals;
    private readonly bool _isNull;
    private readonly bool _isDefaulted;
    private readonly T _val;
    private readonly int _current = -1;

    public T Value
    {
        get
        {
            if (_isNull)
                throw new NullReferenceException("Value is null.");
            return _val;
        }
    }
    public bool HasValue => !_isNull;
    public bool SelectedIsDefault => _isDefaulted;
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

    public static implicit operator T(RotatableConfig<T> config)
    {
        if (config._isNull)
        {
            if (isNullableClass) return default!;
            if (isNullableStruct) return (T)Activator.CreateInstance(type);
        }
        return config.Value;
    }

    public static implicit operator RotatableConfig<T>(T def)
    {
        return new RotatableConfig<T>(def);
    }

    public static bool operator ==(RotatableConfig<T>? left, RotatableConfig<T>? right) => left is null ? right is null : left.Equals(right);
    public static bool operator !=(RotatableConfig<T>? left, RotatableConfig<T>? right) => !(left == right);

    public override bool Equals(object obj)
    {
        if (obj is RotatableConfig<T> t) return Equals(t);
        return base.Equals(obj);
    }

    public bool Equals(RotatableConfig<T>? other)
    {
        if (other is null || other._isNull)
            return this is null || this._isNull;
        if (this is null || this._isNull) return false;
        if (ReferenceEquals(this, other))
            return true;
        return other.Value!.Equals(this.Value);
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
                if (_maps[i].Equals(Provider.map))
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