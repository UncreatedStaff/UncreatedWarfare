using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                vehicleName.Equals(k.id.ToString(), StringComparison.OrdinalIgnoreCase) ||
                vehicleName.Split(' ').All(l => k.vehicleName.ToLower().Contains(l)) ||
                vehicleName.Split(' ').All(l => k.name.ToLower().Contains(l))
                );

            return asset;
        }
    }
}
