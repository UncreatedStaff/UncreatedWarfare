using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Gamemodes.Flags.Invasion;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Maps;

namespace Uncreated.Warfare.Gamemodes;

public sealed class GamemodeConfig : Config<GamemodeConfigData>
{
    public GamemodeConfig() : base(Warfare.Data.Paths.BaseDirectory, "gamemode_settings.json", "gameconfig") { }
    protected override void OnReload()
    {
        UI_CONFIG ui = Data.UI;
        Squads.SquadManager.ListUI.LoadFromConfig(ui.SquadListGUID);
        Squads.SquadManager.MenuUI.LoadFromConfig(ui.SquadMenuGUID);
        Squads.SquadManager.RallyUI.LoadFromConfig(ui.RallyGUID);
        Squads.SquadManager.OrderUI.LoadFromConfig(ui.OrderUI);
        FOBs.FOBManager.ListUI.LoadFromConfig(ui.FOBListGUID);
        FOBs.FOBManager.ResourceUI.LoadFromConfig(ui.NearbyResourcesGUID);
        CTFUI.ListUI.LoadFromConfig(ui.FlagListGUID);
        CTFUI.CaptureUI.LoadFromConfig(ui.CaptureGUID);
        Teams.TeamSelector.JoinUI.LoadFromConfig(ui.JoinUIGUID);
        UCPlayer.MutedUI.LoadFromConfig(ui.MutedUI);
        UCPlayerData.ReloadToastIDs();
        Tickets.TicketManager.TicketUI.LoadFromConfig(ui.TicketUI);
        Gamemode.WinToastUI.LoadFromConfig(ui.WinToastGUID);
        Gamemode.ReadGamemodes();
        if (Warfare.Data.Is<TeamCTF>() || Warfare.Data.Is<Invasion>())
            Warfare.Data.Gamemode.SetTiming(Data.TeamCTF.EvaluateTime);
    }
}
public class GamemodeConfigData : ConfigData
{
    public BARRICADE_IDS Barricades;
    public ITEM_IDS Items;
    public UI_CONFIG UI;
    public TEAM_CTF_CONFIG TeamCTF;
    public CONQUEST_CONFIG Conquest;
    public INVASION Invasion;
    public INSURGENCY Insurgency;
    public GENERAL_GM_CONFIG GeneralConfig;
    public GamemodeConfigData() => SetDefaults();
    public override void SetDefaults()
    {
        Barricades = new BARRICADE_IDS();
        Barricades.SetDefaults();
        Items = new ITEM_IDS();
        Items.SetDefaults();
        UI = new UI_CONFIG();
        UI.SetDefaults();
        Invasion = new INVASION();
        Invasion.SetDefaults();
        Insurgency = new INSURGENCY();
        Insurgency.SetDefaults();
        TeamCTF = new TEAM_CTF_CONFIG();
        TeamCTF.SetDefaults();
        Conquest = new CONQUEST_CONFIG();
        Conquest.SetDefaults();
        GeneralConfig = new GENERAL_GM_CONFIG();
        GeneralConfig.SetDefaults();
    }
}
public class GENERAL_GM_CONFIG
{
    public RotatableConfig<float> AMCKillTime;
    public RotatableConfig<float> LeaderboardDelay;
    public RotatableConfig<float> LeaderboardTime;
    public void SetDefaults()
    {
        AMCKillTime = 10f;
        LeaderboardDelay = 8f;
        LeaderboardTime = 30f;
    }
}

