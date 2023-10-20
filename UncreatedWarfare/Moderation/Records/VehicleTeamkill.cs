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
    public override string GetDisplayName() => "Vehicle Teamkill";
    protected override void ReadIntl(ByteReader reader, ushort version)
    {
        base.ReadIntl(reader, version);

        Origin = reader.ReadBool() ? (EDamageOrigin)reader.ReadUInt16() : null;
        Vehicle = reader.ReadNullableGuid();
        VehicleName = reader.ReadNullableString();
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
        else if (propertyName.Equals("vehicle_guid", StringComparison.InvariantCultureIgnoreCase))
            Vehicle = reader.TokenType == JsonTokenType.Null ? new Guid?() : reader.GetGuid();
        else if (propertyName.Equals("vehicle_name", StringComparison.InvariantCultureIgnoreCase))
            VehicleName = reader.GetString();
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
    }
    internal override int EstimateParameterCount() => base.EstimateParameterCount() + 6;
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
    }

    internal override bool AppendWriteCall(StringBuilder builder, List<object> args)
    {
        bool hasEvidenceCalls = base.AppendWriteCall(builder, args);

        builder.Append($" INSERT INTO `{DatabaseInterface.TableVehicleTeamkills}` ({SqlTypes.ColumnList(
            DatabaseInterface.ColumnExternalPrimaryKey, DatabaseInterface.ColumnVehicleTeamkillsVehicleAsset,
            DatabaseInterface.ColumnVehicleTeamkillsVehicleAssetName, DatabaseInterface.ColumnVehicleTeamkillsDamageOrigin)}) VALUES ");

        F.AppendPropertyList(builder, args.Count, 3, 0, 1);
        builder.Append(" AS `t` " +
                       $"ON DUPLICATE KEY UPDATE `{DatabaseInterface.ColumnVehicleTeamkillsVehicleAsset}` = `t`.`{DatabaseInterface.ColumnVehicleTeamkillsVehicleAsset}`," +
                       $"`{DatabaseInterface.ColumnVehicleTeamkillsVehicleAssetName}` = `t`.`{DatabaseInterface.ColumnVehicleTeamkillsVehicleAssetName}`," +
                       $"`{DatabaseInterface.ColumnVehicleTeamkillsDamageOrigin}` = `t`.`{DatabaseInterface.ColumnVehicleTeamkillsDamageOrigin}`;");

        args.Add(Vehicle.HasValue ? Vehicle.Value.ToString("N") : DBNull.Value);
        args.Add((object?)VehicleName.MaxLength(48) ?? DBNull.Value);
        args.Add(Origin.HasValue ? Origin.Value.ToString() : DBNull.Value);

        return hasEvidenceCalls;
    }
}
