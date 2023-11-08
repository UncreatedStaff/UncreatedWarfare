using SDG.Unturned;
using System;

namespace Uncreated.Warfare.API.Assets;
public static class AssetHelper
{
    public static EAssetType GetAssetCategory<TAsset>() where TAsset : Asset => AssetCategoryHelper<TAsset>.Type;

    private static class AssetCategoryHelper<TAsset> where TAsset : Asset
    {
        public static readonly EAssetType Type;
        static AssetCategoryHelper()
        {
            Type at = typeof(TAsset);
            if (typeof(ItemAsset).IsAssignableFrom(at))
                Type = EAssetType.ITEM;
            else if (typeof(EffectAsset).IsAssignableFrom(at))
                Type = EAssetType.EFFECT;
            else if (typeof(VehicleAsset).IsAssignableFrom(at))
                Type = EAssetType.VEHICLE;
            else if (typeof(ObjectAsset).IsAssignableFrom(at))
                Type = EAssetType.OBJECT;
            else if (typeof(ResourceAsset).IsAssignableFrom(at))
                Type = EAssetType.RESOURCE;
            else if (typeof(AnimalAsset).IsAssignableFrom(at))
                Type = EAssetType.ANIMAL;
            else if (typeof(MythicAsset).IsAssignableFrom(at))
                Type = EAssetType.MYTHIC;
            else if (typeof(SkinAsset).IsAssignableFrom(at))
                Type = EAssetType.SKIN;
            else if (typeof(SpawnAsset).IsAssignableFrom(at))
                Type = EAssetType.SPAWN;
            else if (typeof(DialogueAsset).IsAssignableFrom(at) || typeof(VendorAsset).IsAssignableFrom(at) || typeof(QuestAsset).IsAssignableFrom(at))
                Type = EAssetType.NPC;
            else
                Type = EAssetType.NONE;
        }
    }
}
