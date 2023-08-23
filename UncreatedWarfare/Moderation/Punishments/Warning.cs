using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Uncreated.SQL;

namespace Uncreated.Warfare.Moderation.Punishments;

[ModerationEntry(ModerationEntryType.Warning)]
[JsonConverter(typeof(ModerationEntryConverter))]
public class Warning : Punishment
{
    /// <summary>
    /// <see langword="false"/> until the player actually sees the warning. This is for when a player is warned while they're offline.
    /// </summary>
    [JsonPropertyName("has_been_displayed")]
    public bool HasBeenDisplayed { get; set; }
    public override string GetDisplayName() => "Warning";
    public override void ReadProperty(ref Utf8JsonReader reader, string propertyName, JsonSerializerOptions options)
    {
        if (propertyName.Equals("has_been_displayed", StringComparison.InvariantCultureIgnoreCase))
            HasBeenDisplayed = reader.GetBoolean();
        else
            base.ReadProperty(ref reader, propertyName, options);
    }
    public override void Write(Utf8JsonWriter writer, JsonSerializerOptions options)
    {
        base.Write(writer, options);
        writer.WriteBoolean("has_been_displayed", HasBeenDisplayed);
    }

    internal override int EstimateColumnCount() => base.EstimateColumnCount() + 1;
    internal override bool AppendWriteCall(StringBuilder builder, List<object> args)
    {
        bool hasEvidenceCalls = base.AppendWriteCall(builder, args);

        builder.Append($" INSERT INTO `{DatabaseInterface.TableWarnings}` ({SqlTypes.ColumnList(
            DatabaseInterface.ColumnExternalPrimaryKey, DatabaseInterface.ColumnWarningsHasBeenDisplayed)}) VALUES " +
                       $"(@{args.Count.ToString(CultureInfo.InvariantCulture)}) AS `t` " +
                       $"ON DUPLICATE KEY UPDATE `{DatabaseInterface.ColumnWarningsHasBeenDisplayed}` = `t`.`{DatabaseInterface.ColumnWarningsHasBeenDisplayed}`;");

        args.Add(HasBeenDisplayed);

        return hasEvidenceCalls;
    }
}