using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Encoding;
using Uncreated.Framework;
using Uncreated.SQL;

namespace Uncreated.Warfare.Moderation;

/// <summary>
/// Base class for a moderation record for a player. All punishments and commendations derive from this.
/// </summary>
[JsonConverter(typeof(ModerationEntryConverter))]
public abstract class ModerationEntry : IModerationEntry
{
    private const ushort DataVersion = 0;
    public static readonly ModerationEntryType MaxEntry = ModerationEntryType.PlayerReportAccepted;

    [JsonIgnore]
    public virtual bool IsAppealable => false;

    /// <inheritdoc/>
    [JsonPropertyName("id")]
    public PrimaryKey Id { get; set; } = PrimaryKey.NotAssigned;

    /// <inheritdoc/>
    [JsonPropertyName("target_steam_64")]
    public ulong Player { get; set; }

    /// <inheritdoc/>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <inheritdoc/>
    [JsonPropertyName("actors")]
    public RelatedActor[] Actors { get; set; } = Array.Empty<RelatedActor>();

    /// <inheritdoc/>
    [JsonPropertyName("is_legacy")]
    public bool IsLegacy { get; set; }

    /// <inheritdoc/>
    [JsonPropertyName("started_utc")]
    public DateTimeOffset StartedTimestamp { get; set; }

    /// <inheritdoc/>
    [JsonPropertyName("resolved_utc")]
    public DateTimeOffset? ResolvedTimestamp { get; set; }

    /// <inheritdoc/>
    [JsonPropertyName("reputation")]
    public double Reputation { get; set; }

    /// <inheritdoc/>
    [JsonPropertyName("pending_reputation")]
    public double PendingReputation { get; set; }

    /// <inheritdoc/>
    [JsonPropertyName("legacy_id")]
    public uint? LegacyId { get; set; }

    /// <inheritdoc/>
    [JsonPropertyName("relevant_logs_begin_utc")]
    public DateTimeOffset? RelevantLogsBegin { get; set; }

    /// <inheritdoc/>
    [JsonPropertyName("relevant_logs_end_utc")]
    public DateTimeOffset? RelevantLogsEnd { get; set; }

    /// <inheritdoc/>
    [JsonPropertyName("evidence")]
    public Evidence[] Evidence { get; set; } = Array.Empty<Evidence>();

    /// <inheritdoc/>
    [JsonPropertyName("is_removed")]
    public bool Removed { get; set; }

    /// <inheritdoc/>
    [JsonPropertyName("removing_actor")]
    [JsonConverter(typeof(ActorConverter))]
    public IModerationActor? RemovedBy { get; set; }

    /// <inheritdoc/>
    [JsonPropertyName("removed_timestamp_utc")]
    public DateTimeOffset? RemovedTimestamp { get; set; }

    /// <inheritdoc/>
    [JsonPropertyName("removed_message")]
    public string? RemovedMessage { get; set; }

    /// <inheritdoc/>
    [JsonPropertyName("related_entries")]
    public PrimaryKey[] RelatedEntryKeys { get; set; } = Array.Empty<PrimaryKey>();

    /// <inheritdoc/>
    [JsonIgnore]
    public ModerationEntry?[]? RelatedEntries { get; set; }

