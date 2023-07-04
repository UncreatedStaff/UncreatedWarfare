using System;
using Uncreated.SQL;

namespace Uncreated.Warfare.Moderation.Punishments;

[ModerationEntry(ModerationEntryType.AssetBan)]
public class AssetBan : DurationPunishment
{
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
    public override string GetDisplayName() => "Asset Ban";
}
