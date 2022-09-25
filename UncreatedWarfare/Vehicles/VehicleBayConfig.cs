using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Configuration;

namespace Uncreated.Warfare.Vehicles;
public class VehicleBayConfig : Config<VehicleBayData>
{
    public VehicleBayConfig() : base(Warfare.Data.Paths.VehicleStorage, "config.json") { }
}


public class VehicleBayData : JSONConfigData
{
    public JsonAssetReference<EffectAsset> MissileWarningID;
    public JsonAssetReference<EffectAsset> MissileWarningDriverID;
    public JsonAssetReference<EffectAsset> CountermeasureEffectID;
    public JsonAssetReference<VehicleAsset> CountermeasureGUID;
    public JsonAssetReference<ItemGunAsset>[] TOWMissileWeapons;
    public JsonAssetReference<ItemGunAsset>[] GroundAAWeapons;
    public JsonAssetReference<ItemGunAsset>[] AirAAWeapons;
    public JsonAssetReference<ItemGunAsset>[] LaserGuidedWeapons;

    public override void SetDefaults()
    {
        MissileWarningID       = (JsonAssetReference<EffectAsset>)26033;
        MissileWarningDriverID = (JsonAssetReference<EffectAsset>)26034;
        CountermeasureEffectID = (JsonAssetReference<EffectAsset>)26035;
        CountermeasureGUID     = "16dbd4e5928e498783675529ca53fc61";
        TOWMissileWeapons = new JsonAssetReference<ItemGunAsset>[]
        {
            "f8978efc872e415bb41086fdee10d7ad", // TOW
            "d900a6de5bb344f887855ce351e3bb41", // Kornet
            "9ed685df6df34527b104ab227465489d", // M2 Bradley TOW
            "d6312ceb00ad4530bdb07735bc02f070"  // BMP-2 Konkurs
        };
        GroundAAWeapons = new JsonAssetReference<ItemGunAsset>[]
        {
            "58b18a3fa1104ca58a7bdebef3ab6b29", // stinger
            "5ae39e59d299415d8c4d08b233206302"  // igla
        };
        AirAAWeapons = new JsonAssetReference<ItemGunAsset>[]
        {
            "661a347f5e56406e85510a1b427bc4d6", // F-15 AA
            "ad70852b3d31401b9001a13d64a13f78"  // Su-34 AA
        };
        LaserGuidedWeapons = new JsonAssetReference<ItemGunAsset>[]
        {
            "433ea5249699420eb7adb67791a98134", // F-15 Laser Guided
            "3754ca2527ee40e2ad0951c8930efb07", // Su-34 Laser Guided
        };
    }
}