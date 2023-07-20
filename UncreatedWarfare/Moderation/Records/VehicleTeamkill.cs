using SDG.Unturned;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Uncreated.Encoding;

namespace Uncreated.Warfare.Moderation.Records;
[ModerationEntry(ModerationEntryType.VehicleTeamkill)]
[JsonConverter(typeof(ModerationEntryConverter))]
public class VehicleTeamkill : ModerationEntry
{
    [JsonPropertyName("damage_origin")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EDamageOrigin Origin { get; set; }

    [JsonPropertyName("vehicle_guid")]
    public Guid Vehicle { get; set; }

    [JsonPropertyName("vehicle_name")]
    public string VehicleName { get; set; }

    [JsonPropertyName("item_guid")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Guid? Item { get; set; }

    [JsonPropertyName("item_name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ItemName { get; set; }

    [JsonPropertyName("death_message")]
    public string? DeathMessage { get; set; }
    public override string GetDisplayName() => "Vehicle Teamkill";
    protected override void ReadIntl(ByteReader reader, ushort version)
    {
        base.ReadIntl(reader, version);

        Origin = (EDamageOrigin)reader.ReadUInt16();
        Vehicle = reader.ReadGuid();
        VehicleName = reader.ReadString();
        Item = reader.ReadNullableGuid();
        ItemName = reader.ReadNullableString();
        DeathMessage = reader.ReadNullableString();
    }

    protected override void WriteIntl(ByteWriter writer)
    {
        base.WriteIntl(writer);

        writer.Write((ushort)Origin);
        writer.Write(Vehicle);
        writer.Write(VehicleName);
        writer.WriteNullable(Item);
        writer.WriteNullable(ItemName);
        writer.WriteNullable(DeathMessage);
    }
    public override void ReadProperty(ref Utf8JsonReader reader, string propertyName, JsonSerializerOptions options)
    {
        if (propertyName.Equals("death_cause", StringComparison.InvariantCultureIgnoreCase))
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                int num = reader.GetInt32();
                if (num >= 0)
                {
                    Origin = (EDamageOrigin)num;
                    return;
                }

                throw new JsonException($"Invalid integer for EDamageOrigin: {num}.");
            }
            string str = reader.GetString()!;
            if (!Enum.TryParse(str, true, out EDamageOrigin origin))
                throw new JsonException("Invalid string value for EDamageOrigin.");
            Origin = origin;
        }
        else if (propertyName.Equals("item_guid", StringComparison.InvariantCultureIgnoreCase))
            Item = reader.TokenType == JsonTokenType.Null ? new Guid?() : reader.GetGuid();
        else if (propertyName.Equals("item_name", StringComparison.InvariantCultureIgnoreCase))
            ItemName = reader.GetString();
        else if (propertyName.Equals("vehicle_guid", StringComparison.InvariantCultureIgnoreCase))
            Vehicle = reader.TokenType == JsonTokenType.Null ? default : reader.GetGuid();
        else if (propertyName.Equals("vehicle_name", StringComparison.InvariantCultureIgnoreCase))
            VehicleName = reader.GetString() ?? string.Empty;
        else if (propertyName.Equals("death_message", StringComparison.InvariantCultureIgnoreCase))
            DeathMessage = reader.GetString();
        else
            base.ReadProperty(ref reader, propertyName, options);
    }
    public override void Write(Utf8JsonWriter writer, JsonSerializerOptions options)
    {
        base.Write(writer, options);

        writer.WriteString("damage_origin", Origin.ToString());
        writer.WriteString("vehicle_guid", Vehicle);
        writer.WriteString("vehicle_name", VehicleName);
        if (Item.HasValue)
            writer.WriteString("item_guid", Item.Value.ToString());
        if (ItemName != null)
            writer.WriteString("item_name", ItemName);
        if (DeathMessage != null)
            writer.WriteString("death_message", DeathMessage);
    }
}