public class UI_CONFIG
{
    public RotatableConfig<JsonAssetReference<EffectAsset>> CaptureGUID;
    public RotatableConfig<JsonAssetReference<EffectAsset>> FlagListGUID;
    public RotatableConfig<JsonAssetReference<EffectAsset>> HeaderGUID;
    public RotatableConfig<JsonAssetReference<EffectAsset>> FOBListGUID;
    public RotatableConfig<JsonAssetReference<EffectAsset>> SquadListGUID;
    public RotatableConfig<JsonAssetReference<EffectAsset>> SquadMenuGUID;
    public RotatableConfig<JsonAssetReference<EffectAsset>> RallyGUID;
    public RotatableConfig<JsonAssetReference<EffectAsset>> OrderUI;
    public RotatableConfig<JsonAssetReference<EffectAsset>> MutedUI;
    public RotatableConfig<JsonAssetReference<EffectAsset>> InfoToast;
    public RotatableConfig<JsonAssetReference<EffectAsset>> WarningToast;
    public RotatableConfig<JsonAssetReference<EffectAsset>> SevereToast;
    public RotatableConfig<JsonAssetReference<EffectAsset>> XPToast;
    public RotatableConfig<JsonAssetReference<EffectAsset>> MediumToast;
    public RotatableConfig<JsonAssetReference<EffectAsset>> BigToast;
    public RotatableConfig<JsonAssetReference<EffectAsset>> ProgressToast;
    public RotatableConfig<JsonAssetReference<EffectAsset>> TipToast;
    public RotatableConfig<JsonAssetReference<EffectAsset>> InjuredUI;
    public RotatableConfig<JsonAssetReference<EffectAsset>> XPGUID;
    public RotatableConfig<JsonAssetReference<EffectAsset>> OfficerGUID;
    public RotatableConfig<JsonAssetReference<EffectAsset>> CTFLeaderboardGUID;
    public RotatableConfig<JsonAssetReference<EffectAsset>> NearbyResourcesGUID;
    public RotatableConfig<JsonAssetReference<EffectAsset>> MarkerAmmo;
    public RotatableConfig<JsonAssetReference<EffectAsset>> MarkerRepair;
    public RotatableConfig<JsonAssetReference<EffectAsset>> MarkerRadio;
    public RotatableConfig<JsonAssetReference<EffectAsset>> MarkerRadioDamaged;
    public RotatableConfig<JsonAssetReference<EffectAsset>> MarkerBunker;
    public RotatableConfig<JsonAssetReference<EffectAsset>> MarkerCacheAttack;
    public RotatableConfig<JsonAssetReference<EffectAsset>> MarkerCacheDefend;
    public RotatableConfig<JsonAssetReference<EffectAsset>> MarkerBuildable;
    public RotatableConfig<JsonAssetReference<EffectAsset>> JoinUIGUID;
    public RotatableConfig<JsonAssetReference<EffectAsset>> WinToastGUID;
    public RotatableConfig<JsonAssetReference<EffectAsset>> TicketUI;
    public bool EnablePlayerCount;
    public bool ShowPointsOnUI;
    public string ProgressChars;
    public char PlayerIcon;
    public char AttackIcon;
    public char DefendIcon;
    public char LockIcon;
    public void SetDefaults()
    {
        CaptureGUID =           new JsonAssetReference<EffectAsset>("76a9ffb4659a494080d98c8ef7733815");
        FlagListGUID =          new JsonAssetReference<EffectAsset>("c01fe46d9b794364aca6a3887a028164");
        HeaderGUID =            new JsonAssetReference<EffectAsset>("c14fe9ffee6d4f8dbe7f57885f678edd");
        FOBListGUID =           new JsonAssetReference<EffectAsset>("2c01a36943ea45189d866f5463f8e5e9");
        SquadListGUID =         new JsonAssetReference<EffectAsset>("5acd091f1e7b4f93ac9f5431729ac5cc");
        SquadMenuGUID =         new JsonAssetReference<EffectAsset>("98154002fbcd4b7499552d6497db8fc5");
        RallyGUID =             new JsonAssetReference<EffectAsset>("a280ac3fe8c1486cadc8eca331e8ce32");
        OrderUI =               new JsonAssetReference<EffectAsset>("57a08eb9c4cb4fd2ad30a3e413e29b27");
        JoinUIGUID =            new JsonAssetReference<EffectAsset>("b5924bc83eb24d7298a47f933d3f16d9");
        MutedUI =               new JsonAssetReference<EffectAsset>("c5e31c7357134be09732c1930e0e4ff0");
        InfoToast =             new JsonAssetReference<EffectAsset>("d75046834b324ed491914b4136ab1bc8");
        WarningToast =          new JsonAssetReference<EffectAsset>("5678a559695e4d999dfea9a771b6616f");
        SevereToast =           new JsonAssetReference<EffectAsset>("26fed6564ccf4c46aac1df01dbba0aab");
        XPToast =               new JsonAssetReference<EffectAsset>("a213915d61ad41cebab34fb12fe6870c");
        MediumToast =           new JsonAssetReference<EffectAsset>("5f695955f0da4d19adacac39140da797");
        BigToast =              new JsonAssetReference<EffectAsset>("9de82ffea13946b391090eb918bf3991");
        InjuredUI =             new JsonAssetReference<EffectAsset>("27b84636ed8d4c0fb557a67d89254b00");
        ProgressToast =         new JsonAssetReference<EffectAsset>("a113a0f2d0af4db8b5e5bcbc17fc96c9");
        TipToast =              new JsonAssetReference<EffectAsset>("abbf74e86f1c4665925884c70b9433ba");
        XPGUID =                new JsonAssetReference<EffectAsset>("d6de0a8025de44d29a99a41937a58a59");
        OfficerGUID =           new JsonAssetReference<EffectAsset>("9fd31b776b744b72847f2dc00dba93a8");
        CTFLeaderboardGUID =    new JsonAssetReference<EffectAsset>("b83389df1245438db18889af94f04960");
        NearbyResourcesGUID =   new JsonAssetReference<EffectAsset>("3775a1e7d84b47e79cacecd5e6b2a224");
        MarkerAmmo =            new JsonAssetReference<EffectAsset>("827b0c00724b466d8d33633fe2a7743a");
        MarkerRepair =          new JsonAssetReference<EffectAsset>("bcfda6fb871f42cd88597c8ac5f7c424");
        MarkerRadio =           new JsonAssetReference<EffectAsset>("bc6f0e7d5d9340f39ca4968bc3f7a132");
        MarkerRadioDamaged =    new JsonAssetReference<EffectAsset>("37d5c48597ea4b61a7a87ed85a4c9b39");
        MarkerBunker =          new JsonAssetReference<EffectAsset>("d7452e8671c14e93a5e9d69e077d999c");
        MarkerCacheAttack =     new JsonAssetReference<EffectAsset>("26b60044bc1442eb9d0521bfea306517");
        MarkerCacheDefend =     new JsonAssetReference<EffectAsset>("06efa2c2f9ec413aa417c717a7be3364");
        MarkerBuildable =       new JsonAssetReference<EffectAsset>("35ab4b71bfb74755b318ce62935f58c9");
        WinToastGUID =          new JsonAssetReference<EffectAsset>("1f3ce50c120042c390f5c42522bd0fcd");
        TicketUI =              new JsonAssetReference<EffectAsset>("aba88eedb84448e8a30bb803a53a7236");
        EnablePlayerCount = true;
        ShowPointsOnUI = false;
        ProgressChars = "ĀāĂăĄąĆćĈĉĊċČčĎďĐđĒēĔĕĖėĘęĚěĜĝĞğĠġĢģĤĥĦħĨĩĪīĬĭĮįİıĲĳĴĵĶķĸĹĺĻļĽľĿŀ";
        PlayerIcon = '³';
        AttackIcon = 'µ';
        DefendIcon = '´';
        LockIcon = '²';
    }
}

