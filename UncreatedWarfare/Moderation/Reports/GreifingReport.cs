using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Uncreated.Encoding;
using Uncreated.Framework;
using Uncreated.SQL;
using Uncreated.Warfare.Structures;

namespace Uncreated.Warfare.Moderation.Reports;

/*
 * Decided to remove all the random report reasons in place of a few small categories, mainly this one.
 * Should make it less complex for players.
 */
[ModerationEntry(ModerationEntryType.GreifingReport)]
[JsonConverter(typeof(ModerationEntryConverter))]
public class GreifingReport : Report
{
    [JsonPropertyName("structure_damage")]
    public StructureDamageRecord[] DamageRecord { get; set; } = Array.Empty<StructureDamageRecord>();

    [JsonPropertyName("vehicle_requests")]
    public VehicleRequestRecord[] VehicleRequestRecord { get; set; } = Array.Empty<VehicleRequestRecord>();

    [JsonPropertyName("teamkills")]
    public TeamkillRecord[] TeamkillRecord { get; set; } = Array.Empty<TeamkillRecord>();

    [JsonPropertyName("vehicle_teamkills")]
    public VehicleTeamkillRecord[] VehicleTeamkillRecord { get; set; } = Array.Empty<VehicleTeamkillRecord>();
    public override string GetDisplayName() => "Greifing Report";
    protected override void ReadIntl(ByteReader reader, ushort version)
    {
        base.ReadIntl(reader, version);

        DamageRecord = new StructureDamageRecord[reader.ReadInt32()];
        for (int i = 0; i < DamageRecord.Length; ++i)
            DamageRecord[i] = new StructureDamageRecord(reader);

        VehicleRequestRecord = new VehicleRequestRecord[reader.ReadInt32()];
        for (int i = 0; i < VehicleRequestRecord.Length; ++i)
            VehicleRequestRecord[i] = new VehicleRequestRecord(reader);

        TeamkillRecord = new TeamkillRecord[reader.ReadInt32()];
        for (int i = 0; i < TeamkillRecord.Length; ++i)
            TeamkillRecord[i] = new TeamkillRecord(reader);

        VehicleTeamkillRecord = new VehicleTeamkillRecord[reader.ReadInt32()];
        for (int i = 0; i < VehicleTeamkillRecord.Length; ++i)
            VehicleTeamkillRecord[i] = new VehicleTeamkillRecord(reader);
    }

    protected override void WriteIntl(ByteWriter writer)
    {
        base.WriteIntl(writer);

        writer.Write(DamageRecord.Length);
        for (int i = 0; i < DamageRecord.Length; ++i)
            DamageRecord[i].Write(writer);

        writer.Write(VehicleRequestRecord.Length);
        for (int i = 0; i < VehicleRequestRecord.Length; ++i)
            VehicleRequestRecord[i].Write(writer);

        writer.Write(TeamkillRecord.Length);
        for (int i = 0; i < TeamkillRecord.Length; ++i)
            TeamkillRecord[i].Write(writer);

        writer.Write(VehicleTeamkillRecord.Length);
        for (int i = 0; i < VehicleTeamkillRecord.Length; ++i)
            VehicleTeamkillRecord[i].Write(writer);
    }
    public override void ReadProperty(ref Utf8JsonReader reader, string propertyName, JsonSerializerOptions options)
    {
        if (propertyName.Equals("structure_damage", StringComparison.InvariantCultureIgnoreCase))
            DamageRecord = JsonSerializer.Deserialize<StructureDamageRecord[]>(ref reader, options) ?? Array.Empty<StructureDamageRecord>();
        else if (propertyName.Equals("vehicle_requests", StringComparison.InvariantCultureIgnoreCase))
            VehicleRequestRecord = JsonSerializer.Deserialize<VehicleRequestRecord[]>(ref reader, options) ?? Array.Empty<VehicleRequestRecord>();
        else if (propertyName.Equals("teamkills", StringComparison.InvariantCultureIgnoreCase))
            TeamkillRecord = JsonSerializer.Deserialize<TeamkillRecord[]>(ref reader, options) ?? Array.Empty<TeamkillRecord>();
        else if (propertyName.Equals("vehicle_teamkills", StringComparison.InvariantCultureIgnoreCase))
            VehicleTeamkillRecord = JsonSerializer.Deserialize<VehicleTeamkillRecord[]>(ref reader, options) ?? Array.Empty<VehicleTeamkillRecord>();
        else
            base.ReadProperty(ref reader, propertyName, options);
    }
    public override void Write(Utf8JsonWriter writer, JsonSerializerOptions options)
    {
        base.Write(writer, options);

        writer.WritePropertyName("structure_damage");
        JsonSerializer.Serialize(writer, DamageRecord, options);

        writer.WritePropertyName("vehicle_requests");
        JsonSerializer.Serialize(writer, VehicleRequestRecord, options);

        writer.WritePropertyName("teamkills");
        JsonSerializer.Serialize(writer, TeamkillRecord, options);

        writer.WritePropertyName("vehicle_teamkills");
        JsonSerializer.Serialize(writer, VehicleTeamkillRecord, options);
    }

