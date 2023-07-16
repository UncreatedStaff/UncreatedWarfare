using System;
using System.Text.Json;
using System.Text.Json.Serialization;

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
}