    /// <summary>
    /// Fills any cached properties.
    /// </summary>
    internal virtual Task FillDetail(DatabaseInterface db, CancellationToken token = default)
    {
        if (RelatedEntries == null || RelatedEntries.Length != RelatedEntryKeys.Length)
            RelatedEntries = new ModerationEntry?[RelatedEntryKeys.Length];

        return db.ReadAll(RelatedEntries, RelatedEntryKeys, true, true, false, token);
    }
    public virtual string GetDisplayName() => ToString();
    public virtual string? GetDisplayMessage() => Message;
    public virtual Guid? GetIcon() => null;
    public virtual async Task AddExtraInfo(DatabaseInterface db, List<string> workingList, IFormatProvider formatter, CancellationToken token = default)
    {
        workingList.Add($"Entry ID: {Id.Key.ToString(formatter)}");
        if (IsLegacy)
        {
            workingList.Add($"Legacy ID: {(LegacyId.HasValue ? LegacyId.Value.ToString(formatter) : "--")}");
        }

        if (PendingReputation != 0d)
        {
            workingList.Add($"Pending Reptuation: {PendingReputation.ToString("0.#", CultureInfo.InvariantCulture)}");
        }

        if (Removed)
        {
            if (RemovedBy != null)
            {
                string disp = await RemovedBy.GetDisplayName(db, token).ConfigureAwait(false) + " (" + RemovedBy.Id.ToString(CultureInfo.InvariantCulture) + ")";
                if (RemovedTimestamp.HasValue)
                    workingList.Add($"Removed By: {disp} @ {RemovedTimestamp.Value.UtcDateTime.ToString(ModerationUI.DateTimeFormat, formatter)}");
                else
                    workingList.Add($"Removed By: {disp}");
            }
            else
            {
                if (RemovedTimestamp.HasValue)
                    workingList.Add($"Removed @ {RemovedTimestamp.Value.UtcDateTime.ToString(ModerationUI.DateTimeFormat, formatter)}");
                else
                    workingList.Add("Removed");
            }
            if (RemovedMessage != null)
            {
                workingList.Add("For: \"" + RemovedMessage.MaxLength(64) + "\"");
            }
        }

        if (RelevantLogsEnd.HasValue)
        {
            if (RelevantLogsBegin.HasValue)
                workingList.Add($"Relevant Logs: {RelevantLogsBegin.Value.UtcDateTime.ToString(ModerationUI.DateTimeFormat, formatter)} to {RelevantLogsEnd.Value.UtcDateTime.ToString(ModerationUI.DateTimeFormat, formatter)}");
            else
                workingList.Add($"Relevant Log: {RelevantLogsEnd.Value.UtcDateTime.ToString(ModerationUI.DateTimeFormat, formatter)}");
        }
        else if (RelevantLogsBegin.HasValue)
            workingList.Add($"Relevant Log: {RelevantLogsBegin.Value.UtcDateTime.ToString(ModerationUI.DateTimeFormat, formatter)}");
    }

    public virtual bool TryGetDisplayActor(out RelatedActor actor) => TryGetPrimaryAdmin(out actor);
    public bool TryGetPrimaryAdmin(out RelatedActor actor)
    {
        for (int i = 0; i < Actors.Length; ++i)
        {
            if (string.Equals(Actors[i].Role, RelatedActor.RolePrimaryAdmin, StringComparison.OrdinalIgnoreCase))
            {
                actor = Actors[i];
                return true;
            }
        }

        actor = new RelatedActor(RelatedActor.RolePrimaryAdmin, true, ConsoleActor.Instance);
        return false;
    }
    public bool TryGetActor(string role, out RelatedActor actor)
    {
        for (int i = Actors.Length - 1; i >= 0; --i)
        {
            if (string.Equals(Actors[i].Role, role, StringComparison.OrdinalIgnoreCase))
            {
                actor = Actors[i];
                return true;
            }
        }

        actor = new RelatedActor(role, false, ConsoleActor.Instance);
        return false;
    }
    public virtual void ReadProperty(ref Utf8JsonReader reader, string propertyName, JsonSerializerOptions options)
    {
        if (propertyName.Equals("id", StringComparison.InvariantCultureIgnoreCase))
            Id = reader.GetUInt32();
        else if (propertyName.Equals("target_steam_64", StringComparison.InvariantCultureIgnoreCase))
            Player = reader.GetUInt64();
        else if (propertyName.Equals("message", StringComparison.InvariantCultureIgnoreCase))
            Message = reader.GetString();
        else if (propertyName.Equals("is_legacy", StringComparison.InvariantCultureIgnoreCase))
            IsLegacy = reader.TokenType != JsonTokenType.Null && reader.GetBoolean();
        else if (propertyName.Equals("started_utc", StringComparison.InvariantCultureIgnoreCase))
            StartedTimestamp = new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(), DateTimeKind.Utc));
        else if (propertyName.Equals("resolved_utc", StringComparison.InvariantCultureIgnoreCase))
            ResolvedTimestamp = reader.TokenType == JsonTokenType.Null ? null : new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(), DateTimeKind.Utc));
        else if (propertyName.Equals("reputation", StringComparison.InvariantCultureIgnoreCase))
            Reputation = reader.TokenType == JsonTokenType.Null ? 0d : reader.GetDouble();
        else if (propertyName.Equals("pending_reputation", StringComparison.InvariantCultureIgnoreCase))
            PendingReputation = reader.TokenType == JsonTokenType.Null ? 0d : reader.GetDouble();
        else if (propertyName.Equals("legacy_id", StringComparison.InvariantCultureIgnoreCase))
            LegacyId = reader.TokenType == JsonTokenType.Null ? null : reader.GetUInt32();
        else if (propertyName.Equals("relevant_logs_begin_utc", StringComparison.InvariantCultureIgnoreCase))
            RelevantLogsBegin = reader.TokenType == JsonTokenType.Null ? null : new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(), DateTimeKind.Utc));
        else if (propertyName.Equals("relevant_logs_end_utc", StringComparison.InvariantCultureIgnoreCase))
            RelevantLogsEnd = reader.TokenType == JsonTokenType.Null ? null : new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(), DateTimeKind.Utc));
        else if (propertyName.Equals("actors", StringComparison.InvariantCultureIgnoreCase))
            Actors = reader.TokenType == JsonTokenType.Null ? Array.Empty<RelatedActor>() : JsonSerializer.Deserialize<RelatedActor[]>(ref reader, options) ?? Array.Empty<RelatedActor>();
        else if (propertyName.Equals("evidence", StringComparison.InvariantCultureIgnoreCase))
            Evidence = reader.TokenType == JsonTokenType.Null ? Array.Empty<Evidence>() : JsonSerializer.Deserialize<Evidence[]>(ref reader, options) ?? Array.Empty<Evidence>();
        else if (propertyName.Equals("is_removed", StringComparison.InvariantCultureIgnoreCase))
            Removed = reader.TokenType != JsonTokenType.Null && reader.GetBoolean();
        else if (propertyName.Equals("removing_actor", StringComparison.InvariantCultureIgnoreCase))
            RemovedBy = reader.TokenType == JsonTokenType.Null ? null : Moderation.Actors.GetActor(reader.GetUInt64());
        else if (propertyName.Equals("removed_timestamp_utc", StringComparison.InvariantCultureIgnoreCase))
            RemovedTimestamp = reader.TokenType == JsonTokenType.Null ? null : new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(), DateTimeKind.Utc));
        else if (propertyName.Equals("removed_message", StringComparison.InvariantCultureIgnoreCase))
            RemovedMessage = reader.GetString();
    }
    public virtual void Write(Utf8JsonWriter writer, JsonSerializerOptions options)
    {
        writer.WriteNumber("id", Id.Key);
        writer.WriteNumber("target_steam_64", Player);
        writer.WriteString("message", Message);

        if (IsLegacy)
            writer.WriteBoolean("is_legacy", true);

        writer.WriteString("started_utc", StartedTimestamp.UtcDateTime);
        if (ResolvedTimestamp.HasValue)
            writer.WriteString("resolved_utc", ResolvedTimestamp.Value.UtcDateTime);
        writer.WriteNumber("reputation", Reputation);
        writer.WriteNumber("pending_reputation", PendingReputation);
        if (LegacyId.HasValue)
            writer.WriteNumber("legacy_id", LegacyId.Value);
        if (RelevantLogsBegin.HasValue)
            writer.WriteString("relevant_logs_begin_utc", RelevantLogsBegin.Value.UtcDateTime);
        if (RelevantLogsEnd.HasValue)
            writer.WriteString("relevant_logs_end_utc", RelevantLogsEnd.Value.UtcDateTime);

        writer.WritePropertyName("actors");
        JsonSerializer.Serialize(writer, Actors, options);

        writer.WritePropertyName("evidence");
        JsonSerializer.Serialize(writer, Evidence, options);

        writer.WriteBoolean("is_removed", Removed);
        if (Removed)
        {
            writer.WriteNumber("removing_actor", RemovedBy == null ? 0ul : RemovedBy.Id);
            if (RemovedTimestamp.HasValue)
                writer.WriteString("removed_timestamp_utc", RemovedTimestamp.Value.UtcDateTime);

            writer.WriteString("removed_message", RemovedMessage);
        }
        
        writer.WritePropertyName("related_entries");
        JsonSerializer.Serialize(writer, RelatedEntryKeys, options);
    }

    protected virtual void ReadIntl(ByteReader reader, ushort version) { }
    protected virtual void WriteIntl(ByteWriter writer) { }
    public static void Write(ByteWriter writer, ModerationEntry entry)
    {
        ModerationEntryType type = ModerationReflection.GetType(entry.GetType()) ?? throw new Exception($"Unrecognized moderation entry type: {entry.GetType().Name}.");
        writer.Write(type);
        entry.WriteContent(writer);
    }
    public static ModerationEntry Read(ByteReader reader)
    {
        ModerationEntryType type = reader.ReadEnum<ModerationEntryType>();
        Type entryType = ModerationReflection.GetType(type) ?? throw new Exception($"Unrecognized moderation entry type: {type}.");
        ModerationEntry entry = (ModerationEntry)Activator.CreateInstance(entryType);
        entry.ReadContent(reader);
        return entry;
    }
    internal void ReadContent(ByteReader reader)
    {
        ushort version = reader.ReadUInt16();

        Id = reader.ReadUInt32();
        Player = reader.ReadUInt64();
        Message = reader.ReadNullableString();
        byte flag = reader.ReadUInt8();
        IsLegacy = (flag & 1) != 0;
        StartedTimestamp = reader.ReadDateTimeOffset();
        ResolvedTimestamp = reader.ReadNullableDateTimeOffset();
        Reputation = reader.ReadDouble();
        PendingReputation = reader.ReadDouble();
        LegacyId = reader.ReadNullableUInt32();
        RelevantLogsBegin = reader.ReadNullableDateTimeOffset();
        RelevantLogsEnd = reader.ReadNullableDateTimeOffset();
        int ct = reader.ReadInt32();
        Actors = ct == 0 ? Array.Empty<RelatedActor>() : new RelatedActor[ct];
        for (int i = 0; i < Actors.Length; ++i)
            Actors[i] = new RelatedActor(reader, version);
        ct = reader.ReadInt32();
        Evidence = ct == 0 ? Array.Empty<Evidence>() : new Evidence[ct];
        for (int i = 0; i < Evidence.Length; ++i)
            Evidence[i] = new Evidence(reader, version);

        Removed = (flag & 2) != 0;
        if (Removed)
        {
            RemovedBy = Moderation.Actors.GetActor(reader.ReadUInt64());
            RemovedTimestamp = reader.ReadNullableDateTimeOffset();
            RemovedMessage = reader.ReadNullableString();
        }

        ReadIntl(reader, version);
    }
    internal void WriteContent(ByteWriter writer)
    {
        writer.Write(DataVersion);

        writer.Write(Id.Key);
        writer.Write(Player);
        writer.WriteNullable(Message);
        byte flag = (byte)((IsLegacy ? 1 : 0) | (Removed ? 2 : 0));
        writer.Write(flag);
        writer.Write(StartedTimestamp);
        writer.WriteNullable(ResolvedTimestamp);
        writer.Write(Reputation);
        writer.Write(PendingReputation);
        writer.WriteNullable(LegacyId);
        writer.WriteNullable(RelevantLogsBegin);
        writer.WriteNullable(RelevantLogsEnd);

        writer.Write(Actors.Length);
        for (int i = 0; i < Actors.Length; ++i)
            Actors[i].Write(writer);

        writer.Write(Evidence.Length);
        for (int i = 0; i < Evidence.Length; ++i)
            Evidence[i].Write(writer);

        if (Removed)
        {
            writer.Write(RemovedBy == null ? 0ul : RemovedBy.Id);
            writer.WriteNullable(RemovedTimestamp);
            writer.WriteNullable(RemovedMessage);
        }

        WriteIntl(writer);
    }

    internal virtual int EstimateParameterCount() => 1 + Actors.Length * 4 + Evidence.Length * 7;
    internal virtual bool AppendWriteCall(StringBuilder builder, List<object> args)
    {
        builder.Append($"DELETE FROM `{DatabaseInterface.TableActors}` WHERE `{DatabaseInterface.ColumnExternalPrimaryKey}` = @0;");
        if (Actors is { Length: > 0 })
        {
            builder.Append($" INSERT INTO `{DatabaseInterface.TableActors}` ({SqlTypes.ColumnList(
                DatabaseInterface.ColumnExternalPrimaryKey, DatabaseInterface.ColumnActorsIndex,
                DatabaseInterface.ColumnActorsId, DatabaseInterface.ColumnActorsRole, DatabaseInterface.ColumnActorsAsAdmin)}) VALUES ");
            
            for (int i = 0; i < Actors.Length; ++i)
            {
                ref RelatedActor actor = ref Actors[i];

                F.AppendPropertyList(builder, args.Count, 4, i, 1);

                args.Add(i);
                args.Add(actor.Actor.Id);
                args.Add(actor.Role.MaxLength(255) ?? string.Empty);
                args.Add(actor.Admin);
            }
            builder.Append(';');
        }

        if (Evidence.Length == 0)
            builder.Append($"DELETE FROM `{DatabaseInterface.TableEvidence}` WHERE `{DatabaseInterface.ColumnExternalPrimaryKey}` = @0;");

        bool anyNew = false;
        if (Evidence is { Length: > 0 })
        {
            bool anyOld = false;
            for (int i = 0; i < Evidence.Length; ++i)
            {
                ref Evidence evidence = ref Evidence[i];
                if (evidence.Id.IsValid)
                    anyOld = true;
                else
                    anyNew = true;
            }

            if (anyOld)
            {
                builder.Append($" INSERT INTO `{DatabaseInterface.TableEvidence}` ({SqlTypes.ColumnList(
                    DatabaseInterface.ColumnExternalPrimaryKey, DatabaseInterface.ColumnEvidenceId,
                    DatabaseInterface.ColumnEvidenceActorId, DatabaseInterface.ColumnEvidenceIsImage,
                    DatabaseInterface.ColumnEvidenceLink, DatabaseInterface.ColumnEvidenceLocalSource,
                    DatabaseInterface.ColumnEvidenceMessage, DatabaseInterface.ColumnEvidenceTimestamp)}) VALUES ");

                for (int i = 0; i < Evidence.Length; ++i)
                {
                    ref Evidence evidence = ref Evidence[i];
                    if (!evidence.Id.IsValid)
                        continue;

                    F.AppendPropertyList(builder, args.Count, 7, i, 1);

                    args.Add(evidence.Id.Key);
                    args.Add(evidence.Actor == null ? DBNull.Value : evidence.Actor.Id);
                    args.Add(evidence.Image);
                    args.Add(evidence.URL.MaxLength(512)!);
                    args.Add((object?)evidence.SavedLocation.MaxLength(512) ?? DBNull.Value);
                    args.Add((object?)evidence.Message.MaxLength(1024) ?? DBNull.Value);
                    args.Add(evidence.Timestamp.UtcDateTime);
                }

                builder.Append($" AS `t` ON DUPLICATE KEY UPDATE " +
                               $"`{DatabaseInterface.ColumnExternalPrimaryKey}` = `t`.`{DatabaseInterface.ColumnExternalPrimaryKey}`," +
                               $"`{DatabaseInterface.ColumnEvidenceActorId}` = `t`.`{DatabaseInterface.ColumnEvidenceActorId}`," +
                               $"`{DatabaseInterface.ColumnEvidenceIsImage}` = `t`.`{DatabaseInterface.ColumnEvidenceIsImage}`," +
                               $"`{DatabaseInterface.ColumnEvidenceLink}` = `t`.`{DatabaseInterface.ColumnEvidenceLink}`," +
                               $"`{DatabaseInterface.ColumnEvidenceLocalSource}` = `t`.`{DatabaseInterface.ColumnEvidenceLocalSource}`," +
                               $"`{DatabaseInterface.ColumnEvidenceMessage}` = `t`.`{DatabaseInterface.ColumnEvidenceMessage}`," +
                               $"`{DatabaseInterface.ColumnEvidenceTimestamp}` = `t`.`{DatabaseInterface.ColumnEvidenceTimestamp}`;");
            }

            if (anyNew)
            {
                builder.Append($" INSERT INTO `{DatabaseInterface.TableEvidence}` ({SqlTypes.ColumnList(
                    DatabaseInterface.ColumnExternalPrimaryKey, DatabaseInterface.ColumnEvidenceActorId,
                    DatabaseInterface.ColumnEvidenceIsImage, DatabaseInterface.ColumnEvidenceLink,
                    DatabaseInterface.ColumnEvidenceLocalSource, DatabaseInterface.ColumnEvidenceMessage,
                    DatabaseInterface.ColumnEvidenceTimestamp)}) VALUES ");

                for (int i = 0; i < Evidence.Length; ++i)
                {
                    ref Evidence evidence = ref Evidence[i];
                    if (evidence.Id.IsValid)
                        continue;

                    F.AppendPropertyList(builder, args.Count, 6, i, 1);

                    args.Add(evidence.Actor == null ? DBNull.Value : evidence.Actor.Id);
                    args.Add(evidence.Image);
                    args.Add(evidence.URL.MaxLength(512)!);
                    args.Add((object?)evidence.SavedLocation?.MaxLength(512) ?? DBNull.Value);
                    args.Add((object?)evidence.Message?.MaxLength(1024) ?? DBNull.Value);
                    args.Add(evidence.Timestamp.UtcDateTime);
                }

                builder.Append($"; SELECT {SqlTypes.ColumnList(DatabaseInterface.ColumnEvidenceId,
                    DatabaseInterface.ColumnEvidenceLink, DatabaseInterface.ColumnEvidenceMessage,
                    DatabaseInterface.ColumnEvidenceLocalSource, DatabaseInterface.ColumnEvidenceIsImage,
                    DatabaseInterface.ColumnEvidenceTimestamp, DatabaseInterface.ColumnEvidenceActorId)} FROM `{DatabaseInterface.TableEvidence}` WHERE `{DatabaseInterface.ColumnExternalPrimaryKey}` = @0;");
            }
        }

        return anyNew;
    }
}
public interface IModerationEntry
{
    /// <summary>
    /// Unique ID to all types of entries.
    /// </summary>
    [JsonPropertyName("id")]
    PrimaryKey Id { get; set; }

