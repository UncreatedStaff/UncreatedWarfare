using System.Text.Json;
using System.Text.Json.Serialization;
using Uncreated.Framework;
using Uncreated.Json;
using Uncreated.SQL;

namespace Uncreated.Warfare.Gamemodes.Flags;

public struct AdjacentFlagData : IJsonReadWrite, IListItem
{
    [JsonPropertyName("flag_id")]
    public int LegacyFlagId
    {
        get => PrimaryKey.Key;
        set => PrimaryKey = value;
    }

    [JsonIgnore]
    public PrimaryKey PrimaryKey { get; set; }

    [JsonPropertyName("weight")]
    public float Weight;
    public AdjacentFlagData(int flagId, float weight = 1f)
    {
        PrimaryKey = flagId;
        Weight = weight;
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
                switch (prop)
                {
                    case "flag_id":
                        PrimaryKey = reader.GetInt32();
                        break;
                    case "weight":
                        Weight = (float)reader.GetDecimal();
                        break;
                }
            }
        }
    }
    public void WriteJson(Utf8JsonWriter writer)
    {
        writer.WriteProperty("flag_id", PrimaryKey);
        writer.WriteProperty("weight", Weight);
    }
}