    internal override int EstimateColumnCount() => base.EstimateColumnCount() + 
                                                   DamageRecord.Length * 8 + VehicleRequestRecord.Length * 6 +
                                                   TeamkillRecord.Length * 5 + VehicleTeamkillRecord.Length * 4;
    internal override bool AppendWriteCall(StringBuilder builder, List<object> args)
    {
        bool hasEvidenceCalls = base.AppendWriteCall(builder, args);

        builder.Append($"DELETE FROM `{DatabaseInterface.TableReportStructureDamageRecords}` WHERE `{DatabaseInterface.ColumnExternalPrimaryKey}` = @0;");

        if (DamageRecord.Length > 0)
        {
            builder.Append($" INSERT INTO `{DatabaseInterface.TableReportStructureDamageRecords}` ({SqlTypes.ColumnList(
                DatabaseInterface.ColumnExternalPrimaryKey, DatabaseInterface.ColumnReportsStructureDamageStructure,
                DatabaseInterface.ColumnReportsStructureDamageStructureName, DatabaseInterface.ColumnReportsStructureDamageStructureOwner,
                DatabaseInterface.ColumnReportsStructureDamageStructureType, DatabaseInterface.ColumnReportsStructureDamageDamageOrigin,
                DatabaseInterface.ColumnReportsStructureDamageInstanceId, DatabaseInterface.ColumnReportsStructureDamageDamage,
                DatabaseInterface.ColumnReportsStructureDamageWasDestroyed)}) VALUES ");
            
            for (int i = 0; i < DamageRecord.Length; ++i)
            {
                ref StructureDamageRecord record = ref DamageRecord[i];
                F.AppendPropertyList(builder, args.Count, 8, i, 1);

                args.Add(record.Structure.ToString("N"));
                args.Add(record.Name.MaxLength(48) ?? string.Empty);
                args.Add(record.Owner);
                args.Add(record.Type.ToString());
                args.Add(record.Origin.ToString());
                args.Add(record.ID);
                args.Add(record.Damage);
                args.Add(record.Destroyed);
            }