    /// <summary>
    /// Steam64 ID for the target player.
    /// </summary>
    [JsonPropertyName("target_steam_64")]
    ulong Player { get; set; }

    /// <summary>
    /// Short message about the player.
    /// </summary>
    [JsonPropertyName("message")]
    string? Message { get; set; }

    /// <summary>
    /// Other related players, including admins.
    /// </summary>
    [JsonPropertyName("actors")]
    RelatedActor[] Actors { get; set; }

    /// <summary>
    /// If the entry was from before the moderation rewrite.
    /// </summary>
    [JsonPropertyName("is_legacy")]
    bool IsLegacy { get; set; }

    /// <summary>
    /// When the entry was started, i.e. when an offense was reported.
    /// </summary>
    [JsonPropertyName("started_utc")]
    DateTimeOffset StartedTimestamp { get; set; }

    /// <summary>
    /// When the entry was finished, i.e. when a punishment was handed out. <see langword="null"/> if the entry is still in progress.
    /// </summary>
    [JsonPropertyName("resolved_utc")]
    DateTimeOffset? ResolvedTimestamp { get; set; }

    /// <summary>
    /// Effect this entry has on the player's reputation. Negative for punishments, positive for commendations.
    /// </summary>
    [JsonPropertyName("reputation")]
    double Reputation { get; set; }

