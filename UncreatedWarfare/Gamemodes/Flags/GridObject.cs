using SDG.Unturned;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes.Flags;

public class GridObject : IJsonReadWrite, IListItem
{
    [JsonPropertyName("flag_id")]
    public uint PrimaryKey { get; set; }

    [JsonPropertyName("instance_id")]
    public uint ObjectInstanceId { get; set; }

    [JsonPropertyName("object_guid")]
    public Guid Guid { get; set; }

    [JsonPropertyName("x")]
    public float X { get; set; }

    [JsonPropertyName("y")]
    public float Y { get; set; }

    [JsonPropertyName("z")]
    public float Z { get; set; }

    [JsonIgnore]
    public LevelObject? Object { get; set; }

    public GridObject(uint primaryKey, uint objectInstanceId, Guid guid, float x, float y, float z, LevelObject? @object = null)
    {
        PrimaryKey = primaryKey;
        ObjectInstanceId = objectInstanceId;
        Guid = guid;
        X = x;
        Y = y;
        Z = z;
        Object = @object;
        if (@object == null && LevelObjects.objects != null)
            Object = UCBarricadeManager.FindObject(objectInstanceId, new Vector3(x, y, z));
    }

    public GridObject() : this (0, uint.MaxValue, Guid.Empty, 0f, 0f, 0f) { }
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
                    PrimaryKey = reader.GetUInt32();
                else if (string.Equals(prop, "instance_id", StringComparison.OrdinalIgnoreCase))
                    ObjectInstanceId = reader.GetUInt32();
                else if (string.Equals(prop, "object_guid", StringComparison.OrdinalIgnoreCase))
                    Guid = reader.GetGuid();
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