using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Uncreated.SQL;

namespace Uncreated.Warfare.Moderation;

/// <summary>
/// Base class for a moderation record for a player. All punishments and commendations derive from this.
/// </summary>
public abstract class ModerationEntry
{
    /// <summary>
    /// Unique ID to all types of entries.
    /// </summary>
    public PrimaryKey Id { get; set; }

    /// <summary>
    /// Steam64 ID for the target player.
    /// </summary>
    public ulong Player { get; set; }

    /// <summary>
    /// Short message about the player.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Other related players, including admins.
    /// </summary>
    public RelatedActor[] Actors { get; set; } = Array.Empty<RelatedActor>();

    /// <summary>
    /// If the entry was from before the moderation rewrite.
    /// </summary>
    public bool IsLegacy { get; set; }

    /// <summary>
    /// When the entry was started, i.e. when an offense was reported.
    /// </summary>
    public DateTimeOffset StartedTimestamp { get; set; }

    /// <summary>
    /// When the entry was finished, i.e. when a punishment was handed out. <see langword="null"/> if the entry is still in progress.
    /// </summary>
    public DateTimeOffset? ResolvedTimestamp { get; set; }

    /// <summary>
    /// Effect this entry has on the player's reputation. Negative for punishments, positive for commendations.
    /// </summary>
    public double Reputation { get; set; }

    /// <summary>
    /// If this entry's reputation change has been applied.
    /// </summary>
    public bool ReputationApplied { get; set; }

    /// <summary>
    /// Unique legacy ID to only this type of entry. Only will exist when <see cref="IsLegacy"/> is <see langword="true"/>.
    /// </summary>
    public uint? LegacyId { get; set; }

    /// <summary>
    /// Start time of <see cref="ActionLog"/>s relevant to this entry.
    /// </summary>
    public DateTimeOffset? RelevantLogsStart { get; set; }

    /// <summary>
    /// End time of <see cref="ActionLog"/>s relevant to this entry.
    /// </summary>
    public DateTimeOffset? RelevantLogsEnd { get; set; }

    /// <summary>
    /// URL's to video/photo evidence.
    /// </summary>
    public Evidence[] Evidence { get; set; } = Array.Empty<Evidence>();

    /// <summary>
    /// Fills any cached properties.
    /// </summary>
    internal virtual Task FillDetail(DatabaseInterface db) => Task.CompletedTask;
    public virtual string GetDisplayName() => ToString();
}

public class ModerationCache : Dictionary<int, ModerationEntryCacheEntry>
{
    public ModerationCache() { }
    public ModerationCache(int capacity) : base(capacity) { }
    public new ModerationEntry this[int key]
    {
        get => base[key].Entry;
        set => base[key] = new ModerationEntryCacheEntry(value);
    }
    public void AddOrUpdate(ModerationEntry entry) => this[entry.Id.Key] = entry;
    public bool TryGet<T>(PrimaryKey key, out T value) where T : ModerationEntry
    {
        if (TryGetValue(key.Key, out ModerationEntryCacheEntry entry))
        {
            value = (entry.Entry as T)!;
            return value != null;
        }

        value = null!;
        return false;
    }
    public bool TryGet<T>(PrimaryKey key, out T value, TimeSpan timeout) where T : ModerationEntry
    {
        if (timeout.Ticks > 0 && TryGetValue(key.Key, out ModerationEntryCacheEntry entry))
        {
            value = (entry.Entry as T)!;
            return value != null && (DateTime.UtcNow - entry.LastRefreshed) < timeout;
        }

        value = null!;
        return false;
    }
}
public readonly struct ModerationEntryCacheEntry
{
    public ModerationEntry Entry { get; }
    public DateTime LastRefreshed { get; }
    public ModerationEntryCacheEntry(ModerationEntry entry) : this(entry, DateTime.UtcNow) { }
    public ModerationEntryCacheEntry(ModerationEntry entry, DateTime lastRefreshed)
    {
        Entry = entry;
        LastRefreshed = lastRefreshed;
    }
}

public enum ModerationEntryType : ushort
{
    None,
    Warning,
    Kick,
    Ban,
    Mute,
    [Translatable("Asset Ban")]
    AssetBan,
    Teamkill,
    [Translatable("Vehicle Teamkill")]
    VehicleTeamkill,
    [Translatable("BattlEye Kick")]
    BattlEyeKick,
    Appeal,
    Report,
    [Translatable("Greifing Report")]
    GreifingReport,
    [Translatable("Chat Abuse Report")]
    ChatAbuseReport,
    Note,
    Commendation,
    [Translatable("Bug Report Accepted")]
    BugReportAccepted,
    [Translatable("Player Report Accepted")]
    PlayerReportAccepted
}