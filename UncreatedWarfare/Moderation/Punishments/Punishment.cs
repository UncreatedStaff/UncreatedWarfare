using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Encoding;
using Uncreated.Framework;
using Uncreated.SQL;
using Uncreated.Warfare.Moderation.Appeals;
using Uncreated.Warfare.Moderation.Punishments.Presets;
using Report = Uncreated.Warfare.Moderation.Reports.Report;

namespace Uncreated.Warfare.Moderation.Punishments;

[JsonConverter(typeof(ModerationEntryConverter))]
public abstract class Punishment : ModerationEntry
{
    /// <summary>
    /// Type of preset.
    /// </summary>
    [JsonPropertyName("preset_type")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PresetType PresetType { get; set; }

    /// <summary>
    /// Level of preset (indexed from 1).
    /// </summary>
    [JsonPropertyName("preset_level")]
    public int PresetLevel { get; set; }

    /// <summary>
    /// Keys for all related appeals.
    /// </summary>
    [JsonPropertyName("appeals")]
    public PrimaryKey[] AppealKeys { get; set; } = Array.Empty<PrimaryKey>();

    /// <summary>
    /// Keys for all related reports.
    /// </summary>
    [JsonPropertyName("reports")]
    public PrimaryKey[] ReportKeys { get; set; } = Array.Empty<PrimaryKey>();

    /// <summary>
    /// All related appeals.
    /// </summary>
    [JsonPropertyName("appeals_detail")]
    public Appeal?[]? Appeals { get; set; }

    /// <summary>
    /// All related reports.
    /// </summary>
    [JsonPropertyName("reports_detail")]
    public Report?[]? Reports { get; set; }

    /// <summary>
    /// Try to find a resolved appeal with a state matching the value for <paramref name="state"/> in <see cref="Appeals"/>.
    /// </summary>
    /// <param name="appeal">The first matching appeal found.</param>
    /// <param name="state">Which state to look for, defaults to accepted.</param>
    /// <returns><see langword="true"/> if an appeal is found.</returns>
    public bool TryFindAppeal(out Appeal appeal, bool state = true)
    {
        if (Appeals != null)
        {
            for (int i = 0; i < Appeals.Length; ++i)
            {
                Appeal? appeal2 = Appeals[i];
                if (appeal2 is { AppealState: not null } && appeal2.AppealState.Value == state)
                {
                    appeal = appeal2;
                    return true;
                }
            }
        }

        appeal = null!;
        return false;
    }
    internal override async Task FillDetail(DatabaseInterface db, CancellationToken token = default)
    {
        if (Appeals == null || Appeals.Length != AppealKeys.Length)
            Appeals = new Appeal?[AppealKeys.Length];
        if (Reports == null || Reports.Length != ReportKeys.Length)
            Reports = new Report?[ReportKeys.Length];

        await db.ReadAll(Appeals, AppealKeys, true, true, false, token).ConfigureAwait(false);
        await db.ReadAll(Reports, ReportKeys, true, true, false, token).ConfigureAwait(false);

        await base.FillDetail(db, token).ConfigureAwait(false);
    }

    protected override void ReadIntl(ByteReader reader, ushort version)
    {
        base.ReadIntl(reader, version);
        
        AppealKeys = new PrimaryKey[reader.ReadInt32()];
        for (int i = 0; i < AppealKeys.Length; ++i)
            AppealKeys[i] = reader.ReadUInt32();
        ReportKeys = new PrimaryKey[reader.ReadInt32()];
        for (int i = 0; i < ReportKeys.Length; ++i)
            ReportKeys[i] = reader.ReadUInt32();
        Appeals = null;
        Reports = null;
    }

    protected override void WriteIntl(ByteWriter writer)
    {
        base.WriteIntl(writer);
        
        writer.Write(AppealKeys.Length);
        for (int i = 0; i < AppealKeys.Length; ++i)
            writer.Write(AppealKeys[i].Key);
        writer.Write(ReportKeys.Length);
        for (int i = 0; i < ReportKeys.Length; ++i)
            writer.Write(ReportKeys[i].Key);
    }

    public override void ReadProperty(ref Utf8JsonReader reader, string propertyName, JsonSerializerOptions options)
    {
        if (propertyName.Equals("appeals", StringComparison.InvariantCultureIgnoreCase))
            AppealKeys = JsonSerializer.Deserialize<PrimaryKey[]>(ref reader, options) ?? Array.Empty<PrimaryKey>();
        else if (propertyName.Equals("appeals_detail", StringComparison.InvariantCultureIgnoreCase))
            Appeals = JsonSerializer.Deserialize<Appeal[]>(ref reader, options);
        else if (propertyName.Equals("reports", StringComparison.InvariantCultureIgnoreCase))
            ReportKeys = JsonSerializer.Deserialize<PrimaryKey[]>(ref reader, options) ?? Array.Empty<PrimaryKey>();
        else if (propertyName.Equals("reports_detail", StringComparison.InvariantCultureIgnoreCase))
            Reports = JsonSerializer.Deserialize<Report[]>(ref reader, options);
        else if (propertyName.Equals("preset_level", StringComparison.InvariantCultureIgnoreCase))
            PresetLevel = reader.TokenType == JsonTokenType.Null ? 0 : reader.GetInt32();
        else if (propertyName.Equals("preset_type", StringComparison.InvariantCultureIgnoreCase))
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.Null:
                    PresetType = PresetType.None;
                    break;
                case JsonTokenType.Number:
                    PresetType = (PresetType)reader.GetInt32();
                    break;
                case JsonTokenType.String:
                    if (Enum.TryParse(reader.GetString(), true, out PresetType type))
                        PresetType = type;
                    else throw new JsonException($"Unable to read preset type with value: {reader.GetString()}.");
                    break;
                default:
                    throw new JsonException($"Unexpected token type {reader.TokenType} for \"preset_type\".");
            }
        }
        else
            base.ReadProperty(ref reader, propertyName, options);
    }

    public override void Write(Utf8JsonWriter writer, JsonSerializerOptions options)
    {
        base.Write(writer, options);

        if (PresetType != PresetType.None)
            writer.WriteString("preset_type", PresetType.ToString());
        if (PresetLevel > 0)
            writer.WriteNumber("preset_level", PresetLevel);

        if (Appeals is { Length: > 0 } && Appeals.Length == AppealKeys.Length && Appeals.All(x => x != null))
        {
            writer.WritePropertyName("appeals_detail");
            JsonSerializer.Serialize(writer, Appeals, options);
        }
        writer.WritePropertyName("appeals");
        JsonSerializer.Serialize(writer, AppealKeys, options);

        if (Reports is { Length: > 0 } && Reports.Length == ReportKeys.Length && Reports.All(x => x != null))
        {
            writer.WritePropertyName("reports_detail");
            JsonSerializer.Serialize(writer, Reports, options);
        }
        writer.WritePropertyName("reports");
        JsonSerializer.Serialize(writer, ReportKeys, options);
    }

    internal override int EstimateParameterCount() => base.EstimateParameterCount() + AppealKeys.Length + ReportKeys.Length + 2;
    public override async Task AddExtraInfo(DatabaseInterface db, List<string> workingList, IFormatProvider formatter, CancellationToken token = default)
    {
        await base.AddExtraInfo(db, workingList, formatter, token);
        if (PresetType != PresetType.None)
        {
            workingList.Add($"Preset: {(UCWarfare.IsLoaded ? Localization.TranslateEnum(PresetType) : PresetType.ToString())} | Level {PresetLevel.ToString(formatter)}");
        }
    }
    internal override bool AppendWriteCall(StringBuilder builder, List<object> args)
    {
        bool hasEvidenceCalls = base.AppendWriteCall(builder, args);

        builder.Append($" INSERT INTO `{DatabaseInterface.TablePunishments}` ({SqlTypes.ColumnList(
            DatabaseInterface.ColumnExternalPrimaryKey, DatabaseInterface.ColumnPunishmentsPresetType, DatabaseInterface.ColumnPunishmentsPresetLevel)}) VALUES ");

        F.AppendPropertyList(builder, args.Count, 2, 0, 1);
        builder.Append(" AS `t` " +
                       $"ON DUPLICATE KEY UPDATE `{DatabaseInterface.ColumnPunishmentsPresetType}` = `t`.`{DatabaseInterface.ColumnPunishmentsPresetType}`," +
                       $"`{DatabaseInterface.ColumnPunishmentsPresetLevel}` = `t`.`{DatabaseInterface.ColumnPunishmentsPresetLevel}`;");

        args.Add(PresetType == PresetType.None ? DBNull.Value : PresetType.ToString());
        args.Add(PresetLevel <= 0 || PresetType == PresetType.None ? DBNull.Value : PresetLevel);

        builder.Append($"DELETE FROM `{DatabaseInterface.TableLinkedReports}` WHERE `{DatabaseInterface.ColumnExternalPrimaryKey}` = @0;");

        if (ReportKeys.Length > 0)
        {
            builder.Append($" INSERT INTO `{DatabaseInterface.TableLinkedReports}` ({SqlTypes.ColumnList(
                DatabaseInterface.ColumnExternalPrimaryKey, DatabaseInterface.ColumnLinkedReportsReport)}) VALUES ");
            for (int i = 0; i < ReportKeys.Length; ++i)
            {
                F.AppendPropertyList(builder, args.Count, 1, i, 1);
                args.Add(ReportKeys[i].Key);
            }
            builder.Append(';');
        }
        builder.Append($"DELETE FROM `{DatabaseInterface.TableLinkedAppeals}` WHERE `{DatabaseInterface.ColumnExternalPrimaryKey}` = @0;");
        
        if (AppealKeys.Length > 0)
        {
            builder.Append($" INSERT INTO `{DatabaseInterface.TableLinkedAppeals}` ({SqlTypes.ColumnList(
                DatabaseInterface.ColumnExternalPrimaryKey, DatabaseInterface.ColumnLinkedAppealsAppeal)}) VALUES ");
            for (int i = 0; i < AppealKeys.Length; ++i)
            {
                F.AppendPropertyList(builder, args.Count, 1, i, 1);
                args.Add(AppealKeys[i].Key);
            }
            builder.Append(';');
        }

        return hasEvidenceCalls;
    }
}