            builder.Append(';');
        }

        builder.Append($"DELETE FROM `{DatabaseInterface.TableReportTeamkillRecords}` WHERE `{DatabaseInterface.ColumnExternalPrimaryKey}` = @0;");

        if (TeamkillRecord.Length > 0)
        {
            builder.Append($" INSERT INTO `{DatabaseInterface.TableReportTeamkillRecords}` ({SqlTypes.ColumnList(
                DatabaseInterface.ColumnExternalPrimaryKey, DatabaseInterface.ColumnReportsTeamkillRecordTeamkill,
                DatabaseInterface.ColumnReportsTeamkillRecordVictim, DatabaseInterface.ColumnReportsTeamkillRecordDeathCause,
                DatabaseInterface.ColumnReportsTeamkillRecordWasIntentional, DatabaseInterface.ColumnReportsTeamkillRecordMessage)}) VALUES ");

            for (int i = 0; i < TeamkillRecord.Length; ++i)
            {
                ref TeamkillRecord record = ref TeamkillRecord[i];
                F.AppendPropertyList(builder, args.Count, 5, i, 1);

                args.Add(record.Teamkill.IsValid ? record.Teamkill.Key : DBNull.Value);
                args.Add(record.Victim);
                args.Add(record.Cause.ToString());
                args.Add(record.Intentional.HasValue ? record.Intentional.Value : DBNull.Value);
                args.Add((object?)record.Message.MaxLength(255) ?? DBNull.Value);
            }

            builder.Append(';');
        }

        builder.Append($"DELETE FROM `{DatabaseInterface.TableReportVehicleTeamkillRecords}` WHERE `{DatabaseInterface.ColumnExternalPrimaryKey}` = @0;");

        if (VehicleTeamkillRecord.Length > 0)
        {
            builder.Append($" INSERT INTO `{DatabaseInterface.TableReportVehicleTeamkillRecords}` ({SqlTypes.ColumnList(
                DatabaseInterface.ColumnExternalPrimaryKey, DatabaseInterface.ColumnReportsVehicleTeamkillRecordTeamkill,
                DatabaseInterface.ColumnReportsVehicleTeamkillRecordDamageOrigin, DatabaseInterface.ColumnReportsVehicleTeamkillRecordVictim,
                DatabaseInterface.ColumnReportsVehicleTeamkillRecordMessage)}) VALUES ");

            for (int i = 0; i < VehicleTeamkillRecord.Length; ++i)
            {
                ref VehicleTeamkillRecord record = ref VehicleTeamkillRecord[i];
                F.AppendPropertyList(builder, args.Count, 4, i, 1);

                args.Add(record.Teamkill.IsValid ? record.Teamkill.Key : DBNull.Value);
                args.Add(record.Origin.ToString());
                args.Add(record.Victim);
                args.Add((object?)record.Message.MaxLength(255) ?? DBNull.Value);
            }

            builder.Append(';');
        }

        builder.Append($"DELETE FROM `{DatabaseInterface.TableReportVehicleRequestRecords}` WHERE `{DatabaseInterface.ColumnExternalPrimaryKey}` = @0;");

        if (VehicleRequestRecord.Length > 0)
        {
            builder.Append($" INSERT INTO `{DatabaseInterface.TableReportVehicleRequestRecords}` ({SqlTypes.ColumnList(
                DatabaseInterface.ColumnExternalPrimaryKey, DatabaseInterface.ColumnReportsVehicleRequestRecordVehicle,
                DatabaseInterface.ColumnReportsVehicleRequestRecordVehicleName, DatabaseInterface.ColumnReportsVehicleRequestRecordDamageOrigin,
                DatabaseInterface.ColumnReportsVehicleRequestRecordRequestTimestamp, DatabaseInterface.ColumnReportsVehicleRequestRecordDestroyTimestamp,
                DatabaseInterface.ColumnReportsVehicleRequestRecordInstigator)}) VALUES ");

            for (int i = 0; i < VehicleRequestRecord.Length; ++i)
            {
                ref VehicleRequestRecord record = ref VehicleRequestRecord[i];
                F.AppendPropertyList(builder, args.Count, 6, i, 1);

                args.Add(record.Vehicle.ToString("N"));
                args.Add(record.Name.MaxLength(48) ?? string.Empty);
                args.Add(record.Origin.ToString());
                args.Add(record.Requested.UtcDateTime);
                args.Add(record.Destroyed.HasValue ? record.Destroyed.Value.UtcDateTime : DBNull.Value);
                args.Add(Util.IsValidSteam64Id(record.Instigator) ? record.Instigator : DBNull.Value);
            }

            builder.Append(';');
        }

        return hasEvidenceCalls;
    }
}