    /// <summary>
    /// If this entry's reputation change has been applied.
    /// </summary>
    [JsonPropertyName("pending_reputation")]
    double PendingReputation { get; set; }

    /// <summary>
    /// Unique legacy ID to only this type of entry. Only will exist when <see cref="IsLegacy"/> is <see langword="true"/>.
    /// </summary>
    [JsonPropertyName("legacy_id")]
    uint? LegacyId { get; set; }

    /// <summary>
    /// Start time of <see cref="ActionLog"/>s relevant to this entry.
    /// </summary>
    [JsonPropertyName("relevant_logs_begin_utc")]
    DateTimeOffset? RelevantLogsBegin { get; set; }

    /// <summary>
    /// End time of <see cref="ActionLog"/>s relevant to this entry.
    /// </summary>
    [JsonPropertyName("relevant_logs_end_utc")]
    DateTimeOffset? RelevantLogsEnd { get; set; }

    /// <summary>
    /// URL's to video/photo evidence.
    /// </summary>
    [JsonPropertyName("evidence")]
    Evidence[] Evidence { get; set; }

    /// <summary>
    /// If the moderation entry was removed.
    /// </summary>
    [JsonPropertyName("is_removed")]
    bool Removed { get; set; }

    /// <summary>
    /// Who removed the moderation entry.
    /// </summary>
    [JsonPropertyName("removing_actor")]
    [JsonConverter(typeof(ActorConverter))]
    IModerationActor? RemovedBy { get; set; }

