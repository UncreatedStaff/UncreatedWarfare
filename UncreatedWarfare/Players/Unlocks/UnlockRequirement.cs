using DanielWillett.ReflectionTools;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Uncreated.Encoding;
using Uncreated.Framework;
using Uncreated.Json;
using Uncreated.SQL;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Traits;
using Uncreated.Warfare.Vehicles;

namespace Uncreated.Warfare.Players.Unlocks;

[JsonConverter(typeof(UnlockRequirementConverter))]
public abstract class UnlockRequirement : ICloneable, IVersionableReadWrite
{
    public uint PrimaryKey { get; set; }

    public const byte DataVersion = 0;
    private static readonly Dictionary<byte, KeyValuePair<Type, string[]>> Types = new Dictionary<byte, KeyValuePair<Type, string[]>>(4);
    private static readonly Dictionary<Type, byte> TypesInverse = new Dictionary<Type, byte>(4);
    private static bool _hasReflected;
    private static void Reflect()
    {
        if (_hasReflected)
            return;
        Types.Clear();
        foreach (Type type in Accessor.GetTypesSafe(true).Where(typeof(UnlockRequirement).IsAssignableFrom))
        {
            if (type.IsAbstract)
                continue;
            if (!TypesInverse.ContainsKey(type) && Attribute.GetCustomAttribute(type, typeof(UnlockRequirementAttribute)) is UnlockRequirementAttribute att && !Types.ContainsKey(att.Type))
            {
                Types.Add(att.Type, new KeyValuePair<Type, string[]>(type, att.Properties));
                TypesInverse.Add(type, att.Type);
            }
        }
        _hasReflected = true;
    }
    public abstract bool CanAccess(UCPlayer player);
    public static UnlockRequirement? Read(ref Utf8JsonReader reader)
    {
        if (!_hasReflected) Reflect();
        UnlockRequirement? t = null;
        while (reader.TokenType == JsonTokenType.PropertyName || (reader.Read() && reader.TokenType == JsonTokenType.PropertyName))
        {
            string? property = reader.GetString();
            if (reader.Read() && property != null)
            {
                if (t != null)
                {
                    t.ReadProperty(ref reader, property);
                    continue;
                }

                foreach (KeyValuePair<byte, KeyValuePair<Type, string[]>> propertyList in Types)
                {
                    string[] values = propertyList.Value.Value;
                    for (int i = 0; i < values.Length; i++)
                    {
                        if (property.Equals(values[i], StringComparison.OrdinalIgnoreCase))
                        {
                            t = Activator.CreateInstance(propertyList.Value.Key) as UnlockRequirement;
                            goto done;
                        }
                    }
                }
                continue;
            done:
                if (t != null)
                    t.ReadProperty(ref reader, property);
                else
                {
                    L.LogWarning("Failed to find property \"" + property + "\" when parsing unlock requirements.");
                }
            }
        }
        return t;
    }
    public static UnlockRequirement? Read(MySqlDataReader reader, int colOffset = 0)
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(reader.GetString(colOffset + 1));
        Utf8JsonReader reader2 = new Utf8JsonReader(bytes, JsonEx.readerOptions);
        return Read(ref reader2);
    }
    public static void Write(Utf8JsonWriter writer, UnlockRequirement requirement)
    {
        requirement.WriteProperties(writer);
    }
    public string ToJson(bool condensed = true)
    {
        using MemoryStream stream = new MemoryStream(32);
        using Utf8JsonWriter writer = new Utf8JsonWriter(stream, condensed ? JsonEx.condensedWriterOptions : JsonEx.writerOptions);
        writer.Flush();
        return System.Text.Encoding.UTF8.GetString(stream.GetBuffer(), 0, (int)stream.Position);
    }
    protected abstract void ReadProperty(ref Utf8JsonReader reader, string property);
    protected abstract void WriteProperties(Utf8JsonWriter writer);
    public abstract string GetSignText(UCPlayer player);
    public abstract object Clone();
    protected abstract void Read(ByteReader reader);
    byte IVersionableReadWrite.Version { get; set; }
    public static void WriteRequirement(ByteWriter writer, UnlockRequirement? req)
    {
        if (req == null)
        {
            writer.Write(false);
            return;
        }

        writer.Write(true);
        Reflect();
        if (!TypesInverse.TryGetValue(req.GetType(), out byte val))
            throw new ArgumentException("Unknown type: " + req.GetType().Name, nameof(req));
        writer.Write(val);

        req.Write(writer);
    }
    public static UnlockRequirement? ReadRequirement(ByteReader reader)
    {
        if (!reader.ReadBool())
            return null;

        byte type = reader.ReadUInt8();
        if (Types.TryGetValue(type, out KeyValuePair<Type, string[]> typeData) && Activator.CreateInstance(typeData.Key) is UnlockRequirement t)
        {
            return t;
        }

        throw new Exception("Unable to create unlock requirement with type id " + type + "!");
    }
    void IReadWrite.Write(ByteWriter writer)
    {
        writer.Write(DataVersion);

        Write(writer);
    }
    void IReadWrite.Read(ByteReader reader)
    {
        ((IVersionableReadWrite)this).Version = reader.ReadUInt8();

        Read(reader);
    }

    protected abstract void Write(ByteWriter writer);
    // ReSharper disable InconsistentNaming
    public const string COLUMN_PK = "pk";
    public const string COLUMN_JSON = "JSON";
    // ReSharper restore InconsistentNaming
    public static Schema GetDefaultSchema(string tableName, string fkColumn, string mainTable, string mainPkColumn, bool oneToOne = false, bool hasPk = false)
    {
        if (!oneToOne && fkColumn.Equals(COLUMN_PK, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Foreign key column may not be the same as \"" + COLUMN_PK + "\".", nameof(fkColumn));
        int ct = 2;
        if (!oneToOne && hasPk)
            ++ct;
        Schema.Column[] columns = new Schema.Column[ct];
        int index = 0;
        if (!oneToOne && hasPk)
        {
            columns[0] = new Schema.Column(COLUMN_PK, SqlTypes.INCREMENT_KEY)
            {
                PrimaryKey = true,
                AutoIncrement = true
            };
        }
        else index = -1;
        columns[++index] = new Schema.Column(fkColumn, SqlTypes.INCREMENT_KEY)
        {
            PrimaryKey = oneToOne,
            AutoIncrement = oneToOne,
            ForeignKey = true,
            ForeignKeyColumn = mainPkColumn,
            ForeignKeyTable = mainTable
        };
        columns[++index] = new Schema.Column(COLUMN_JSON, SqlTypes.STRING_255);
        return new Schema(tableName, columns, false, typeof(UnlockRequirement));
    }
    public virtual Exception RequestKitFailureToMeet(CommandContext ctx, Kit kit)
    {
        L.LogWarning("Unhandled kit requirement type: " + GetType().Name);
        return ctx.SendUnknownError();
    }
    public virtual Exception RequestVehicleFailureToMeet(CommandContext ctx, VehicleData data)
    {
        L.LogWarning("Unhandled vehicle requirement type: " + GetType().Name);
        return ctx.SendUnknownError();
    }
    public virtual Exception RequestTraitFailureToMeet(CommandContext ctx, TraitData trait)
    {
        L.LogWarning("Unhandled trait requirement type: " + GetType().Name);
        return ctx.SendUnknownError();
    }
}
public class UnlockRequirementConverter : JsonConverter<UnlockRequirement>
{
    public override UnlockRequirement Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => UnlockRequirement.Read(ref reader)!;
    public override void Write(Utf8JsonWriter writer, UnlockRequirement value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        UnlockRequirement.Write(writer, value);
        writer.WriteEndObject();
    }
}

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class UnlockRequirementAttribute : Attribute
{
    private readonly string[] _properties;
    private readonly byte _type;
    public string[] Properties => _properties;
    public byte Type => _type;

    /// <param name="properties">Must be unique among other unlock requirements.</param>
    public UnlockRequirementAttribute(byte type, params string[] properties)
    {
        _properties = properties;
        _type = type;
    }
}