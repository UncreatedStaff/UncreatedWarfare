using SDG.Framework.Utilities;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
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

    [JsonPropertyName("vehicle_type_filter")]
    [JsonConverter(typeof(ArrayConverter<VehicleType, JsonStringEnumConverter>))]
    public VehicleType[] VehicleTypeFilter { get; set; }

    public bool IsAssetBanned(VehicleType type, bool considerForgiven, bool checkStillActive = true)
    {
        if (checkStillActive && !IsApplied(considerForgiven))
            return false;

        if (!checkStillActive && considerForgiven && (Forgiven || Removed))
            return true;
        
        if (VehicleTypeFilter.Length == 0 && AssetFilter.Length == 0) return true;
        if (type == VehicleType.None) return false;
        for (int i = 0; i < VehicleTypeFilter.Length; ++i)
        {
            if (VehicleTypeFilter[i] == type)
                return true;
        }

        return false;
    }
    public bool IsAssetBanned(PrimaryKey assetKey, bool considerForgiven, bool checkStillActive = true)
    {
        if (checkStillActive && !IsApplied(considerForgiven))
            return false;

        if (!checkStillActive && considerForgiven && (Forgiven || Removed))
            return true;
        
        if (VehicleTypeFilter.Length == 0 && AssetFilter.Length == 0) return true;
        if (!assetKey.IsValid) return false;
        int key = assetKey.Key;
        for (int i = 0; i < AssetFilter.Length; ++i)
        {
            if (AssetFilter[i].Key == key)
                return true;
        }

        return false;
    }
    public bool IsAssetBanned(PrimaryKey assetKey, VehicleType type, bool considerForgiven, bool checkStillActive = true)
    {
        if (checkStillActive && !IsApplied(considerForgiven))
            return false;

        if (!checkStillActive && considerForgiven && (Forgiven || Removed))
            return true;
        
        if (VehicleTypeFilter.Length == 0 && AssetFilter.Length == 0) return true;
        if (assetKey.IsValid)
        {
            int key = assetKey.Key;
            for (int i = 0; i < AssetFilter.Length; ++i)
            {
                if (AssetFilter[i].Key == key)
                    return true;
            }
        }

        if (type != VehicleType.None)
        {
            for (int i = 0; i < VehicleTypeFilter.Length; ++i)
            {
                if (VehicleTypeFilter[i] == type)
                    return true;
            }
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
        else if (propertyName.Equals("vehicle_type_filter", StringComparison.InvariantCultureIgnoreCase))
        {
            if (reader.TokenType == JsonTokenType.Null)
                VehicleTypeFilter = Array.Empty<VehicleType>();
            else if (reader.TokenType == JsonTokenType.StartArray)
            {
                List<VehicleType> list;
                bool pooled = false;
                if (UCWarfare.IsLoaded && UCWarfare.IsMainThread)
                {
                    pooled = true;
                    list = ListPool<VehicleType>.claim();
                }
                else list = new List<VehicleType>(16);

                try
                {
                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonTokenType.EndArray)
                            break;
                        switch (reader.TokenType)
                        {
                            case JsonTokenType.Null:
                                list.Add(VehicleType.None);
                                break;
                            case JsonTokenType.String:
                                list.Add((VehicleType)Enum.Parse(typeof(VehicleType), reader.GetString()!, true));
                                break;
                            case JsonTokenType.Number:
                                list.Add((VehicleType)reader.GetInt32());
                                break;
                            default:
                                throw new JsonException($"Invalid token type: {reader.TokenType} for VehicleType[] element.");
                        }
                    }

                    VehicleTypeFilter = list.Count == 0 ? Array.Empty<VehicleType>() : list.ToArray();
                }
                finally
                {
                    if (pooled)
                        ListPool<VehicleType>.release(list);
                }
            }
            else
                throw new JsonException($"Invalid token type: {reader.TokenType} for VehicleType[].");
        }
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
        writer.WritePropertyName("vehicle_type_filter");
        writer.WriteStartArray();
        for (int i = 0; i < VehicleTypeFilter.Length; ++i)
            writer.WriteStringValue(VehicleTypeFilter[i].ToString());
        writer.WriteEndArray();
    }

    public override async Task AddExtraInfo(DatabaseInterface db, List<string> workingList, IFormatProvider formatter, CancellationToken token = default)
    {
        int maxFilters = VehicleTypeFilter.Length > 0 ? 3 : 4;

        await base.AddExtraInfo(db, workingList, formatter, token);

        workingList.Add($"Type Filter - {string.Join(", ", VehicleTypeFilter.Select(x => UCWarfare.IsLoaded ? Localization.TranslateEnum(x) : x.ToString()))}");

        int ct = 0;
        if (AssetFilter.Length > 0)
        {
            if (UCWarfare.IsLoaded && Data.Singletons.TryGetSingleton(out VehicleBay vb))
            {
                await vb.WaitAsync(token).ConfigureAwait(false);
                try
                {
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
                int ct2 = Math.Min(maxFilters, AssetFilter.Length);
                for (int i = 0; i < ct2; ++i)
                {
                    workingList.Add($"Asset Filter - {AssetFilter[i]}");
                }

                if (ct2 < AssetFilter.Length)
                    workingList[workingList.Count - 1] += " (and " + (AssetFilter.Length - ct2).ToString(formatter) + " more)";
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

        builder.Append($"DELETE FROM `{DatabaseInterface.TableAssetBanFilters}` WHERE `{DatabaseInterface.ColumnExternalPrimaryKey}` = @0;");

        if (AssetFilter.Length > 0)
        {
            builder.Append($" INSERT INTO `{DatabaseInterface.TableAssetBanFilters}` ({SqlTypes.ColumnList(
                DatabaseInterface.ColumnExternalPrimaryKey, DatabaseInterface.ColumnAssetBanFiltersAsset)}) VALUES ");

            for (int i = 0; i < AssetFilter.Length; ++i)
            {
                F.AppendPropertyList(builder, args.Count, 1, i, 1);
                args.Add(AssetFilter[i].Key);
            }

            builder.Append(';');
        }
        
        
        builder.Append($"DELETE FROM `{DatabaseInterface.TableAssetBanTypeFilters}` WHERE `{DatabaseInterface.ColumnExternalPrimaryKey}` = @0;");

        if (VehicleTypeFilter.Length > 0)
        {
            builder.Append($" INSERT INTO `{DatabaseInterface.TableAssetBanTypeFilters}` ({SqlTypes.ColumnList(
                DatabaseInterface.ColumnExternalPrimaryKey, DatabaseInterface.ColumnAssetBanTypeFiltersType)}) VALUES ");

            for (int i = 0; i < VehicleTypeFilter.Length; ++i)
            {
                F.AppendPropertyList(builder, args.Count, 1, i, 1);
                args.Add(VehicleTypeFilter[i].ToString());
            }

            builder.Append(';');
        }

        return hasEvidenceCalls;
    }
}
