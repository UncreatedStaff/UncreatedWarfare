using SDG.Unturned;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Kits;

namespace Uncreated.Warfare.Squads;
public sealed class SquadsConfig : Config<SquadConfigData>
{
    public SquadsConfig() : base(Warfare.Data.Paths.SquadStorage, "config.json")
    {
    }
    protected override void OnReload()
    {
        SquadManager.MenuUI.LoadFromConfig(Data.SquadMenuUI);
        SquadManager.ListUI.LoadFromConfig(Data.SquadListUI);
        SquadManager.RallyUI.LoadFromConfig(Data.SquadRallyUI);
    }
}
[JsonSerializable(typeof(SquadConfigData))]
public class SquadConfigData : JSONConfigData
{
    public ushort RallyTimer;
    public float RallyDespawnDistance;
    public int SquadDisconnectTime;
    public float MedicRange;
    public ClassConfig[] Classes;
    public JsonAssetReference<EffectAsset> EmptyMarker;
    public JsonAssetReference<EffectAsset> SquadLeaderEmptyMarker;
    public JsonAssetReference<EffectAsset> MortarMarker;
    public JsonAssetReference<EffectAsset> InjuredMarker;
    public JsonAssetReference<EffectAsset> MedicMarker;
    public JsonAssetReference<EffectAsset> SquadMenuUI;
    public JsonAssetReference<EffectAsset> SquadListUI;
    public JsonAssetReference<EffectAsset> SquadRallyUI;
    public override void SetDefaults()
    {
        RallyTimer = 45;
        RallyDespawnDistance = 30;
        MedicRange = 300f;
        SquadDisconnectTime = 120;

        EmptyMarker = "84d38753c46e4a1b8e5b26ff44787ef2";
        SquadLeaderEmptyMarker = "dc95d06e787e4a069518e0487645ed6b";
        MortarMarker = "e608e0add55d4f3cbfbd6f5cabe091f3";
        InjuredMarker = "5edcd9bb87e240428902c04e1ba04e97";
        MedicMarker = "0d846b64766041f6b9f1a08df88a9268";

        Classes = new ClassConfig[]
        {
            new ClassConfig(EClass.NONE,               '±', "28b4d205725c42be9a816346200ba1d8", "fc661ae0d8eb4fb3a2dcdee3b8fb6070"),
            new ClassConfig(EClass.UNARMED,            '±', "28b4d205725c42be9a816346200ba1d8", "fc661ae0d8eb4fb3a2dcdee3b8fb6070"),
            new ClassConfig(EClass.SQUADLEADER,        '¦', "44e8988ace914a37b7997c12a8d9f187", "be6b04b9cedf4d54b4878b4ee10ff0d5"),
            new ClassConfig(EClass.RIFLEMAN,           '¡', "08e8efff011e497ba953652f2197a3fb", "7e10f895582b446b9bb265cfb402ae55"),
            new ClassConfig(EClass.MEDIC,              '¢', "93ad1da8a885422984a9abe4e36aa169", "575bafd3f3c246a0a67a06a303d68d63"),
            new ClassConfig(EClass.BREACHER,           '¤', "69602a83758b40ae9a0c9e241a4d7a26", "db94515c46c144019510790f4857e50d"),
            new ClassConfig(EClass.AUTOMATIC_RIFLEMAN, '¥', "413210b4424e489bb94fcacd71882c9a", "46998d84c5b347f3a02fbc283b52a765"),
            new ClassConfig(EClass.GRENADIER,          '¬', "8f1a42df30824ba3b927f290fd26be64", "b2b1a257f7e94bde909c51ba0a5e7bae"),
            new ClassConfig(EClass.MACHINE_GUNNER,     '«', "68045950f00a46cda6f6bf4f5ee45e2d", "fc6aabbda77344f8be249f9de2e9266a"),
            new ClassConfig(EClass.LAT,                '®', "b4f3fbde3dbd4c48a6de689352bab1ce", "2c5b3129f1b942b0864a90adb7248b7d"),
            new ClassConfig(EClass.HAT,                '¯', "fb187276ea3e43e282541dd2d4cc56c1", "be372546261b45c09269fd550977794f"),
            new ClassConfig(EClass.MARKSMAN,           '¨', "04fc36ed64864c3c933d1535ae685229", "a78823ec4a734f1c94ec02e4feba189e"),
            new ClassConfig(EClass.SNIPER,             '£', "1f22b297324f491d8e397a43671c954e", "adbec23553e24a6eba10e9dbf981352b"),
            new ClassConfig(EClass.AP_RIFLEMAN,        '©', "e697bf94b9d74e02ba6f89acce5830fb", "65a2a7e1eb434649bc70e3e3be424571"),
            new ClassConfig(EClass.COMBAT_ENGINEER,    'ª', "b677dde97254483380a31c7a78152501", "c5771f8815584f56ae3791aa839f6201"),
            new ClassConfig(EClass.CREWMAN,            '§', "36d38dbde16c4e76ba4322430696cb7d", "7626dcd783434a0b850527ca7c9c6cc2"),
            new ClassConfig(EClass.PILOT,              '°', "c29f7ce8667b4daa976c81531379007f", "a37bafeb17264b1f9ebae34dd06537a1"),
            new ClassConfig(EClass.SPEC_OPS,           '×', "a1ebe4722dc24da5a7eef7604436c9e3", "21fe1dcba8bf49caa3f7928fc2bb3f36")
        };

        SquadMenuUI = "98154002fbcd4b7499552d6497db8fc5";
        SquadListUI = "5acd091f1e7b4f93ac9f5431729ac5cc";
        SquadRallyUI = "a280ac3fe8c1486cadc8eca331e8ce32";
    }
}

public struct ClassConfig
{
    [JsonPropertyName("class")]
    public EClass Class;
    [JsonPropertyName("icon")]
    public char Icon;
    [JsonPropertyName("marker_effect")]
    public JsonAssetReference<EffectAsset> MarkerEffect;
    [JsonPropertyName("marker_effect_squad_leader")]
    public JsonAssetReference<EffectAsset> SquadLeaderMarkerEffect;
    public ClassConfig(EClass @class, char icon, JsonAssetReference<EffectAsset> markerEffect, JsonAssetReference<EffectAsset> squadLeaderMarkerEffect)
    {
        this.Class = @class;
        this.Icon = icon;
        this.MarkerEffect = markerEffect;
        this.SquadLeaderMarkerEffect = squadLeaderMarkerEffect ?? markerEffect;
    }
}