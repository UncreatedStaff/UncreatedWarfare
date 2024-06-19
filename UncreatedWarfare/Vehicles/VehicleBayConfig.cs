using SDG.Unturned;
using Uncreated.Warfare.Configuration;

namespace Uncreated.Warfare.Vehicles;
public class VehicleBayConfig : Config<VehicleBayData>
{
    public VehicleBayConfig() : base(Warfare.Data.Paths.VehicleStorage, "config.json", "vbconfig") { }
}

public class VehicleBayData : JSONConfigData
{
    public IAssetLink<EffectAsset> CountermeasureEffectID { get; set; }
    public IAssetLink<VehicleAsset> CountermeasureGUID { get; set; }
    public IAssetLink<ItemGunAsset>[] TOWMissileWeapons { get; set; }
    public IAssetLink<ItemGunAsset>[] GroundAAWeapons { get; set; }
    public IAssetLink<ItemGunAsset>[] AirAAWeapons { get; set; }
    public IAssetLink<ItemGunAsset>[] LaserGuidedWeapons { get; set; }

    public override void SetDefaults()
    {
        CountermeasureEffectID = AssetLink.Create<EffectAsset>(26035);
        CountermeasureGUID = AssetLink.Create<VehicleAsset>("16dbd4e5928e498783675529ca53fc61");
        TOWMissileWeapons =
        [
            AssetLink.Create<ItemGunAsset>("f8978efc872e415bb41086fdee10d7ad"), // TOW
            AssetLink.Create<ItemGunAsset>("d900a6de5bb344f887855ce351e3bb41"), // Kornet
            AssetLink.Create<ItemGunAsset>("9ed685df6df34527b104ab227465489d"), // M2 Bradley TOW
            AssetLink.Create<ItemGunAsset>("d6312ceb00ad4530bdb07735bc02f070"), // BMP-2 Konkurs
            AssetLink.Create<ItemGunAsset>("0c12296d4e954fe0a4a82809b6ac597d"), // BMP-2 Konkurs
            AssetLink.Create<ItemGunAsset>("70e099314d1946fd84b3d35e2e5453c8"), // HJ-8
            AssetLink.Create<ItemGunAsset>("e88e40a0ba494177ba452f276e2b01ce")  // Type 86G HJ-8
        ];
        GroundAAWeapons =
        [
            AssetLink.Create<ItemGunAsset>("58b18a3fa1104ca58a7bdebef3ab6b29"), // stinger
            AssetLink.Create<ItemGunAsset>("5ae39e59d299415d8c4d08b233206302")  // igla
        ];
        AirAAWeapons =
        [
            AssetLink.Create<ItemGunAsset>("661a347f5e56406e85510a1b427bc4d6"), // F-15 AA
            AssetLink.Create<ItemGunAsset>("ad70852b3d31401b9001a13d64a13f78"), // Su-34 AA
            AssetLink.Create<ItemGunAsset>("0a58fa7fdad2470c97af71e03059f181"), // Eurofighter Typhoon AA
            AssetLink.Create<ItemGunAsset>("d9447148f8aa41f0ad885edd24ac5a02"), // J-10 AA
            AssetLink.Create<ItemGunAsset>("0b21724b3a1f40e7b88de9484a1733bc")  // JH-7 AA
        ];
        LaserGuidedWeapons =
        [
            AssetLink.Create<ItemGunAsset>("433ea5249699420eb7adb67791a98134"), // F-15 Laser Guided
            AssetLink.Create<ItemGunAsset>("3754ca2527ee40e2ad0951c8930efb07") // Su-34 Laser Guided
        ];
    }
}