public abstract class DurationPunishment : Punishment, IForgiveableModerationEntry
{
    /// <summary>
    /// Length of the punishment, negative implies permanent.
    /// </summary>
    [JsonPropertyName("duration")]
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Returns <see langword="true"/> if the punishment will never expire, not considering <see cref="Forgiven"/>.
    /// </summary>
    /// <remarks>This is indicated by a negative <see cref="Duration"/>.</remarks>
    /// <exception cref="ArgumentException">Thrown if you set to <see langword="false"/>.</exception>
    [JsonIgnore]
    public bool IsPermanent
    {
        get => Duration.Ticks < 0L;
        set => Duration = value ? Timeout.InfiniteTimeSpan : throw new ArgumentException("Can not set IsPermanent to false.", nameof(value));
    }

    /// <summary>
    /// If the moderation entry was forgiven.
    /// </summary>
    [JsonPropertyName("is_forgiven")]
    public bool Forgiven { get; set; }

    /// <summary>
    /// Who forgave the moderation entry.
    /// </summary>
    [JsonPropertyName("forgiving_actor")]
    [JsonConverter(typeof(ActorConverter))]
    public IModerationActor? ForgivenBy { get; set; }

    /// <summary>
    /// When the moderation entry was forgiven.
    /// </summary>
    [JsonPropertyName("forgive_timestamp_utc")]
    public DateTimeOffset? ForgiveTimestamp { get; set; }

