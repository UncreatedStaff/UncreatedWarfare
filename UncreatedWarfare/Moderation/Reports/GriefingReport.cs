using DanielWillett.SpeedBytes;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Database.Manual;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Moderation.Reports;

/*
 * Decided to remove all the random report reasons in place of a few small categories, mainly this one.
 * Should make it less complex for players.
 */
[ModerationEntry(ModerationEntryType.GriefingReport)]
[JsonConverter(typeof(ModerationEntryConverter))]
public class GriefingReport : Report
{
    [JsonPropertyName("structure_damage")]
    public StructureDamageRecord[] DamageRecord { get; set; } = Array.Empty<StructureDamageRecord>();

    [JsonPropertyName("vehicle_requests")]
    public VehicleRequestRecord[] VehicleRequestRecord { get; set; } = Array.Empty<VehicleRequestRecord>();

    [JsonPropertyName("teamkills")]
    public TeamkillRecord[] TeamkillRecord { get; set; } = Array.Empty<TeamkillRecord>();

    [JsonPropertyName("vehicle_teamkills")]
    public VehicleTeamkillRecord[] VehicleTeamkillRecord { get; set; } = Array.Empty<VehicleTeamkillRecord>();
    public override string GetDisplayName() => "Griefing Report";
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
    public override bool ReadProperty(ref Utf8JsonReader reader, string propertyName, JsonSerializerOptions options)
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
            return base.ReadProperty(ref reader, propertyName, options);

        return true;
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

    internal override int EstimateParameterCount() => base.EstimateParameterCount() + 
                                                   DamageRecord.Length * 8 + VehicleRequestRecord.Length * 6 +
                                                   TeamkillRecord.Length * 5 + VehicleTeamkillRecord.Length * 4;
    public override async Task AddExtraInfo(DatabaseInterface db, List<string> workingList, IFormatProvider formatter, CancellationToken token = default)
    {
        await base.AddExtraInfo(db, workingList, formatter, token);
        int ttl = 0;
        for (int i = 0; i < DamageRecord.Length; ++i)
            ttl += DamageRecord[i].Damage;
        workingList.Add($"Recorded Structure Damage: {ttl.ToString(formatter)} dmg");
        workingList.Add($"Recorded Vehicle Requests: {VehicleRequestRecord.Length.ToString(formatter)}");
        workingList.Add($"Recorded Player Teamkills: {TeamkillRecord.Length.ToString(formatter)}");
        workingList.Add($"Recorded Vehicle Teamkills: {VehicleTeamkillRecord.Length.ToString(formatter)}");
    }
    internal override bool AppendWriteCall(StringBuilder builder, List<object> args)
    {
        bool hasEvidenceCalls = base.AppendWriteCall(builder, args);

        builder.Append($"DELETE FROM `{DatabaseInterface.TableReportStructureDamageRecords}` WHERE `{DatabaseInterface.ColumnExternalPrimaryKey}` = @0;");

        if (DamageRecord.Length > 0)
        {
            builder.Append($" INSERT INTO `{DatabaseInterface.TableReportStructureDamageRecords}` ({MySqlSnippets.ColumnList(
                DatabaseInterface.ColumnExternalPrimaryKey, DatabaseInterface.ColumnReportsStructureDamageStructure,
                DatabaseInterface.ColumnReportsStructureDamageStructureName, DatabaseInterface.ColumnReportsStructureDamageStructureOwner,
                DatabaseInterface.ColumnReportsStructureDamageStructureType, DatabaseInterface.ColumnReportsStructureDamageDamageOrigin,
                DatabaseInterface.ColumnReportsStructureDamageInstanceId, DatabaseInterface.ColumnReportsStructureDamageDamage,
                DatabaseInterface.ColumnReportsStructureDamageWasDestroyed, DatabaseInterface.ColumnReportsStructureDamageTimestamp)}) VALUES ");
            
            for (int i = 0; i < DamageRecord.Length; ++i)
            {
                ref StructureDamageRecord record = ref DamageRecord[i];
                MySqlSnippets.AppendPropertyList(builder, args.Count, 9, i, 1);

                args.Add(record.Structure.ToString("N"));
                args.Add(record.Name.Truncate(48) ?? string.Empty);
                args.Add(record.Owner);
                args.Add(record.IsStructure);
                args.Add(record.Origin.ToString());
                args.Add(record.ID);
                args.Add(record.Damage);
                args.Add(record.Destroyed);
                args.Add(record.Timestamp.UtcDateTime);
            }

