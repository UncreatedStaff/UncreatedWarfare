using SDG.Unturned;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
using Uncreated.Encoding;
using Uncreated.Framework;

namespace Uncreated.Warfare.Sync;
[JsonConverter(typeof(SyncableArrayConverterFactory))]
public class SyncableArray<T> : INotifyValueUpdate, IEnumerable<T>, IReadWrite
{
    private static ByteReader.Reader<T>? readerCallback = null;
    private static ByteWriter.Writer<T>? writerCallback = null;

    public event Action<object>? OnUpdate;
    private T[] Items;
    public int Length => Items.Length;

    public SyncableArray()
    {
        Items = Array.Empty<T>();
    }
    public SyncableArray(T[] items)
    {
        Items = items ?? throw new ArgumentNullException(nameof(items));
    }
    public T this[int index]
    {
        get => Items[index];
        set
        {
            if (Items[index] == null)
            {
                if (value == null)
                    return;
            }
            if (value != null && value.Equals(Items[index]))
            {
                Items[index] = value;
                return;
            }

            Items[index] = value;
            OnUpdate?.Invoke(this);
        }
    }
    public static bool operator ==(SyncableArray<T> left, SyncableArray<T> right) => left is null ? right is null : left.Equals(right);
    public static bool operator !=(SyncableArray<T> left, SyncableArray<T> right) => !(left == right);
    public static bool operator ==(SyncableArray<T> left, T[] right) => left is null ? right is null : left.Equals(right);
    public static bool operator !=(SyncableArray<T> left, T[] right) => !(left == right);
    public static bool operator ==(T[] left, SyncableArray<T> right) => left is null ? right is null : left.Equals(right);
    public static bool operator !=(T[] left, SyncableArray<T> right) => !(left == right);

    public static implicit operator SyncableArray<T>(T[] array) => new SyncableArray<T>(array);
    public override bool Equals(object obj)
    {
        if (obj is SyncableArray<T> t) return Equals(t);
        else if (obj is T[] t2) return Equals(t2);
        else return false;
    }
    public override int GetHashCode() => Items.GetHashCode();
    public bool Equals(SyncableArray<T>? other)
    {
        if (other is null)
            return this is null;
        if (this is null)
            return false;
        if (other.Items.Length != Items.Length)
            return false;
        for (int i = 0; i < Items.Length; ++i)
        {
            T item1 = Items[i];
            T item2 = other.Items[i];
            if (item1 is null)
            {
                if (item2 is not null)
                    return false;
                else continue;
            }
            else if (item2 is null)
                return false;
            if (!item1.Equals(item2))
                return false;
        }
        return true;
    }
    public bool Equals(T[]? other)
    {
        if (other is null)
            return this is null;
        if (this is null)
            return false;
        if (other.Length != Items.Length)
            return false;
        for (int i = 0; i < Items.Length; ++i)
        {
            T item1 = Items[i];
            T item2 = other[i];
            if (item1 is null)
            {
                if (item2 is not null)
                    return false;
                else continue;
            }
            else if (item2 is null)
                return false;
            if (!item1.Equals(item2))
                return false;
        }
        return true;
    }

    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)Items).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => Items.GetEnumerator();
    public void Read(ByteReader reader)
    {
        readerCallback ??= !typeof(T).IsValueType ||
                           (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() != typeof(Nullable<>))
            ? ByteReader.NullableReaderHelper<T>.Reader!
            : ByteReader.ReaderHelper<T>.Reader!;
        int length = reader.ReadInt32();
        Items = new T[length];
        for (int i = 0; i < length; ++i)
        {
            Items[i] = readerCallback(reader);
        }

        OnUpdate?.Invoke(this);
    }
    public void Write(ByteWriter writer)
    {
        writerCallback ??= !typeof(T).IsValueType ||
                           (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() != typeof(Nullable<>))
            ? ByteWriter.NullableWriterHelper<T>.Writer!
            : ByteWriter.WriterHelper<T>.Writer!;
        writer.Write(Items.Length);
        for (int i = 0; i < Items.Length; ++i)
        {
            writerCallback(writer, Items[i]);
        }
    }
    internal static SyncableArray<T>? Read(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected start array token.");
        List<T> list = new List<T>(16);
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                break;
            list.Add((T)JsonSerializer.Deserialize(ref reader, typeof(T), options)!);
        }
        return new SyncableArray<T>(list.ToArray());
    }
    internal void Write(Utf8JsonWriter writer, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        for (int i = 0; i < Items.Length; ++i)
        {
            JsonSerializer.Serialize(Items[i], typeof(T), options);
        }
        writer.WriteEndArray();
    }
}

internal class SyncableArrayConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(SyncableArray<>);
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        Type[] types = typeToConvert.GetGenericArguments();
        if (types.Length != 1)
            throw new NotSupportedException("Invalid type given to " + nameof(SyncableArrayConverterFactory));
        Type assetType = types[0];
        return (JsonConverter)Activator.CreateInstance(typeof(SyncableArrayConverter<>).MakeGenericType(assetType));
    }
}

internal class SyncableArrayConverter<T> : JsonConverter<SyncableArray<T>>
{
    public override SyncableArray<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return SyncableArray<T>.Read(ref reader, options);
    }

    public override void Write(Utf8JsonWriter writer, SyncableArray<T>? value, JsonSerializerOptions options)
    {
        if (value is null)
            writer.WriteNullValue();
        else
            value.Write(writer, options);
    }
}