    /// <summary>
    /// Why the moderation entry was forgiven.
    /// </summary>
    [JsonPropertyName("forgive_message")]
    public string? ForgiveMessage { get; set; }

    /// <summary>
    /// Gets the time at which the punishment expires.
    /// </summary>
    /// <param name="considerForgiven">Considers the values of <see cref="Forgiven"/> and <see cref="ModerationEntry.Removed"/>.</param>
    /// <exception cref="InvalidOperationException">This punishment hasn't been resolved (<see cref="ModerationEntry.ResolvedTimestamp"/> is <see langword="null"/>).</exception>
    public DateTimeOffset GetExpiryTimestamp(bool considerForgiven)
    {
        if (!ResolvedTimestamp.HasValue)
            throw new InvalidOperationException(GetType().Name + " has not been resolved.");
        
        if (considerForgiven)
        {
            if (Forgiven && ForgiveTimestamp.HasValue)
                return ForgiveTimestamp.Value;

            if (Removed && RemovedTimestamp.HasValue)
                return RemovedTimestamp.Value;
        }

        return IsPermanent ? DateTimeOffset.MaxValue : ResolvedTimestamp.Value.Add(Duration);
    }

    /// <summary>
    /// Checks if the punishment is still active.
    /// </summary>
    /// <param name="considerForgiven">Considers the values of <see cref="Forgiven"/> and <see cref="ModerationEntry.Removed"/>.</param>
    /// <exception cref="InvalidOperationException">This punishment hasn't been resolved (<see cref="ModerationEntry.ResolvedTimestamp"/> is <see langword="null"/>).</exception>
    public bool IsApplied(bool considerForgiven)
    {
        if (!ResolvedTimestamp.HasValue)
            throw new InvalidOperationException(GetType().Name + " has not been resolved.");

        if (considerForgiven)
        {
            if (Forgiven && ForgiveTimestamp.HasValue)
                return ForgiveTimestamp.Value > DateTime.UtcNow;

            if (Removed && RemovedTimestamp.HasValue)
                return RemovedTimestamp.Value > DateTime.UtcNow;
        }

        L.LogDebug($"{ResolvedTimestamp.Value} + {Duration}.");
        return IsPermanent || DateTime.UtcNow > ResolvedTimestamp.Value.UtcDateTime.Add(Duration);
    }