public readonly struct StructureDamageRecord
{
    [JsonPropertyName("structure")]
    public Guid Structure { get; }

    [JsonPropertyName("name")]
    public string Name { get; }

    [JsonPropertyName("owner")]
    public ulong Owner { get; }

    [JsonPropertyName("origin")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EDamageOrigin Origin { get; }

    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public StructType Type { get; }

    [JsonPropertyName("id")]
    public uint ID { get; }

    [JsonPropertyName("damage")]
    public int Damage { get; }

    [JsonPropertyName("destroyed")]
    public bool Destroyed { get; }

    [JsonConstructor]
    public StructureDamageRecord(Guid structure, string name, ulong owner, EDamageOrigin origin, StructType type, uint id, int damage, bool destroyed)
    {
        Structure = structure;
        Name = name;
        Owner = owner;
        Origin = origin;
        Type = type;
        ID = id;
        Damage = damage;
        Destroyed = destroyed;
    }
    public StructureDamageRecord(ByteReader reader)
    {
        Structure = reader.ReadGuid();
        Name = reader.ReadString();
        Owner = reader.ReadUInt64();
        Origin = (EDamageOrigin)reader.ReadUInt16();
        Type = (StructType)reader.ReadUInt8();
        ID = reader.ReadUInt32();
        Damage = reader.ReadInt32();
        Destroyed = reader.ReadBool();
    }
    public void Write(ByteWriter writer)
    {
        writer.Write(Structure);
        writer.Write(Name);
        writer.Write(Owner);
        writer.Write((ushort)Origin);
        writer.Write((byte)Type);
        writer.Write(ID);
        writer.Write(Damage);
        writer.Write(Destroyed);
    }
}
public readonly struct TeamkillRecord
{
    [JsonPropertyName("teamkill")]
    public PrimaryKey Teamkill { get; }

    [JsonPropertyName("victim")]
    public ulong Victim { get; }

    [JsonPropertyName("cause")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EDeathCause Cause { get; }

    [JsonPropertyName("message")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Message { get; }

    [JsonPropertyName("intentional")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool? Intentional { get; }

    [JsonConstructor]
    public TeamkillRecord(PrimaryKey teamkill, ulong victim, EDeathCause cause, string message, bool? intentional)
    {
        Teamkill = teamkill;
        Victim = victim;
        Cause = cause;
        Message = message;
        Intentional = intentional;
    }
    public TeamkillRecord(ByteReader reader)
    {
        Teamkill = reader.ReadInt32();
        Victim = reader.ReadUInt64();
        Cause = (EDeathCause)reader.ReadUInt16();
        Message = reader.ReadNullableString();
        Intentional = reader.ReadNullableBool();
    }
    public void Write(ByteWriter writer)
    {
        writer.Write(Teamkill.Key);
        writer.Write(Victim);
        writer.Write((ushort)Cause);
        writer.WriteNullable(Message);
        writer.WriteNullable(Intentional);
    }
}
public readonly struct VehicleTeamkillRecord
{
    [JsonPropertyName("teamkill")]
    public PrimaryKey Teamkill { get; }

    [JsonPropertyName("victim")]
    public ulong Victim { get; }

    [JsonPropertyName("origin")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EDamageOrigin Origin { get; }

    [JsonPropertyName("message")]
    public string? Message { get; }

    [JsonConstructor]
    public VehicleTeamkillRecord(PrimaryKey teamkill, ulong victim, EDamageOrigin origin, string? message)
    {
        Teamkill = teamkill;
        Victim = victim;
        Origin = origin;
        Message = message;
    }
    public VehicleTeamkillRecord(ByteReader reader)
    {
        Teamkill = reader.ReadInt32();
        Victim = reader.ReadUInt64();
        Origin = (EDamageOrigin)reader.ReadUInt16();
        Message = reader.ReadNullableString();
    }
    public void Write(ByteWriter writer)
    {
        writer.Write(Teamkill.Key);
        writer.Write(Victim);
        writer.Write((ushort)Origin);
        writer.WriteNullable(Message);
    }
}
public readonly struct VehicleRequestRecord
{
    [JsonPropertyName("vehicle")]
    public Guid Vehicle { get; }

    [JsonPropertyName("name")]
    public string Name { get; }

    [JsonPropertyName("requested")]
    public DateTimeOffset Requested { get; }

    [JsonPropertyName("destroyed")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public DateTimeOffset? Destroyed { get; }

    [JsonPropertyName("origin")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EDamageOrigin Origin { get; }

    [JsonPropertyName("instigator")]
    public ulong Instigator { get; }
    public VehicleRequestRecord(Guid vehicle, string name, DateTimeOffset requested, DateTimeOffset? destroyed, EDamageOrigin origin, ulong instigator)
    {
        Vehicle = vehicle;
        Name = name;
        Requested = requested;
        Destroyed = destroyed;
        Origin = origin;
        Instigator = instigator;
    }
    public VehicleRequestRecord(ByteReader reader)
    {
        Vehicle = reader.ReadGuid();
        Name = reader.ReadString();
        Requested = reader.ReadDateTimeOffset();
        Destroyed = reader.ReadNullableDateTimeOffset();
        Origin = (EDamageOrigin)reader.ReadUInt16();
        Instigator = reader.ReadUInt64();
    }
    public void Write(ByteWriter writer)
    {
        writer.Write(Vehicle);
        writer.Write(Name);
        writer.Write(Requested);
        writer.WriteNullable(Destroyed);
        writer.Write((ushort)Origin);
        writer.Write(Instigator);
    }
}