    /// <summary>
    /// When the moderation entry was removed.
    /// </summary>
    [JsonPropertyName("removed_timestamp_utc")]
    DateTimeOffset? RemovedTimestamp { get; set; }

    /// <summary>
    /// Why the moderation entry was removed.
    /// </summary>
    [JsonPropertyName("removed_message")]
    string? RemovedMessage { get; set; }

    /// <summary>
    /// The keys of related moderation entries.
    /// </summary>
    [JsonPropertyName("related_entries")]
    PrimaryKey[] RelatedEntryKeys { get; set; }

    /// <summary>
    /// Related moderation entries to this one.
    /// </summary>
    [JsonIgnore]
    ModerationEntry?[]? RelatedEntries { get; set; }
}
public interface IForgiveableModerationEntry : IDurationModerationEntry
{
    /// <summary>
    /// If the moderation entry was forgiven.
    /// </summary>
    [JsonPropertyName("is_forgiven")]
    bool Forgiven { get; set; }

    /// <summary>
    /// Who forgave the moderation entry.
    /// </summary>
    [JsonPropertyName("forgiving_actor")]
    [JsonConverter(typeof(ActorConverter))]
    IModerationActor? ForgivenBy { get; set; }

    /// <summary>
    /// When the moderation entry was forgiven.
    /// </summary>
    [JsonPropertyName("forgive_timestamp_utc")]
    DateTimeOffset? ForgiveTimestamp { get; set; }