    /// <summary>
    /// Checks if the punishment was still active at <paramref name="timestamp"/>.
    /// </summary>
    /// <param name="considerForgiven">Considers the values of <see cref="Forgiven"/> and <see cref="ModerationEntry.Removed"/>.</param>
    /// <exception cref="InvalidOperationException">This punishment hasn't been resolved (<see cref="ModerationEntry.ResolvedTimestamp"/> is <see langword="null"/>).</exception>
    public bool WasAppliedAt(DateTimeOffset timestamp, bool considerForgiven)
    {
        if (!ResolvedTimestamp.HasValue)
            throw new InvalidOperationException(GetType().Name + " has not been resolved.");

        if (considerForgiven)
        {
            if (Forgiven && ForgiveTimestamp.HasValue)
                return ForgiveTimestamp.Value > timestamp.UtcDateTime;

            if (Removed && RemovedTimestamp.HasValue)
                return RemovedTimestamp.Value > timestamp.UtcDateTime;
        }

        return IsPermanent || timestamp.UtcDateTime > ResolvedTimestamp.Value.UtcDateTime.Add(Duration);
    }
    public override void ReadProperty(ref Utf8JsonReader reader, string propertyName, JsonSerializerOptions options)
    {
        if (propertyName.Equals("duration", StringComparison.InvariantCultureIgnoreCase))
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                if (reader.TokenType == JsonTokenType.Null)
                    IsPermanent = true;
                else
                    throw new JsonException("Expected string duration.");
            }
            else
            {
                string str = reader.GetString()!;
                if (TimeSpan.TryParseExact(str, "G", CultureInfo.InvariantCulture, TimeSpanStyles.None, out TimeSpan ts) ||
                    TimeSpan.TryParse(str, CultureInfo.InvariantCulture, out ts))
                {
                    Duration = ts;
                    if (ts.Ticks < 0)
                        IsPermanent = true;
                }
                else if (str.Equals("permanent", StringComparison.InvariantCultureIgnoreCase))
                    IsPermanent = true;
                else throw new JsonException($"Invalid duration: \"{str}\".");
            }
        }
        else if (propertyName.Equals("is_forgiven", StringComparison.InvariantCultureIgnoreCase))
            Removed = reader.TokenType != JsonTokenType.Null && reader.GetBoolean();
        else if (propertyName.Equals("forgiving_actor", StringComparison.InvariantCultureIgnoreCase))
            RemovedBy = reader.TokenType == JsonTokenType.Null ? null : Moderation.Actors.GetActor(reader.GetUInt64());
        else if (propertyName.Equals("forgive_timestamp_utc", StringComparison.InvariantCultureIgnoreCase))
            RemovedTimestamp = reader.TokenType == JsonTokenType.Null ? null : new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(), DateTimeKind.Utc));
        else if (propertyName.Equals("forgive_message", StringComparison.InvariantCultureIgnoreCase))
            RemovedMessage = reader.GetString();
        else
            base.ReadProperty(ref reader, propertyName, options);
    }
    public override void Write(Utf8JsonWriter writer, JsonSerializerOptions options)
    {
        base.Write(writer, options);

        writer.WritePropertyName("duration");
        writer.WriteStringValue(IsPermanent ? "permanent" : Duration.ToString("G", CultureInfo.InvariantCulture));

        writer.WriteBoolean("is_forgiven", Forgiven);
        if (Forgiven)
        {
            writer.WriteNumber("forgiving_actor", ForgivenBy == null ? 0ul : ForgivenBy.Id);
            if (ForgiveTimestamp.HasValue)
                writer.WriteString("forgive_timestamp_utc", ForgiveTimestamp.Value.UtcDateTime);

            writer.WriteString("forgive_message", ForgiveMessage);
        }
    }
    internal override int EstimateParameterCount() => base.EstimateParameterCount() + 5;
    public override async Task AddExtraInfo(DatabaseInterface db, List<string> workingList, IFormatProvider formatter, CancellationToken token = default)
    {
        await base.AddExtraInfo(db, workingList, formatter, token);

        if (IsPermanent)
            workingList.Add("Permanent");
        else
            workingList.Add($"Duration: {Util.ToTimeString((int)Math.Round(Duration.TotalSeconds))}");

        if (!Removed && Forgiven)
        {
            if (ForgivenBy != null)
            {
                string disp = await ForgivenBy.GetDisplayName(db, token).ConfigureAwait(false) + " (" + ForgivenBy.Id.ToString(CultureInfo.InvariantCulture) + ")";
                if (ForgiveTimestamp.HasValue)
                    workingList.Add($"Forgiven By: {disp} @ {ForgiveTimestamp.Value.UtcDateTime.ToString(ModerationUI.DateTimeFormat, formatter)}");
                else
                    workingList.Add($"Forgiven By: {disp}");
            }
            else
            {
                if (ForgiveTimestamp.HasValue)
                    workingList.Add($"Forgiven @ {ForgiveTimestamp.Value.UtcDateTime.ToString(ModerationUI.DateTimeFormat, formatter)}");
                else
                    workingList.Add("Forgiven");
            }
            if (ForgiveMessage != null)
            {
                workingList.Add("For: \"" + ForgiveMessage.MaxLength(64) + "\"");
            }
        }
    }

    internal override bool AppendWriteCall(StringBuilder builder, List<object> args)
    {
        bool hasEvidenceCalls = base.AppendWriteCall(builder, args);

        builder.Append($" INSERT INTO `{DatabaseInterface.TableDurationPunishments}` ({SqlTypes.ColumnList(
            DatabaseInterface.ColumnExternalPrimaryKey, DatabaseInterface.ColumnDurationsDurationSeconds, DatabaseInterface.ColumnDurationsForgiven,
            DatabaseInterface.ColumnDurationsForgivenBy, DatabaseInterface.ColumnDurationsForgivenTimestamp, DatabaseInterface.ColumnDurationsForgivenReason)}) VALUES ");

        F.AppendPropertyList(builder, args.Count, 5, 0, 1);
        builder.Append(" AS `t` " +
                       $"ON DUPLICATE KEY UPDATE `{DatabaseInterface.ColumnDurationsDurationSeconds}` = `t`.`{DatabaseInterface.ColumnDurationsDurationSeconds}`," +
                       $"`{DatabaseInterface.ColumnDurationsForgiven}` = `t`.`{DatabaseInterface.ColumnDurationsForgiven}`," +
                       $"`{DatabaseInterface.ColumnDurationsForgivenBy}` = `t`.`{DatabaseInterface.ColumnDurationsForgivenBy}`," +
                       $"`{DatabaseInterface.ColumnDurationsForgivenTimestamp}` = `t`.`{DatabaseInterface.ColumnDurationsForgivenTimestamp}`," +
                       $"`{DatabaseInterface.ColumnDurationsForgivenReason}` = `t`.`{DatabaseInterface.ColumnDurationsForgivenReason}`;");

        args.Add(IsPermanent ? -1L : (long)Math.Round(Duration.TotalSeconds));
        args.Add(Forgiven);
        args.Add(ForgivenBy == null ? DBNull.Value : ForgivenBy.Id);
        args.Add(ForgiveTimestamp.HasValue ? ForgiveTimestamp.Value.UtcDateTime : DBNull.Value);
        args.Add((object?)ForgiveMessage.MaxLength(1024) ?? DBNull.Value);

        return hasEvidenceCalls;
    }
}