public class BARRICADE_IDS
{
    public RotatableConfig<JsonAssetReference<ItemBarricadeAsset>> InsurgencyCacheGUID;
    public RotatableConfig<JsonAssetReference<ItemBarricadeAsset>> FOBRadioDamagedGUID;
    public RotatableConfig<JsonAssetReference<ItemBarricadeAsset>> FOBGUID;
    public RotatableConfig<JsonAssetReference<ItemBarricadeAsset>> FOBBaseGUID;
    public RotatableConfig<JsonAssetReference<ItemBarricadeAsset>> AmmoCrateGUID;
    public RotatableConfig<JsonAssetReference<ItemBarricadeAsset>> AmmoCrateBaseGUID;
    public RotatableConfig<JsonAssetReference<ItemBarricadeAsset>> RepairStationGUID;
    public RotatableConfig<JsonAssetReference<ItemBarricadeAsset>> RepairStationBaseGUID;
    public RotatableConfig<JsonAssetReference<ItemBarricadeAsset>> AmmoBagGUID;
    public RotatableConfig<JsonAssetReference<ItemAsset>> VehicleBayGUID;
    public RotatableConfig<JsonAssetReference<ItemBarricadeAsset>[]> TimeLimitedStorages;
    public RotatableConfig<JsonAssetReference<ItemBarricadeAsset>[]> FOBRadioGUIDs;
    public RotatableConfig<JsonAssetReference<ItemBarricadeAsset>> Team1ZoneBlocker;
    public RotatableConfig<JsonAssetReference<ItemBarricadeAsset>> Team2ZoneBlocker;