            builder.Append(';');
        }

        builder.Append($"DELETE FROM `{DatabaseInterface.TableReportTeamkillRecords}` WHERE `{DatabaseInterface.ColumnExternalPrimaryKey}` = @0;");

        if (TeamkillRecord.Length > 0)
        {
            builder.Append($" INSERT INTO `{DatabaseInterface.TableReportTeamkillRecords}` ({MySqlSnippets.ColumnList(
                DatabaseInterface.ColumnExternalPrimaryKey, DatabaseInterface.ColumnReportsTeamkillRecordTeamkill,
                DatabaseInterface.ColumnReportsTeamkillRecordVictim, DatabaseInterface.ColumnReportsTeamkillRecordDeathCause,
                DatabaseInterface.ColumnReportsTeamkillRecordWasIntentional, DatabaseInterface.ColumnReportsTeamkillRecordMessage,
                DatabaseInterface.ColumnReportsTeamkillRecordTimestamp)}) VALUES ");

            for (int i = 0; i < TeamkillRecord.Length; ++i)
            {
                ref TeamkillRecord record = ref TeamkillRecord[i];
                MySqlSnippets.AppendPropertyList(builder, args.Count, 6, i, 1);

                args.Add(record.Teamkill != 0u ? record.Teamkill : DBNull.Value);
                args.Add(record.Victim);
                args.Add(record.Cause.ToString());
                args.Add(record.Intentional.HasValue ? record.Intentional.Value : DBNull.Value);
                args.Add((object?)record.Message.Truncate(255) ?? DBNull.Value);
                args.Add(record.Timestamp.UtcDateTime);
            }

            builder.Append(';');
        }

        builder.Append($"DELETE FROM `{DatabaseInterface.TableReportVehicleTeamkillRecords}` WHERE `{DatabaseInterface.ColumnExternalPrimaryKey}` = @0;");

        if (VehicleTeamkillRecord.Length > 0)
        {
            builder.Append($" INSERT INTO `{DatabaseInterface.TableReportVehicleTeamkillRecords}` ({MySqlSnippets.ColumnList(
                DatabaseInterface.ColumnExternalPrimaryKey, DatabaseInterface.ColumnReportsVehicleTeamkillRecordTeamkill,
                DatabaseInterface.ColumnReportsVehicleTeamkillRecordDamageOrigin, DatabaseInterface.ColumnReportsVehicleTeamkillRecordVictim,
                DatabaseInterface.ColumnReportsVehicleTeamkillRecordMessage, DatabaseInterface.ColumnReportsVehicleTeamkillRecordTimestamp)}) VALUES ");

            for (int i = 0; i < VehicleTeamkillRecord.Length; ++i)
            {
                ref VehicleTeamkillRecord record = ref VehicleTeamkillRecord[i];
                MySqlSnippets.AppendPropertyList(builder, args.Count, 5, i, 1);

                args.Add(record.Teamkill != 0u ? record.Teamkill : DBNull.Value);
                args.Add(record.Origin.ToString());
                args.Add(record.Victim);
                args.Add((object?)record.Message.Truncate(255) ?? DBNull.Value);
                args.Add(record.Timestamp.UtcDateTime);
            }

            builder.Append(';');
        }

        builder.Append($"DELETE FROM `{DatabaseInterface.TableReportVehicleRequestRecords}` WHERE `{DatabaseInterface.ColumnExternalPrimaryKey}` = @0;");

        if (VehicleRequestRecord.Length > 0)
        {
            builder.Append($" INSERT INTO `{DatabaseInterface.TableReportVehicleRequestRecords}` ({MySqlSnippets.ColumnList(
                DatabaseInterface.ColumnExternalPrimaryKey, DatabaseInterface.ColumnReportsVehicleRequestRecordVehicle,
                DatabaseInterface.ColumnReportsVehicleRequestRecordVehicleName, DatabaseInterface.ColumnReportsVehicleRequestRecordDamageOrigin,
                DatabaseInterface.ColumnReportsVehicleRequestRecordRequestTimestamp, DatabaseInterface.ColumnReportsVehicleRequestRecordDestroyTimestamp,
                DatabaseInterface.ColumnReportsVehicleRequestRecordInstigator)}) VALUES ");

            for (int i = 0; i < VehicleRequestRecord.Length; ++i)
            {
                ref VehicleRequestRecord record = ref VehicleRequestRecord[i];
                MySqlSnippets.AppendPropertyList(builder, args.Count, 6, i, 1);

                args.Add(record.Vehicle.ToString("N"));
                args.Add(record.Name.Truncate(48) ?? string.Empty);
                args.Add(record.Origin.ToString());
                args.Add(record.Timestamp.UtcDateTime);
                args.Add(record.Destroyed.HasValue ? record.Destroyed.Value.UtcDateTime : DBNull.Value);
                args.Add(new CSteamID(record.Instigator).GetEAccountType() == EAccountType.k_EAccountTypeIndividual ? record.Instigator : DBNull.Value);
            }

            builder.Append(';');
        }

        return hasEvidenceCalls;
    }
}

public struct StructureDamageRecord
{
    [JsonPropertyName("structure")]
    public Guid Structure { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("owner")]
    public ulong Owner { get; set; }

