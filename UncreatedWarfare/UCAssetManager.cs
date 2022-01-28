using SDG.Unturned;
using System;
using System.Linq;

namespace Uncreated.Warfare
{
    public static class UCAssetManager
    {
        public static VehicleAsset FindVehicleAsset(ushort vehicleID) => Assets.find(EAssetType.VEHICLE).Cast<VehicleAsset>().Where(k => k?.id == vehicleID).FirstOrDefault();

        public static VehicleAsset FindVehicleAsset(string vehicleName)
        {
            var assets = Assets.find(EAssetType.VEHICLE).Cast<VehicleAsset>()
                .Where(k => k?.name != null && k.vehicleName != null).OrderBy(k => k.vehicleName.Length).ToList();

            var asset = assets.FirstOrDefault(k =>
                vehicleName.Equals(k.id.ToString(Data.Locale), StringComparison.OrdinalIgnoreCase) ||
                vehicleName.Split(' ').All(l => k.vehicleName.ToLower().Contains(l)) ||
                vehicleName.Split(' ').All(l => k.name.ToLower().Contains(l))
                );

            return asset;
        }
        public static ItemAsset FindItemAsset(string itemName, out int numberOfSimilarNames, bool additionalCheckWithoutNonAlphanumericCharacters = false)
        {
            itemName = itemName.ToLower();

            numberOfSimilarNames = 0;

            var assets = Assets.find(EAssetType.ITEM).Cast<ItemAsset>()
                .Where(k => k?.name != null && k.itemName != null).OrderBy(k => k.itemName.Length).ToList();

            var selection = assets.Where(k =>
                itemName.Equals(k.id.ToString(Data.Locale), StringComparison.OrdinalIgnoreCase) ||
                itemName.Split(' ').All(l => k.itemName.ToLower().Contains(l)) ||
                itemName.Split(' ').All(l => k.name.ToLower().Contains(l))
                );

            numberOfSimilarNames = selection.Count();

            var asset = selection.FirstOrDefault();

            if (asset == null && additionalCheckWithoutNonAlphanumericCharacters)
            {
                itemName = itemName.RemoveMany(false, '.', ',', '&', '-', '_');

                selection = assets.Where(k =>
                itemName.Equals(k.id.ToString(Data.Locale), StringComparison.OrdinalIgnoreCase) ||
                itemName.Split(' ').All(l => k.itemName.ToLower().RemoveMany(false, '.', ',', '&', '-', '_').Contains(l)) ||
                itemName.Split(' ').All(l => k.name.ToLower().RemoveMany(false, '.', ',', '&', '-', '_').Contains(l))
                );

                numberOfSimilarNames = selection.Count();
                asset = selection.FirstOrDefault();
            }

            numberOfSimilarNames--;

            return asset;
        }
    }
}