    public void SetDefaults()
    {
        InsurgencyCacheGUID = new JsonAssetReference<ItemBarricadeAsset>("39051f33f24449b4b3417d0d666a4f27");
        FOBRadioDamagedGUID = new JsonAssetReference<ItemBarricadeAsset>("07e68489e3b547879fa26f94ea227522");
        FOBGUID = new JsonAssetReference<ItemBarricadeAsset>("61c349f10000498fa2b92c029d38e523");
        FOBBaseGUID = new JsonAssetReference<ItemBarricadeAsset>("1bb17277dd8148df9f4c53d1a19b2503");
        AmmoCrateGUID = new JsonAssetReference<ItemBarricadeAsset>("6fe208519d7c45b0be38273118eea7fd");
        AmmoCrateBaseGUID = new JsonAssetReference<ItemBarricadeAsset>("eccfe06e53d041d5b83c614ffa62ee59");
        RepairStationGUID = new JsonAssetReference<ItemBarricadeAsset>("c0d11e0666694ddea667377b4c0580be");
        RepairStationBaseGUID = new JsonAssetReference<ItemBarricadeAsset>("26a6b91cd1944730a0f28e5f299cebf9");
        AmmoBagGUID = new JsonAssetReference<ItemBarricadeAsset>("16f55b999e9b4f158be12645e41dd753");
        VehicleBayGUID = new JsonAssetReference<ItemAsset>("c076f9e9f35f42a4b8b5711dfb230010");
        TimeLimitedStorages = new JsonAssetReference<ItemBarricadeAsset>[]
        {
            AmmoCrateGUID,
            RepairStationGUID,
            "a2eb76590cf74401aeb7ff4b4b79fd86", // supply crate
            "2193aa0b272f4cc1938f719c8e8badb1" // supply roll
        };
        FOBRadioGUIDs = new JsonAssetReference<ItemBarricadeAsset>[]
        {
            "7715ad81f1e24f60bb8f196dd09bd4ef",
            "fb910102ad954169abd4b0cb06a112c8",
            "c7754ac78083421da73006b12a56811a"
        };
        Team1ZoneBlocker = new RotatableConfig<JsonAssetReference<ItemBarricadeAsset>>(
            new JsonAssetReference<ItemBarricadeAsset>(Guid.Empty),
            new RotatableDefaults<JsonAssetReference<ItemBarricadeAsset>>()
            {
                { MapScheduler.Nuijamaa, "57927806-0501-4735-ab01-2f1f7adaf714" },
                { MapScheduler.GulfOfAqaba, "57927806-0501-4735-ab01-2f1f7adaf714" },
            });
        Team2ZoneBlocker = new RotatableConfig<JsonAssetReference<ItemBarricadeAsset>>(
            new JsonAssetReference<ItemBarricadeAsset>(Guid.Empty),
            new RotatableDefaults<JsonAssetReference<ItemBarricadeAsset>>()
            {
                { MapScheduler.Nuijamaa, "b4c0a51b-7005-4ad5-b6fe-06aead982d94" },
                { MapScheduler.GulfOfAqaba, "b4c0a51b-7005-4ad5-b6fe-06aead982d94" },
            });
    }
}