    /// <summary>
    /// Why the moderation entry was forgiven.
    /// </summary>
    [JsonPropertyName("forgive_message")]
    string? ForgiveMessage { get; set; }

    /// <summary>
    /// Checks if the punishment is still active.
    /// </summary>
    /// <param name="considerForgiven">Considers the values of <see cref="Forgiven"/> and <see cref="ModerationEntry.Removed"/>.</param>
    /// <exception cref="InvalidOperationException">This punishment hasn't been resolved (<see cref="ModerationEntry.ResolvedTimestamp"/> is <see langword="null"/>).</exception>
    bool IsApplied(bool considerForgiven);

    /// <summary>
    /// Checks if the punishment was still active at <paramref name="timestamp"/>.
    /// </summary>
    /// <param name="considerForgiven">Considers the values of <see cref="Forgiven"/> and <see cref="ModerationEntry.Removed"/>.</param>
    /// <exception cref="InvalidOperationException">This punishment hasn't been resolved (<see cref="ModerationEntry.ResolvedTimestamp"/> is <see langword="null"/>).</exception>
    bool WasAppliedAt(DateTimeOffset timestamp, bool considerForgiven);
}

public interface IDurationModerationEntry : IModerationEntry
{
    /// <summary>
    /// Length of the punishment, negative implies permanent.
    /// </summary>
    [JsonPropertyName("duration")]
    TimeSpan Duration { get; set; }

