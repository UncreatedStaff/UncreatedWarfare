using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Uncreated.Encoding;
using Uncreated.SQL;
using Uncreated.Warfare.Moderation.Appeals;

namespace Uncreated.Warfare.Moderation.Punishments;

[ModerationEntry(ModerationEntryType.AssetBan)]
[JsonConverter(typeof(ModerationEntryConverter))]
public class AssetBan : DurationPunishment
{
    [JsonPropertyName("asset_filter")]
    public PrimaryKey[] AssetFilter { get; set; } = Array.Empty<PrimaryKey>();

    public bool IsAssetBanned(PrimaryKey assetKey, bool checkStillActive = true)
    {
        if (checkStillActive && !IsApplied())
            return false;
        if (AssetFilter.Length == 0) return true;
        if (!assetKey.IsValid) return false;
        int key = assetKey.Key;
        for (int i = 0; i < AssetFilter.Length; ++i)
        {
            if (AssetFilter[i].Key == key)
                return true;
        }

        return false;
    }
    protected override void ReadIntl(ByteReader reader, ushort version)
    {
        base.ReadIntl(reader, version);
        
        AssetFilter = new PrimaryKey[reader.ReadInt32()];
        for (int i = 0; i < AssetFilter.Length; ++i)
            AssetFilter[i] = reader.ReadInt32();
    }

    protected override void WriteIntl(ByteWriter writer)
    {
        base.WriteIntl(writer);
        
        writer.Write(AssetFilter.Length);
        for (int i = 0; i < AssetFilter.Length; ++i)
            writer.Write(AssetFilter[i].Key);
    }

    public override string GetDisplayName() => "Asset Ban";
    public override void ReadProperty(ref Utf8JsonReader reader, string propertyName, JsonSerializerOptions options)
    {
        if (propertyName.Equals("asset_filter", StringComparison.InvariantCultureIgnoreCase))
            AssetFilter = JsonSerializer.Deserialize<PrimaryKey[]>(ref reader, options) ?? Array.Empty<PrimaryKey>();
        else
            base.ReadProperty(ref reader, propertyName, options);
    }
    public override void Write(Utf8JsonWriter writer, JsonSerializerOptions options)
    {
        base.Write(writer, options);
        writer.WritePropertyName("asset_filter");
        writer.WriteStartArray();
        for (int i = 0; i < AssetFilter.Length; ++i)
            writer.WriteNumberValue(AssetFilter[i]);
        writer.WriteEndArray();
    }
}
