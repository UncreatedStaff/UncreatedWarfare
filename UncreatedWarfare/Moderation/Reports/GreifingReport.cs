using SDG.Unturned;
using System;
using Uncreated.SQL;
using Uncreated.Warfare.Structures;

namespace Uncreated.Warfare.Moderation.Reports;

/*
 * Decided to remove all the random report reasons in place of a few small categories, mainly this one.
 * Should make it less complex for players.
 */
[ModerationEntry(ModerationEntryType.GreifingReport)]
public class GreifingReport : Report
{
    public StructureDamageRecord[] DamageRecord { get; set; } = Array.Empty<StructureDamageRecord>();
    public VehicleRequestRecord[] VehicleRequestRecord { get; set; } = Array.Empty<VehicleRequestRecord>();
    public TeamkillRecord[] TeamkillRecord { get; set; } = Array.Empty<TeamkillRecord>();
    public VehicleTeamkillRecord[] VehicleTeamkillRecord { get; set; } = Array.Empty<VehicleTeamkillRecord>();
    public override string GetDisplayName() => "Greifing Report";
}

public readonly struct StructureDamageRecord
{
    public Guid Structure { get; }
    public string StructureName { get; }
    public ulong StructureOwner { get; }
    public EDamageOrigin DamageOrigin { get; }
    public StructType StructureType { get; }
    public uint InstanceId { get; }
    public int TotalDamage { get; }
    public bool Destroyed { get; }
    public StructureDamageRecord(Guid structure, string structureName, ulong structureOwner, EDamageOrigin damageOrigin, StructType structureType, uint instanceId, int totalDamage, bool destroyed)
    {
        Structure = structure;
        StructureName = structureName;
        StructureOwner = structureOwner;
        DamageOrigin = damageOrigin;
        StructureType = structureType;
        InstanceId = instanceId;
        TotalDamage = totalDamage;
        Destroyed = destroyed;
    }
}
public readonly struct TeamkillRecord
{
    public PrimaryKey Entry { get; }
    public ulong Victim { get; }
    public EDeathCause Cause { get; }
    public string DeathMessage { get; }
    public bool? Intentional { get; }
    public TeamkillRecord(PrimaryKey entry, ulong victim, EDeathCause cause, string deathMessage, bool? intentional)
    {
        Entry = entry;
        Victim = victim;
        Cause = cause;
        DeathMessage = deathMessage;
        Intentional = intentional;
    }
}
public readonly struct VehicleTeamkillRecord
{
    public PrimaryKey Entry { get; }
    public ulong Victim { get; }
    public EDamageOrigin Origin { get; }
    public string DeathMessage { get; }
    public VehicleTeamkillRecord(PrimaryKey entry, ulong victim, EDamageOrigin origin, string deathMessage)
    {
        Entry = entry;
        Victim = victim;
        Origin = origin;
        DeathMessage = deathMessage;
    }
}
public readonly struct VehicleRequestRecord
{
    public Guid Vehicle { get; }
    public string VehicleName { get; }
    public DateTimeOffset RequestTime { get; }
    public DateTimeOffset? DestroyTime { get; }
    public EDamageOrigin DamageOrigin { get; }
    public ulong DamageInstigator { get; }
    public Guid DeathItem { get; }
    public string? DeathMessage { get; }
    public VehicleRequestRecord(Guid vehicle, string vehicleName, DateTimeOffset requestTime, DateTimeOffset? destroyTime, EDamageOrigin damageOrigin, ulong damageInstigator, Guid deathItem, string? deathMessage)
    {
        Vehicle = vehicle;
        VehicleName = vehicleName;
        RequestTime = requestTime;
        DestroyTime = destroyTime;
        DamageOrigin = damageOrigin;
        DamageInstigator = damageInstigator;
        DeathItem = deathItem;
        DeathMessage = deathMessage;
    }
}