    /// <summary>
    /// Returns <see langword="true"/> if the punishment will never expire, not considering <see cref="Forgiven"/>.
    /// </summary>
    /// <remarks>This is indicated by a negative <see cref="Duration"/>.</remarks>
    /// <exception cref="ArgumentException">Thrown if you set to <see langword="false"/>.</exception>
    [JsonIgnore]
    bool IsPermanent { get; set; }
}

public class ModerationCache : Dictionary<uint, ModerationEntryCacheEntry>
{
    public ModerationCache() { }
    public ModerationCache(int capacity) : base(capacity) { }
    public new IModerationEntry this[uint key]
    {
        get => base[key].Entry;
        set => base[key] = new ModerationEntryCacheEntry(value);
    }
    public void AddOrUpdate(IModerationEntry entry)
    {
        if (entry.Id.IsValid)
            this[entry.Id.Key] = entry;
    }

    public bool TryGet<T>(PrimaryKey key, out T value) where T : ModerationEntry
    {
        if (key.IsValid && TryGetValue(key.Key, out ModerationEntryCacheEntry entry))
        {
            value = (entry.Entry as T)!;
            return value != null;
        }

        value = null!;
        return false;
    }
    public bool TryGet<T>(PrimaryKey key, out T value, TimeSpan timeout) where T : class, IModerationEntry
    {
        if (key.IsValid && timeout.Ticks > 0 && TryGetValue(key.Key, out ModerationEntryCacheEntry entry))
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
    public IModerationEntry Entry { get; }
    public DateTime LastRefreshed { get; }
    public ModerationEntryCacheEntry(IModerationEntry entry) : this(entry, DateTime.UtcNow) { }
    public ModerationEntryCacheEntry(IModerationEntry entry, DateTime lastRefreshed)
    {
        Entry = entry;
        LastRefreshed = lastRefreshed;
    }
}
public sealed class ModerationEntryConverter : JsonConverter<ModerationEntry>
{
    public override ModerationEntry Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null!;
        else if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException($"Unexpected token parsing ModerationEntry: {reader.TokenType}.");

