using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Json;
using Uncreated.SQL;
using Uncreated.Warfare.Singletons;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes.Flags;

[SingletonDependency(typeof(ZoneList))]
public class ElectricalGridManager : BaseSingleton
{

    // todo


    public override void Load()
    {

    }

    public override void Unload()
    {

    }
}

/// <param name="PrimaryKey">Flag primary key.</param>
public record struct GridObject(
    [property: JsonPropertyName("flag_id")] PrimaryKey PrimaryKey,
    [property: JsonPropertyName("instance_id")] uint ObjectInstanceId,
    [property: JsonPropertyName("object_guid")] Guid Guid,
    [property: JsonPropertyName("x")] float X,
    [property: JsonPropertyName("y")] float Y,
    [property: JsonPropertyName("z")] float Z) : IJsonReadWrite, IListItem
{
    public GridObject() : this (PrimaryKey.NotAssigned, uint.MaxValue, Guid.Empty, 0f, 0f, 0f) { }
    public override string ToString() => $"Flag: {PrimaryKey}, Object: {ObjectInstanceId}.";
    public void WriteJson(Utf8JsonWriter writer)
    {
        writer.WriteProperty("flag_id", PrimaryKey);
        writer.WriteProperty("instance_id", ObjectInstanceId);
        writer.WriteProperty("object_guid", Guid);
        writer.WritePropertyName("position");
        JsonSerializer.Serialize(writer, new Vector3(X, Y, Z), writer.Options.Indented ? JsonEx.serializerSettings : JsonEx.condensedSerializerSettings);
    }

    public void ReadJson(ref Utf8JsonReader reader)
    {
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject) return;
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string prop = reader.GetString()!;
                if (!reader.Read()) return;
                if (string.Equals(prop, "flag_id", StringComparison.OrdinalIgnoreCase))
                    this.PrimaryKey = reader.GetInt32();
                else if (string.Equals(prop, "instance_id", StringComparison.OrdinalIgnoreCase))
                    this.ObjectInstanceId = reader.GetUInt32();
                else if (string.Equals(prop, "object_guid", StringComparison.OrdinalIgnoreCase))
                    this.Guid = reader.GetGuid();
                else if (string.Equals(prop, "position", StringComparison.OrdinalIgnoreCase))
                {
                    Vector3 pos = JsonSerializer.Deserialize<Vector3>(ref reader, JsonEx.condensedSerializerSettings);
                    X = pos.x;
                    Y = pos.y;
                    Z = pos.z;
                }
            }
        }
    }
}