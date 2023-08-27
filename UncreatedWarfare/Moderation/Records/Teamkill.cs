using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Uncreated.Encoding;
using Uncreated.SQL;

namespace Uncreated.Warfare.Moderation.Records;
[ModerationEntry(ModerationEntryType.Teamkill)]
[JsonConverter(typeof(ModerationEntryConverter))]
public class Teamkill : ModerationEntry
{
    [JsonPropertyName("death_cause")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EDeathCause? Cause { get; set; }

    [JsonPropertyName("item_guid")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Guid? Item { get; set; }

    [JsonPropertyName("item_name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ItemName { get; set; }

    [JsonPropertyName("limb")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ELimb? Limb { get; set; }

    [JsonPropertyName("distance")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public double? Distance { get; set; }

    [JsonPropertyName("death_message")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DeathMessage { get; set; }
    public override string GetDisplayName() => "Player Teamkill";
    protected override void ReadIntl(ByteReader reader, ushort version)
    {
        base.ReadIntl(reader, version);

        Cause = reader.ReadBool() ? (EDeathCause)reader.ReadUInt16() : null;
        Item = reader.ReadNullableGuid();
        ItemName = reader.ReadNullableString();
        Limb = reader.ReadBool() ? (ELimb)reader.ReadUInt16() : null;
        Distance = reader.ReadFloat();
        DeathMessage = reader.ReadNullableString();
    }

    protected override void WriteIntl(ByteWriter writer)
    {
        base.WriteIntl(writer);

        if (Cause.HasValue)
        {
            writer.Write(true);
            writer.Write((ushort)Cause.Value);
        }
        else writer.Write(false);
        writer.WriteNullable(Item);
        writer.WriteNullable(ItemName);
        if (Limb.HasValue)
        {
            writer.Write(true);
            writer.Write((ushort)Limb.Value);
        }
        else writer.Write(false);
        writer.WriteNullable(Distance);
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
                    Cause = (EDeathCause)num;
                    return;
                }

                throw new JsonException($"Invalid integer for EDeathCause: {num}.");
            }

            if (reader.TokenType == JsonTokenType.Null)
            {
                Cause = null;
                return;
            }

            string str = reader.GetString()!;
            if (!Enum.TryParse(str, true, out EDeathCause cause))
                throw new JsonException("Invalid string value for EDeathCause.");
            Cause = cause;
        }
        else if (propertyName.Equals("limb", StringComparison.InvariantCultureIgnoreCase))
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                int num = reader.GetInt32();
                if (num >= 0)
                {
                    Limb = (ELimb)num;
                    return;
                }

                throw new JsonException($"Invalid integer for ELimb: {num}.");
            }

            if (reader.TokenType == JsonTokenType.Null)
            {
                Limb = null;
                return;
            }

            string str = reader.GetString()!;
            if (!Enum.TryParse(str, true, out ELimb limb))
                throw new JsonException("Invalid string value for ELimb.");
            Limb = limb;
        }
        else if (propertyName.Equals("item_guid", StringComparison.InvariantCultureIgnoreCase))
            Item = reader.TokenType == JsonTokenType.Null ? new Guid?() : reader.GetGuid();
        else if (propertyName.Equals("item_name", StringComparison.InvariantCultureIgnoreCase))
            ItemName = reader.GetString();
        else if (propertyName.Equals("distance", StringComparison.InvariantCultureIgnoreCase))
            Distance = reader.TokenType == JsonTokenType.Null ? new double?() : reader.GetSingle();
        else if (propertyName.Equals("death_message", StringComparison.InvariantCultureIgnoreCase))
            DeathMessage = reader.GetString();
        else
            base.ReadProperty(ref reader, propertyName, options);
    }
    public override void Write(Utf8JsonWriter writer, JsonSerializerOptions options)
    {
        base.Write(writer, options);
        
        if (Cause.HasValue)
            writer.WriteString("death_cause", Cause.Value.ToString());
        if (Item.HasValue)
            writer.WriteString("item_guid", Item.Value.ToString());
        if (ItemName != null)
            writer.WriteString("item_name", ItemName);
        if (Limb.HasValue)
            writer.WriteString("limb", Limb.Value.ToString());
        if (Distance.HasValue)
            writer.WriteNumber("distance", Distance.Value);
        if (DeathMessage != null)
            writer.WriteString("death_message", DeathMessage);
    }

    internal override int EstimateColumnCount() => base.EstimateColumnCount() + 6;
    internal override bool AppendWriteCall(StringBuilder builder, List<object> args)
    {
        bool hasEvidenceCalls = base.AppendWriteCall(builder, args);

        builder.Append($" INSERT INTO `{DatabaseInterface.TableTeamkills}` ({SqlTypes.ColumnList(
            DatabaseInterface.ColumnExternalPrimaryKey, DatabaseInterface.ColumnTeamkillsAsset, DatabaseInterface.ColumnTeamkillsAssetName,
            DatabaseInterface.ColumnTeamkillsDeathCause, DatabaseInterface.ColumnTeamkillsDeathMessage, DatabaseInterface.ColumnTeamkillsDistance,
            DatabaseInterface.ColumnTeamkillsLimb)}) VALUES ");

        F.AppendPropertyList(builder, args.Count, 6, 0, 1);
        builder.Append(" AS `t` " +
                       $"ON DUPLICATE KEY UPDATE `{DatabaseInterface.ColumnTeamkillsAsset}` = `t`.`{DatabaseInterface.ColumnTeamkillsAsset}`," +
                       $"`{DatabaseInterface.ColumnTeamkillsAssetName}` = `t`.`{DatabaseInterface.ColumnTeamkillsAssetName}`," +
                       $"`{DatabaseInterface.ColumnTeamkillsDeathCause}` = `t`.`{DatabaseInterface.ColumnTeamkillsDeathCause}`," +
                       $"`{DatabaseInterface.ColumnTeamkillsDeathMessage}` = `t`.`{DatabaseInterface.ColumnTeamkillsDeathMessage}`," +
                       $"`{DatabaseInterface.ColumnTeamkillsDistance}` = `t`.`{DatabaseInterface.ColumnTeamkillsDistance}`," +
                       $"`{DatabaseInterface.ColumnTeamkillsLimb}` = `t`.`{DatabaseInterface.ColumnTeamkillsLimb}`;");
        
        args.Add(Item.HasValue ? Item.Value.ToString("N") : DBNull.Value);
        args.Add((object?)ItemName.MaxLength(48) ?? DBNull.Value);
        args.Add(Cause.HasValue ? Cause.Value.ToString() : DBNull.Value);
        args.Add((object?)Message.MaxLength(255) ?? DBNull.Value);
        args.Add(Distance.HasValue ? Distance.Value : DBNull.Value);
        args.Add(Limb.HasValue ? Limb.Value.ToString() : DBNull.Value);

        return hasEvidenceCalls;
    }
}