        Utf8JsonReader reader2 = reader;
        ModerationEntryType? type = null;
        while (reader2.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;
            if (reader.TokenType == JsonTokenType.StartObject)
                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject);
            if (reader.TokenType == JsonTokenType.StartArray)
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray);
            if (reader.TokenType == JsonTokenType.PropertyName && reader.GetString()!.Equals("type", StringComparison.InvariantCultureIgnoreCase))
            {
                if (!reader.Read())
                    break;

                if (reader.TokenType == JsonTokenType.String)
                {
                    string str = reader.GetString()!;
                    if (int.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out int val) && val <= (int)ModerationEntry.MaxEntry && val >= 0)
                    {
                        type = (ModerationEntryType)val;
                        break;
                    }
                    if (Enum.TryParse(str, true, out ModerationEntryType type2))
                    {
                        type = type2;
                        break;
                    }

                    throw new JsonException("Invalid string value for ModerationEntryType");
                }
                if (reader.TokenType == JsonTokenType.Number)
                {
                    if (!reader.TryGetInt32(out int val) || val > (int)ModerationEntry.MaxEntry && val < 0)
                        throw new JsonException("Invalid number value for ModerationEntryType");

                    type = (ModerationEntryType)val;
                    break;
                }

                throw new JsonException($"Unexpected token for 'type' of ModerationEntry: {reader.TokenType}.");
            }
        }

        if (!type.HasValue || ModerationReflection.GetType(type.Value) is not { } valueType)
            throw new JsonException("The property, 'type', is not specified for ModerationEntry.");

        ModerationEntry entry = (ModerationEntry)Activator.CreateInstance(valueType);

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string? prop = reader.GetString();
                if (prop == null)
                    continue;
                if (reader.Read())
                    entry.ReadProperty(ref reader, prop, options);
            }
        }

        return entry;
    }

    public override void Write(Utf8JsonWriter writer, ModerationEntry value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        ModerationEntryType type = ModerationReflection.GetType(value.GetType()) ?? ModerationEntryType.None;

        writer.WriteStartObject();

        writer.WritePropertyName("type");
        writer.WriteStringValue(type.ToString());

        value.Write(writer, options);

        writer.WriteEndObject();
    }
}
[Translatable("Moderation Entry Type", IsPrioritizedTranslation = false)]
public enum ModerationEntryType : ushort
{
    None,
    Warning,
    Kick,
    Ban,
    Mute,
    AssetBan,
    Teamkill,
    VehicleTeamkill,
    [Translatable("BattlEye Kick")]
    BattlEyeKick,
    Appeal,
    [Translatable("Custom Report")]
    Report,
    GriefingReport,
    ChatAbuseReport,
    CheatingReport,
    Note,
    Commendation,
    [Translatable("Accepted Bug Report")]
    BugReportAccepted,
    [Translatable("Accepted Player Report")]
    PlayerReportAccepted

    // update ModerationEntry.MaxEntry when adding
}