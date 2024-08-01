using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Database.Manual;

namespace Uncreated.Warfare.Moderation.Punishments;

[ModerationEntry(ModerationEntryType.Warning)]
[JsonConverter(typeof(ModerationEntryConverter))]
public class Warning : Punishment
{
    /// <summary>
    /// The timestamp the player actually sees the warning. This is for when a player is warned while they're offline.
    /// </summary>
    [JsonPropertyName("displayed_utc")]
    public DateTimeOffset? DisplayedTimestamp { get; set; }

    /// <summary>
    /// <see langword="false"/> until the player actually sees the warning. This is for when a player is warned while they're offline.
    /// </summary>
    /// <remarks>Always false for legacy <see cref="Warning"/>s.</remarks>
    [JsonIgnore]
    public bool HasBeenDisplayed => IsLegacy || DisplayedTimestamp.HasValue;

    public override string GetDisplayName() => "Warning";
    public override void ReadProperty(ref Utf8JsonReader reader, string propertyName, JsonSerializerOptions options)
    {
        if (propertyName.Equals("displayed_utc", StringComparison.InvariantCultureIgnoreCase))
            DisplayedTimestamp = reader.TokenType == JsonTokenType.Null ? null : reader.GetDateTimeOffset();
        else
            base.ReadProperty(ref reader, propertyName, options);
    }
    public override void Write(Utf8JsonWriter writer, JsonSerializerOptions options)
    {
        base.Write(writer, options);
        writer.WritePropertyName("displayed_utc");
        if (DisplayedTimestamp.HasValue)
            writer.WriteStringValue(DisplayedTimestamp.Value);
        else
            writer.WriteNullValue();
    }

    internal override int EstimateParameterCount() => base.EstimateParameterCount() + 1;
    public override async Task AddExtraInfo(DatabaseInterface db, List<string> workingList, IFormatProvider formatter, CancellationToken token = default)
    {
        await base.AddExtraInfo(db, workingList, formatter, token);

        if (!IsLegacy)
            workingList.Add(HasBeenDisplayed ? "Viewed at " + DisplayedTimestamp!.Value.ToString(ModerationUI.DateTimeFormat, formatter) : "Not yet viewed");
    }

    internal override bool AppendWriteCall(StringBuilder builder, List<object> args)
    {
        bool hasEvidenceCalls = base.AppendWriteCall(builder, args);

        builder.Append($" INSERT INTO `{DatabaseInterface.TableWarnings}` ({MySqlSnippets.ColumnList(
            DatabaseInterface.ColumnExternalPrimaryKey, DatabaseInterface.ColumnWarningsDisplayedTimestamp)}) VALUES " +
                       $"(@0, @{args.Count.ToString(CultureInfo.InvariantCulture)}) AS `t` " +
                       $"ON DUPLICATE KEY UPDATE `{DatabaseInterface.ColumnWarningsDisplayedTimestamp}` = `t`.`{DatabaseInterface.ColumnWarningsDisplayedTimestamp}`;");

        args.Add(DisplayedTimestamp.HasValue ? DisplayedTimestamp.Value.UtcDateTime : DBNull.Value);

        return hasEvidenceCalls;
    }
}