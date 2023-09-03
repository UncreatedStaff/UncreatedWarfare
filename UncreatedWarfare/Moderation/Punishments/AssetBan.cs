using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using SDG.Unturned;
using Steamworks;
using Uncreated.Encoding;
using Uncreated.SQL;
using Uncreated.Warfare.Vehicles;

namespace Uncreated.Warfare.Moderation.Punishments;

[ModerationEntry(ModerationEntryType.AssetBan)]
[JsonConverter(typeof(ModerationEntryConverter))]
public class AssetBan : DurationPunishment
{
    [JsonPropertyName("asset_filter")]
    public PrimaryKey[] AssetFilter { get; set; } = Array.Empty<PrimaryKey>();

    public bool IsAssetBanned(PrimaryKey assetKey, bool considerForgiven, bool checkStillActive = true)
    {
        if (checkStillActive && !IsApplied(considerForgiven))
            return false;

        if (!checkStillActive && considerForgiven && (Forgiven || Removed))
            return true;
        
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

    public override async Task AddExtraInfo(DatabaseInterface db, List<string> workingList, IFormatProvider formatter, CancellationToken token = default)
    {
        const int maxFilters = 4;

        await base.AddExtraInfo(db, workingList, formatter, token);

        if (AssetFilter.Length > 0)
        {
            if (UCWarfare.IsLoaded && Data.Singletons.TryGetSingleton(out VehicleBay vb))
            {
                await vb.WaitAsync(token).ConfigureAwait(false);
                try
                {
                    int ct = 0;
                    foreach (SqlItem<VehicleData> data in vb.Items)
                    {
                        if (Array.Exists(AssetFilter, x => x == data.PrimaryKey) && data.Item is { } v)
                        {
                            if (ct >= maxFilters)
                            {
                                workingList[workingList.Count - 1] += " (and " + (AssetFilter.Length - ct).ToString(formatter) + " more)";
                                break;
                            }
                            workingList.Add($"Asset Filter - {Assets.find(v.VehicleID)?.FriendlyName ?? v.VehicleID.ToString("N")}");
                            ++ct;
                        }
                    }
                }
                finally
                {
                    vb.Release();
                }
            }
            else
            {
                int ct = Math.Min(maxFilters, AssetFilter.Length);
                for (int i = 0; i < ct; ++i)
                {
                    workingList.Add($"Asset Filter - {AssetFilter[i]}");
                }

                if (ct < AssetFilter.Length)
                    workingList[workingList.Count - 1] += " (and " + (AssetFilter.Length - ct).ToString(formatter) + " more)";
            }
        }
        else
        {
            workingList.Add("Asset banned from all assets");
        }
    }

    internal override int EstimateColumnCount() => base.EstimateColumnCount() + AssetFilter.Length;
    internal override bool AppendWriteCall(StringBuilder builder, List<object> args)
    {
        bool hasEvidenceCalls = base.AppendWriteCall(builder, args);

        builder.Append($"DELETE FROM `{DatabaseInterface.TableAssetBanFilters}` WHERE `{DatabaseInterface.ColumnExternalPrimaryKey}` = @0;" +
                       $" INSERT INTO `{DatabaseInterface.TableAssetBanFilters}` ({SqlTypes.ColumnList(
            DatabaseInterface.ColumnExternalPrimaryKey, DatabaseInterface.ColumnAssetBanFiltersAsset)}) VALUES ");
        
        for (int i = 0; i < AssetFilter.Length; ++i)
        {
            F.AppendPropertyList(builder, args.Count, 1, i, 1);
            args.Add(AssetFilter[i].Key);
        }
        builder.Append(';');

        return hasEvidenceCalls;
    }
}