public class ITEM_IDS
{
    public RotatableConfig<JsonAssetReference<ItemMeleeAsset>> EntrenchingTool;
    public void SetDefaults()
    {
        EntrenchingTool = new JsonAssetReference<ItemMeleeAsset>("6cee2662e8884d7bad3a5d743f8222da");
    }
}

public class TEAM_CTF_CONFIG
{
    public int StagingTime;
    public int StartingTickets;
    public float EvaluateTime;
    public int TicketXPInterval;
    public int RequiredPlayerDifferenceToCapture;
    public int OverrideContestDifference;
    public bool AllowVehicleCapture;
    public int DiscoveryForesight;
    public int FlagTickInterval;
    public int TicketsFlagCaptured;
    public int TicketsFlagLost;
    public float CaptureScale;
    public void SetDefaults()
    {
        StagingTime = 90;
        StartingTickets = 300;
        EvaluateTime = 0.25f;
        TicketXPInterval = 10;
        OverrideContestDifference = 2;
        AllowVehicleCapture = false;
        DiscoveryForesight = 2;
        FlagTickInterval = 12;
        TicketsFlagCaptured = 40;
        TicketsFlagLost = -10;
        RequiredPlayerDifferenceToCapture = 2;
        CaptureScale = 3.222f;
    }
}

public class CONQUEST_CONFIG
{
    public float EvaluateTime;
    public float CaptureScale;
    public int PointCount;
    public float FlagTickSeconds;
    public float TicketTickSeconds;

    public void SetDefaults()
    {
        EvaluateTime = 0.25f;
        CaptureScale = 3.222f;
        PointCount = 5;
        FlagTickSeconds = 4f;
        TicketTickSeconds = 8f;
    }
}
public class INVASION
{
    public int StagingTime;
    public int DiscoveryForesight;
    public string SpecialFOBName;
    public int TicketsFlagCaptured;
    public int AttackStartingTickets;
    public int TicketXPInterval;
    public float CaptureScale;
    public void SetDefaults()
    {
        StagingTime = 120;
        DiscoveryForesight = 2;
        SpecialFOBName = "VCP";
        TicketsFlagCaptured = 100;
        AttackStartingTickets = 250;
        TicketXPInterval = 10;
        CaptureScale = 3.222f;
    }
}
[JsonSerializable(typeof(INSURGENCY))]
public class INSURGENCY
{
    public int MinStartingCaches;
    public int MaxStartingCaches;
    public int StagingTime;
    public int FirstCacheSpawnTime;
    public int AttackStartingTickets;
    public int CacheDiscoverRange;
    public int IntelPointsToSpawn;
    public int IntelPointsToDiscovery;
    public int XPCacheDestroyed;
    public int XPCacheTeamkilled;
    public int TicketsCache;
    public int CacheStartingBuild;
    public Dictionary<ushort, int> CacheItems;
    public void SetDefaults()
    {
        MinStartingCaches = 3;
        MaxStartingCaches = 4;
        StagingTime = 150;
        FirstCacheSpawnTime = 240;
        AttackStartingTickets = 180;
        CacheDiscoverRange = 75;
        IntelPointsToDiscovery = 20;
        IntelPointsToSpawn = 20;
        XPCacheDestroyed = 800;
        XPCacheTeamkilled = -8000;
        TicketsCache = 70;
        CacheStartingBuild = 15;
        CacheItems = new Dictionary<ushort, int>();
    }
}
