using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Encoding;
using Uncreated.Framework;
using Uncreated.SQL;

namespace Uncreated.Warfare.Moderation.Records;
[ModerationEntry(ModerationEntryType.VehicleTeamkill)]
[JsonConverter(typeof(ModerationEntryConverter))]
public class VehicleTeamkill : ModerationEntry
{
    [JsonPropertyName("damage_origin")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EDamageOrigin? Origin { get; set; }

    [JsonPropertyName("vehicle_guid")]
    public Guid? Vehicle { get; set; }

    [JsonPropertyName("vehicle_name")]
    public string? VehicleName { get; set; }

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

        Origin = reader.ReadBool() ? (EDamageOrigin)reader.ReadUInt16() : null;
        Vehicle = reader.ReadNullableGuid();
        VehicleName = reader.ReadNullableString();
        Item = reader.ReadNullableGuid();
        ItemName = reader.ReadNullableString();
        DeathMessage = reader.ReadNullableString();
    }

    protected override void WriteIntl(ByteWriter writer)
    {
        base.WriteIntl(writer);

        if (Origin.HasValue)
        {
            writer.Write(true);
            writer.Write((ushort)Origin.Value);
        }
        else writer.Write(false);
        
        writer.WriteNullable(Vehicle);
        writer.WriteNullable(VehicleName);
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
            
            if (reader.TokenType == JsonTokenType.Null)
            {
                Origin = null;
                return;
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
            Vehicle = reader.TokenType == JsonTokenType.Null ? new Guid?() : reader.GetGuid();
        else if (propertyName.Equals("vehicle_name", StringComparison.InvariantCultureIgnoreCase))
            VehicleName = reader.GetString();
        else if (propertyName.Equals("death_message", StringComparison.InvariantCultureIgnoreCase))
            DeathMessage = reader.GetString();
        else
            base.ReadProperty(ref reader, propertyName, options);
    }
    public override void Write(Utf8JsonWriter writer, JsonSerializerOptions options)
    {
        base.Write(writer, options);

        if (Origin.HasValue)
         writer.WriteString("damage_origin", Origin.Value.ToString());
        if (Vehicle.HasValue)
            writer.WriteString("vehicle_guid", Vehicle.Value);
        if (VehicleName != null)
            writer.WriteString("vehicle_name", VehicleName);
        if (Item.HasValue)
            writer.WriteString("item_guid", Item.Value.ToString());
        if (ItemName != null)
            writer.WriteString("item_name", ItemName);
        if (DeathMessage != null)
            writer.WriteString("death_message", DeathMessage);
    }

    internal override int EstimateColumnCount() => base.EstimateColumnCount() + 6;
    public override async Task AddExtraInfo(DatabaseInterface db, List<string> workingList, IFormatProvider formatter, CancellationToken token = default)
    {
        await base.AddExtraInfo(db, workingList, formatter, token);

        workingList.Add($"Damage Origin: {Origin}");
        if (Vehicle.HasValue)
        {
            string name;
            if (UCWarfare.IsLoaded && Assets.find(Vehicle.Value) is VehicleAsset veh)
            {
                name = veh.FriendlyName ?? veh.name;
                if (veh.id > 0)
                    name += " (" + veh.id.ToString(formatter) + ")";
            }
            else
                name = VehicleName ?? Vehicle.Value.ToString("N");
            workingList.Add($"Vehicle: {name}");
        }
        if (Item.HasValue)
        {
            string name;
            if (UCWarfare.IsLoaded && Assets.find(Item.Value) is ItemAsset item)
            {
                name = item.FriendlyName ?? item.name;
                if (item.id > 0)
                    name += " (" + item.id.ToString(formatter) + ")";
            }
            else
                name = ItemName ?? Item.Value.ToString("N");
            workingList.Add($"Item: {name}");
        }

        if (Message != null)
            workingList.Add(Message.MaxLength(128)!);
    }

    internal override bool AppendWriteCall(StringBuilder builder, List<object> args)
    {
        bool hasEvidenceCalls = base.AppendWriteCall(builder, args);

        builder.Append($" INSERT INTO `{DatabaseInterface.TableVehicleTeamkills}` ({SqlTypes.ColumnList(
            DatabaseInterface.ColumnExternalPrimaryKey, DatabaseInterface.ColumnVehicleTeamkillsVehicleAsset, DatabaseInterface.ColumnVehicleTeamkillsVehicleAssetName,
            DatabaseInterface.ColumnVehicleTeamkillsAsset, DatabaseInterface.ColumnVehicleTeamkillsAssetName, DatabaseInterface.ColumnVehicleTeamkillsDamageOrigin,
            DatabaseInterface.ColumnVehicleTeamkillsDeathMessage)}) VALUES ");

        F.AppendPropertyList(builder, args.Count, 6, 0, 1);
        builder.Append(" AS `t` " +
                       $"ON DUPLICATE KEY UPDATE `{DatabaseInterface.ColumnVehicleTeamkillsVehicleAsset}` = `t`.`{DatabaseInterface.ColumnVehicleTeamkillsVehicleAsset}`," +
                       $"`{DatabaseInterface.ColumnVehicleTeamkillsVehicleAssetName}` = `t`.`{DatabaseInterface.ColumnVehicleTeamkillsVehicleAssetName}`," +
                       $"`{DatabaseInterface.ColumnVehicleTeamkillsAsset}` = `t`.`{DatabaseInterface.ColumnVehicleTeamkillsAsset}`," +
                       $"`{DatabaseInterface.ColumnVehicleTeamkillsAssetName}` = `t`.`{DatabaseInterface.ColumnVehicleTeamkillsAssetName}`," +
                       $"`{DatabaseInterface.ColumnVehicleTeamkillsDamageOrigin}` = `t`.`{DatabaseInterface.ColumnVehicleTeamkillsDamageOrigin}`," +
                       $"`{DatabaseInterface.ColumnVehicleTeamkillsDeathMessage}` = `t`.`{DatabaseInterface.ColumnVehicleTeamkillsDeathMessage}`;");

        args.Add(Vehicle.HasValue ? Vehicle.Value.ToString("N") : DBNull.Value);
        args.Add((object?)VehicleName.MaxLength(48) ?? DBNull.Value);
        args.Add(Item.HasValue ? Item.Value.ToString("N") : DBNull.Value);
        args.Add((object?)ItemName.MaxLength(48) ?? DBNull.Value);
        args.Add(Origin.HasValue ? Origin.Value.ToString() : DBNull.Value);
        args.Add((object?)Message.MaxLength(255) ?? DBNull.Value);

        return hasEvidenceCalls;
    }
}
