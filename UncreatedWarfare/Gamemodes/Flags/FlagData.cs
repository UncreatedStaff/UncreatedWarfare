using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes.Flags
{
    // +x > right, +y > up
    public class FlagData
    {
        public int id;
        public string name;
        public float x;
        public float y;
        public ZoneData zone;
        public bool use_map_size_multiplier;
        public float minHeight = -1;
        public float maxHeight = -1;
        public AdjacentFlagData[] adjacencies;
        [JsonIgnore]
        public string color;
        [JsonIgnore]
        public Vector2 Position2D { get => new Vector2(x, y); }
        public FlagData()
        {
            this.color = UCWarfare.Config.FlagSettings.NeutralColor;
        }
        public FlagData(int id, string name, float x, float y, ZoneData zone, bool use_map_size_multiplier, float minHeight, float maxHeight)
        {
            this.id = id;
            this.name = name;
            this.x = x;
            this.y = y;
            this.zone = zone;
            this.use_map_size_multiplier = use_map_size_multiplier;
            this.color = UCWarfare.Config.FlagSettings.NeutralColor;
            this.minHeight = minHeight == default ? -1 : minHeight;
            this.maxHeight = maxHeight == default ? -1 : maxHeight;
            this.adjacencies = new AdjacentFlagData[0];
        }
        [JsonConstructor]
        public FlagData(int id, string name, float x, float y, ZoneData zone, bool use_map_size_multiplier, float minHeight, float maxHeight, AdjacentFlagData[] adjacencies)
        {
            this.id = id;
            this.name = name;
            this.x = x;
            this.y = y;
            this.zone = zone;
            this.use_map_size_multiplier = use_map_size_multiplier;
            this.color = UCWarfare.Config.FlagSettings.NeutralColor;
            this.minHeight = minHeight == default ? -1 : minHeight;
            this.maxHeight = maxHeight == default ? -1 : maxHeight;
            this.adjacencies = adjacencies ?? new AdjacentFlagData[0];
        }
        public static FlagData ReadFlagData(ref Utf8JsonReader reader)
        {
            FlagData data = new FlagData();
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    string val = reader.GetString();
                    if (!reader.Read()) continue;
                    switch (val)
                    {
                        case nameof(id):
                            data.id = reader.GetInt32();
                            break;
                        case nameof(name):
                            data.name = reader.GetString();
                            break;
                        case nameof(x):
                            data.x = (float)reader.GetDecimal();
                            break;
                        case nameof(y):
                            data.y = (float)reader.GetDecimal();
                            break;
                        case nameof(zone):
                            if (reader.TokenType == JsonTokenType.StartObject)
                            {
                                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                                {
                                    val = reader.GetString();
                                    if (reader.Read() && reader.TokenType == JsonTokenType.String)
                                    {
                                        if (val == nameof(ZoneData.type))
                                        {
                                            data.zone.type = reader.GetString();
                                        }
                                        else if (val == nameof(ZoneData.data))
                                        {
                                            data.zone.data = reader.GetString();
                                        }
                                    }
                                }
                            }
                            break;
                        case nameof(use_map_size_multiplier):
                            data.use_map_size_multiplier = reader.GetBoolean();
                            break;
                        case nameof(minHeight):
                            data.minHeight = (float)reader.GetDecimal();
                            break;
                        case nameof(maxHeight):
                            data.maxHeight = (float)reader.GetDecimal();
                            break;
                        case nameof(adjacencies):
                            if (reader.TokenType == JsonTokenType.StartArray)
                            {
                                List<AdjacentFlagData> tlist = new List<AdjacentFlagData>(4);
                                while (reader.Read() && reader.TokenType == JsonTokenType.StartObject)
                                {
                                    AdjacentFlagData afd = new AdjacentFlagData();
                                    afd.ReadJson(ref reader);
                                    tlist.Add(afd);
                                }
                                data.adjacencies = tlist.ToArray();
                            }
                            break;
                    }
                }
                else if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return data;
                }
            }
            return data;
        }
        public void WriteFlagData(Utf8JsonWriter writer)
        {
            writer.WriteProperty(nameof(id), id);
            writer.WriteProperty(nameof(name), name);
            writer.WriteProperty(nameof(x), x);
            writer.WriteProperty(nameof(y), y);
            writer.WritePropertyName(nameof(zone));
            writer.WriteStartObject();
            writer.WriteProperty(nameof(ZoneData.type), zone.type);
            writer.WriteProperty(nameof(ZoneData.data), zone.data);
            writer.WriteEndObject();
            writer.WriteProperty(nameof(use_map_size_multiplier), use_map_size_multiplier);
            writer.WriteProperty(nameof(minHeight), minHeight);
            writer.WriteProperty(nameof(maxHeight), maxHeight);

            writer.WritePropertyName(nameof(adjacencies));
            writer.WriteStartArray();
            for (int i = 0; i < adjacencies.Length; i++)
            {
                writer.WriteStartObject();
                adjacencies[i].WriteJson(writer);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }

        private const string N = "null";

        public override string ToString() =>
            $"FlagData for {name ?? N}, ID {id}. Flag type: {zone.type ?? N} ({zone.data ?? N}) at {x}, {y} from {minHeight} to {maxHeight} in height. " +
            $"World coords? {!use_map_size_multiplier}, Adjacent to: {string.Join(", ", adjacencies == null ? new string[1] { N } : adjacencies.Select(x => x.flag_id + " %= " + x.weight))}.";
    }
    public struct AdjacentFlagData : IJsonReadWrite
    {
        public int flag_id;
        public float weight;
        public AdjacentFlagData(int flagId, float weight = 1f)
        {
            this.flag_id = flagId;
            this.weight = weight;
        }

        public void ReadJson(ref Utf8JsonReader reader)
        {
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject) return;
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    string prop = reader.GetString();
                    if (!reader.Read()) return;
                    switch (prop)
                    {
                        case nameof(flag_id):
                            this.flag_id = reader.GetInt32();
                            break;
                        case nameof(weight):
                            this.weight = (float)reader.GetDecimal();
                            break;
                    }
                }
            }
        }
        public void WriteJson(Utf8JsonWriter writer)
        {
            writer.WriteProperty(nameof(flag_id), flag_id);
            writer.WriteProperty(nameof(weight), weight);
        }
    }
}