    [JsonPropertyName("origin")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EDamageOrigin Origin { get; set; }

    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public bool IsStructure { get; set; }

    [JsonPropertyName("id")]
    public uint ID { get; set; }

    [JsonPropertyName("damage")]
    public int Damage { get; set; }

    [JsonPropertyName("destroyed")]
    public bool Destroyed { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; }
    
    public StructureDamageRecord() { }
    public StructureDamageRecord(Guid structure, string name, ulong owner, EDamageOrigin origin, bool isStructure, uint id, int damage, bool destroyed, DateTimeOffset timestamp)
    {
        Structure = structure;
        Name = name;
        Owner = owner;
        Origin = origin;
        IsStructure = isStructure;
        ID = id;
        Damage = damage;
        Destroyed = destroyed;
        Timestamp = timestamp;
    }
    public StructureDamageRecord(ByteReader reader)
    {
        Structure = reader.ReadGuid();
        Name = reader.ReadString();
        Owner = reader.ReadUInt64();
        Origin = (EDamageOrigin)reader.ReadUInt16();
        IsStructure = reader.ReadBool();
        ID = reader.ReadUInt32();
        Damage = reader.ReadInt32();
        Destroyed = reader.ReadBool();
        Timestamp = reader.ReadDateTimeOffset();
    }
    public void Write(ByteWriter writer)
    {
        writer.Write(Structure);
        writer.Write(Name);
        writer.Write(Owner);
        writer.Write((ushort)Origin);
        writer.Write(IsStructure);
        writer.Write(ID);
        writer.Write(Damage);
        writer.Write(Destroyed);
        writer.Write(Timestamp);
    }
}
public struct TeamkillRecord
{
    [JsonPropertyName("teamkill")]
    public uint Teamkill { get; set; }

    [JsonPropertyName("victim")]
    public ulong Victim { get; set; }

    [JsonPropertyName("cause")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EDeathCause Cause { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("intentional")]
    public bool? Intentional { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; }

    public TeamkillRecord() { }
    public TeamkillRecord(uint teamkill, ulong victim, EDeathCause cause, string message, bool? intentional, DateTimeOffset timestamp)
    {
        Teamkill = teamkill;
        Victim = victim;
        Cause = cause;
        Message = message;
        Intentional = intentional;
        Timestamp = timestamp;
    }
    public TeamkillRecord(ByteReader reader)
    {
        Teamkill = reader.ReadUInt32();
        Victim = reader.ReadUInt64();
        Cause = (EDeathCause)reader.ReadUInt16();
        Message = reader.ReadNullableString();
        Intentional = reader.ReadNullableBool();
        Timestamp = reader.ReadDateTimeOffset();
    }
    public void Write(ByteWriter writer)
    {
        writer.Write(Teamkill);
        writer.Write(Victim);
        writer.Write((ushort)Cause);
        writer.WriteNullable(Message);
        writer.WriteNullable(Intentional);
        writer.Write(Timestamp);
    }
}
public struct VehicleTeamkillRecord
{
    [JsonPropertyName("teamkill")]
    public uint Teamkill { get; set; }

    [JsonPropertyName("victim")]
    public ulong Victim { get; set; }

    [JsonPropertyName("origin")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EDamageOrigin Origin { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; }

    public VehicleTeamkillRecord() { }
    public VehicleTeamkillRecord(uint teamkill, ulong victim, EDamageOrigin origin, string? message, DateTimeOffset timestamp)
    {
        Teamkill = teamkill;
        Victim = victim;
        Origin = origin;
        Message = message;
        Timestamp = timestamp;
    }
    public VehicleTeamkillRecord(ByteReader reader)
    {
        Teamkill = reader.ReadUInt32();
        Victim = reader.ReadUInt64();
        Origin = (EDamageOrigin)reader.ReadUInt16();
        Message = reader.ReadNullableString();
        Timestamp = reader.ReadDateTimeOffset();
    }
    public void Write(ByteWriter writer)
    {
        writer.Write(Teamkill);
        writer.Write(Victim);
        writer.Write((ushort)Origin);
        writer.WriteNullable(Message);
        writer.Write(Timestamp);
    }
}
public struct VehicleRequestRecord
{
    [JsonPropertyName("vehicle")]
    public Guid Vehicle { get; set; }

    [JsonPropertyName("vehicle_bay_id")]
    public uint Asset { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; }

    [JsonPropertyName("destroyed")]
    public DateTimeOffset? Destroyed { get; set; }

    [JsonPropertyName("origin")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EDamageOrigin Origin { get; set; }

    [JsonPropertyName("instigator")]
    public ulong Instigator { get; set; }
    public VehicleRequestRecord() { }
    public VehicleRequestRecord(Guid vehicle, uint asset, string name, DateTimeOffset timestamp, DateTimeOffset? destroyed, EDamageOrigin origin, ulong instigator)
    {
        Vehicle = vehicle;
        Asset = asset;
        Name = name;
        Timestamp = timestamp;
        Destroyed = destroyed;
        Origin = origin;
        Instigator = instigator;
    }
    public VehicleRequestRecord(ByteReader reader)
    {
        Vehicle = reader.ReadGuid();
        Name = reader.ReadString();
        Timestamp = reader.ReadDateTimeOffset();
        Destroyed = reader.ReadNullableDateTimeOffset();
        Origin = (EDamageOrigin)reader.ReadUInt16();
        Instigator = reader.ReadUInt64();
    }
    public void Write(ByteWriter writer)
    {
        writer.Write(Vehicle);
        writer.Write(Name);
        writer.Write(Timestamp);
        writer.WriteNullable(Destroyed);
        writer.Write((ushort)Origin);
        writer.Write(Instigator);
    }
}