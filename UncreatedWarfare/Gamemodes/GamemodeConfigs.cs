using SDG.Unturned;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Actions;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes.Flags.Invasion;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Levels;
using Uncreated.Warfare.Moderation;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Tickets;

namespace Uncreated.Warfare.Gamemodes;

public sealed class GamemodeConfig : Config<GamemodeConfigData>
{
    public GamemodeConfig() : base(Warfare.Data.Paths.BaseDirectory, "gamemode_settings.json", "gameconfig") { }

    /// <inheritdoc />
    protected override void OnReload()
    {
        ToastManager.ReloadToastIds();
        KitManager.MenuUI.LoadFromConfig(Data.UIKitMenu);
        VehicleComponent.VehicleHUD.LoadFromConfig(Data.UIVehicleHUD);
        Gamemode.WinToastUI.LoadFromConfig(Data.UIToastWin);
        ActionManager.UI.LoadFromConfig(Data.UIActionMenu);
        UCPlayer.LoadingUI.LoadFromConfig(Data.UILoading);
        UCPlayer.MutedUI.LoadFromConfig(Data.UIMuted);
        TicketManager.TicketUI.LoadFromConfig(Data.UITickets);
        TeamSelector.JoinUI.LoadFromConfig(Data.UITeamSelector);
        SquadManager.MenuUI.LoadFromConfig(Data.UISquadMenu);
        SquadManager.ListUI.LoadFromConfig(Data.UISquadList);
        SquadManager.RallyUI.LoadFromConfig(Data.UIRally);
        FOBManager.ResourceUI.LoadFromConfig(Data.UINearbyResources);
        FOBManager.ListUI.LoadFromConfig(Data.UIFOBList);
        CTFUI.CaptureUI.LoadFromConfig(Data.UICapture);
        CTFUI.ListUI.LoadFromConfig(Data.UIFlagList);
        Points.XPUI.LoadFromConfig(Data.UIXPPanel);
        Points.CreditsUI.LoadFromConfig(Data.UICreditsPanel);
        ModerationUI.Instance.LoadFromConfig(Data.UICreditsPanel);
        if (Warfare.Data.Is<Flags.Conquest>())
            Warfare.Data.Gamemode.SetTiming(Data.ConquestEvaluateTime);
        if (Warfare.Data.Is<TeamCTF>() || Warfare.Data.Is<Invasion>())
            Warfare.Data.Gamemode.SetTiming(Data.AASEvaluateTime);
    }
}

public sealed class GamemodeConfigData : JSONConfigData
{
    #region Barricades and Structures
    [JsonPropertyName("barricade_insurgency_cache")]
    public IAssetLink<ItemBarricadeAsset> BarricadeInsurgencyCache { get; set; }

    [JsonPropertyName("barricade_fob_radio_damaged")]
    public IAssetLink<ItemBarricadeAsset> BarricadeFOBRadioDamaged { get; set; }

    [JsonPropertyName("barricade_fob_bunker")]
    public IAssetLink<ItemBarricadeAsset> BarricadeFOBBunker { get; set; }

    [JsonPropertyName("barricade_fob_bunker_base")]
    public IAssetLink<ItemBarricadeAsset> BarricadeFOBBunkerBase { get; set; }

    [JsonPropertyName("barricade_fob_ammo_crate")]
    public IAssetLink<ItemBarricadeAsset> BarricadeAmmoCrate { get; set; }

    [JsonPropertyName("barricade_fob_ammo_crate_base")]
    public IAssetLink<ItemBarricadeAsset> BarricadeAmmoCrateBase { get; set; }
    
    [JsonPropertyName("barricade_fob_repair_station")]
    public IAssetLink<ItemBarricadeAsset> BarricadeRepairStation { get; set; }

    [JsonPropertyName("barricade_fob_repair_station_base")]
    public IAssetLink<ItemBarricadeAsset> BarricadeRepairStationBase { get; set; }

    [JsonPropertyName("barricade_ammo_bag")]
    public IAssetLink<ItemBarricadeAsset> BarricadeAmmoBag { get; set; }

    [JsonPropertyName("barricade_uav")]
    public IAssetLink<ItemBarricadeAsset> BarricadeUAV { get; set; }

    [JsonPropertyName("barricade_time_restricted_storages")]
    public IAssetLink<ItemBarricadeAsset>[] TimeLimitedStorages { get; set; }

    [JsonPropertyName("barricade_fob_radios")]
    public IAssetLink<ItemBarricadeAsset>[] FOBRadios { get; set; }

    [JsonPropertyName("barricade_rallypoints")]
    public IAssetLink<ItemBarricadeAsset>[] RallyPoints { get; set; }

    [JsonPropertyName("structure_vehicle_bay")]
    public IAssetLink<ItemAsset> StructureVehicleBay { get; set; }
    #endregion

    #region Items
    [JsonPropertyName("item_entrenching_tool")]
    public IAssetLink<ItemMeleeAsset> ItemEntrenchingTool { get; set; }

    [JsonPropertyName("item_laser_designator")]
    public IAssetLink<ItemGunAsset> ItemLaserDesignator { get; set; }

    [JsonPropertyName("item_april_fools_barrel")]
    public IAssetLink<ItemBarrelAsset> ItemAprilFoolsBarrel { get; set; }
    #endregion

    #region UI and Effects
    [JsonPropertyName("ui_capture")]
    public IAssetLink<EffectAsset> UICapture { get; set; }

    [JsonPropertyName("ui_flag_list")]
    public IAssetLink<EffectAsset> UIFlagList { get; set; }

    [JsonPropertyName("ui_header")]
    public IAssetLink<EffectAsset> UIHeader { get; set; }

    [JsonPropertyName("ui_fob_list")]
    public IAssetLink<EffectAsset> UIFOBList { get; set; }

    [JsonPropertyName("ui_squad_list")]
    public IAssetLink<EffectAsset> UISquadList { get; set; }

    [JsonPropertyName("ui_squad_menu")]
    public IAssetLink<EffectAsset> UISquadMenu { get; set; }

    [JsonPropertyName("ui_rally")]
    public IAssetLink<EffectAsset> UIRally { get; set; }

    [JsonPropertyName("ui_muted")]
    public IAssetLink<EffectAsset> UIMuted { get; set; }

    [JsonPropertyName("ui_injured")]
    public IAssetLink<EffectAsset> UIInjured { get; set; }

    [JsonPropertyName("ui_xp_panel")]
    public IAssetLink<EffectAsset> UIXPPanel { get; set; }

    [JsonPropertyName("ui_xp_officer")]
    public IAssetLink<EffectAsset> UICreditsPanel { get; set; }

    [JsonPropertyName("ui_leaderboard_conventional")]
    public IAssetLink<EffectAsset> UIConventionalLeaderboard { get; set; }

    [JsonPropertyName("ui_nearby_resources")]
    public IAssetLink<EffectAsset> UINearbyResources { get; set; }

    [JsonPropertyName("ui_team_selector")]
    public IAssetLink<EffectAsset> UITeamSelector { get; set; }

    [JsonPropertyName("ui_tickets")]
    public IAssetLink<EffectAsset> UITickets { get; set; }

    [JsonPropertyName("ui_buffs")]
    public IAssetLink<EffectAsset> UIBuffs { get; set; }

    [JsonPropertyName("ui_kit_menu")]
    public IAssetLink<EffectAsset> UIKitMenu { get; set; }

    [JsonPropertyName("ui_vehicle_hud")]
    public IAssetLink<EffectAsset> UIVehicleHUD { get; set; }

    [JsonPropertyName("ui_toast_xp")]
    public IAssetLink<EffectAsset> UIToastXP { get; set; }

    [JsonPropertyName("ui_toast_medium")]
    public IAssetLink<EffectAsset> UIToastMedium { get; set; }

    [JsonPropertyName("ui_toast_large")]
    public IAssetLink<EffectAsset> UIToastLarge { get; set; }

    [JsonPropertyName("ui_toast_progress")]
    public IAssetLink<EffectAsset> UIToastProgress { get; set; }

    [JsonPropertyName("ui_toast_tip")]
    public IAssetLink<EffectAsset> UIToastTip { get; set; }

    [JsonPropertyName("ui_toast_win")]
    public IAssetLink<EffectAsset> UIToastWin { get; set; }

    [JsonPropertyName("ui_action_menu")]
    public IAssetLink<EffectAsset> UIActionMenu { get; set; }

    [JsonPropertyName("ui_loading")]
    public IAssetLink<EffectAsset> UILoading { get; set; }

    [JsonPropertyName("ui_incoming_mortar_warning")]
    public IAssetLink<EffectAsset> UIFlashingWarning { get; set; }

    [JsonPropertyName("ui_moderation_menu")]
    public IAssetLink<EffectAsset> UIModerationMenu { get; set; }

    [JsonPropertyName("ui_popup")]
    public IAssetLink<EffectAsset> UIPopup { get; set; }

    [JsonPropertyName("effect_spotted_marker_infantry")]
    public IAssetLink<EffectAsset> EffectSpottedMarkerInfantry { get; set; }

    [JsonPropertyName("effect_spotted_marker_fob")]
    public IAssetLink<EffectAsset> EffectSpottedMarkerFOB { get; set; }

    [JsonPropertyName("effect_spotted_marker_aa")]
    public IAssetLink<EffectAsset> EffectSpottedMarkerAA { get; set; }

    [JsonPropertyName("effect_spotted_marker_apc")]
    public IAssetLink<EffectAsset> EffectSpottedMarkerAPC { get; set; }

    [JsonPropertyName("effect_spotted_marker_atgm")]
    public IAssetLink<EffectAsset> EffectSpottedMarkerATGM { get; set; }

    [JsonPropertyName("effect_spotted_marker_attack_heli")]
    public IAssetLink<EffectAsset> EffectSpottedMarkerAttackHeli { get; set; }

    [JsonPropertyName("effect_spotted_marker_hmg")]
    public IAssetLink<EffectAsset> EffectSpottedMarkerHMG { get; set; }

    [JsonPropertyName("effect_spotted_marker_humvee")]
    public IAssetLink<EffectAsset> EffectSpottedMarkerHumvee { get; set; }

    [JsonPropertyName("effect_spotted_marker_ifv")]
    public IAssetLink<EffectAsset> EffectSpottedMarkerIFV { get; set; }

    [JsonPropertyName("effect_spotted_marker_jet")]
    public IAssetLink<EffectAsset> EffectSpottedMarkerJet { get; set; }

    [JsonPropertyName("effect_spotted_marker_mbt")]
    public IAssetLink<EffectAsset> EffectSpottedMarkerMBT { get; set; }

    [JsonPropertyName("effect_spotted_marker_mortar")]
    public IAssetLink<EffectAsset> EffectSpottedMarkerMortar { get; set; }

    [JsonPropertyName("effect_spotted_marker_scout_car")]
    public IAssetLink<EffectAsset> EffectSpottedMarkerScoutCar { get; set; }

    [JsonPropertyName("effect_spotted_marker_transport_air")]
    public IAssetLink<EffectAsset> EffectSpottedMarkerTransportAir { get; set; }

    [JsonPropertyName("effect_spotted_marker_logistics_ground")]
    public IAssetLink<EffectAsset> EffectSpottedMarkerLogisticsGround { get; set; }

    [JsonPropertyName("effect_spotted_marker_transport_ground")]
    public IAssetLink<EffectAsset> EffectSpottedMarkerTransportGround { get; set; }

    [JsonPropertyName("effect_marker_ammo")]
    public IAssetLink<EffectAsset> EffectMarkerAmmo { get; set; }

    [JsonPropertyName("effect_marker_repair")]
    public IAssetLink<EffectAsset> EffectMarkerRepair { get; set; }

    [JsonPropertyName("effect_marker_radio")]
    public IAssetLink<EffectAsset> EffectMarkerRadio { get; set; }

    [JsonPropertyName("effect_marker_radio_damaged")]
    public IAssetLink<EffectAsset> EffectMarkerRadioDamaged { get; set; }

    [JsonPropertyName("effect_marker_bunker")]
    public IAssetLink<EffectAsset> EffectMarkerBunker { get; set; }

    [JsonPropertyName("effect_marker_cache_attack")]
    public IAssetLink<EffectAsset> EffectMarkerCacheAttack { get; set; }

    [JsonPropertyName("effect_marker_cache_defend")]
    public IAssetLink<EffectAsset> EffectMarkerCacheDefend { get; set; }

    [JsonPropertyName("effect_marker_buildable")]
    public IAssetLink<EffectAsset> EffectMarkerBuildable { get; set; }

    [JsonPropertyName("effect_action_need_medic")]
    public IAssetLink<EffectAsset> EffectActionNeedMedic { get; set; }

    [JsonPropertyName("effect_action_nearby_medic")]
    public IAssetLink<EffectAsset> EffectActionNearbyMedic { get; set; }

    [JsonPropertyName("effect_action_need_ammo")]
    public IAssetLink<EffectAsset> EffectActionNeedAmmo { get; set; }

    [JsonPropertyName("effect_action_nearby_ammo")]
    public IAssetLink<EffectAsset> EffectActionNearbyAmmo { get; set; }

    [JsonPropertyName("effect_action_need_ride")]
    public IAssetLink<EffectAsset> EffectActionNeedRide { get; set; }

    [JsonPropertyName("effect_action_need_support")]
    public IAssetLink<EffectAsset> EffectActionNeedSupport { get; set; }

    [JsonPropertyName("effect_action_heli_pickup")]
    public IAssetLink<EffectAsset> EffectActionHeliPickup { get; set; }

    [JsonPropertyName("effect_action_heli_dropoff")]
    public IAssetLink<EffectAsset> EffectActionHeliDropoff { get; set; }

    [JsonPropertyName("effect_action_supplies_build")]
    public IAssetLink<EffectAsset> EffectActionSuppliesBuild { get; set; }

    [JsonPropertyName("effect_action_supplies_ammo")]
    public IAssetLink<EffectAsset> EffectActionSuppliesAmmo { get; set; }

    [JsonPropertyName("effect_action_air_support")]
    public IAssetLink<EffectAsset> EffectActionAirSupport { get; set; }

    [JsonPropertyName("effect_action_armor_support")]
    public IAssetLink<EffectAsset> EffectActionArmorSupport { get; set; }

    [JsonPropertyName("effect_action_attack")]
    public IAssetLink<EffectAsset> EffectActionAttack { get; set; }

    [JsonPropertyName("effect_action_defend")]
    public IAssetLink<EffectAsset> EffectActionDefend { get; set; }

    [JsonPropertyName("effect_action_move")]
    public IAssetLink<EffectAsset> EffectActionMove { get; set; }

    [JsonPropertyName("effect_action_build")]
    public IAssetLink<EffectAsset> EffectActionBuild { get; set; }

    [JsonPropertyName("effect_unload_ammo")]
    public IAssetLink<EffectAsset> EffectUnloadAmmo { get; set; }

    [JsonPropertyName("effect_unload_build")]
    public IAssetLink<EffectAsset> EffectUnloadBuild { get; set; }

    [JsonPropertyName("effect_unlock_vehicle")]
    public IAssetLink<EffectAsset> EffectUnlockVehicle { get; set; }

    [JsonPropertyName("effect_dig")]
    public IAssetLink<EffectAsset> EffectDig { get; set; }

    [JsonPropertyName("effect_repair")]
    public IAssetLink<EffectAsset> EffectRepair { get; set; }

    [JsonPropertyName("effect_refuel")]
    public IAssetLink<EffectAsset> EffectRefuel { get; set; }

    [JsonPropertyName("effect_laser_guided_no_sound")]
    public IAssetLink<EffectAsset> EffectLaserGuidedNoSound { get; set; }

    [JsonPropertyName("effect_laser_guided_sound")]
    public IAssetLink<EffectAsset> EffectLaserGuidedSound { get; set; }

    [JsonPropertyName("effect_guided_missile_no_sound")]
    public IAssetLink<EffectAsset> EffectGuidedMissileNoSound { get; set; }

    [JsonPropertyName("effect_guided_missile_sound")]
    public IAssetLink<EffectAsset> EffectGuidedMissileSound { get; set; }

    [JsonPropertyName("effect_heat_seeking_missile_no_sound")]
    public IAssetLink<EffectAsset> EffectHeatSeekingMissileNoSound { get; set; }

    [JsonPropertyName("effect_heat_seeking_missile_sound")]
    public IAssetLink<EffectAsset> EffectHeatSeekingMissileSound { get; set; }

    [JsonPropertyName("effect_purchase")]
    public IAssetLink<EffectAsset> EffectPurchase { get; set; }

    [JsonPropertyName("effect_ammo")]
    public IAssetLink<EffectAsset> EffectAmmo { get; set; }

    [JsonPropertyName("effect_build_success")]
    public IAssetLink<EffectAsset> EffectBuildSuccess { get; set; }

    [JsonPropertyName("ui_capture_enable_player_count")]
    public bool UICaptureEnablePlayerCount { get; set; }

    [JsonPropertyName("ui_capture_show_point_count")]
    public bool UICaptureShowPointCount { get; set; }

    [JsonPropertyName("ui_circle_font_characters")]
    public string UICircleFontCharacters { get; set; }

    [JsonPropertyName("ui_icon_spotted")]
    public string UIIconSpotted { get; set; }

    [JsonPropertyName("ui_icon_vehicle_spotted")]
    public string UIIconVehicleSpotted { get; set; }

    [JsonPropertyName("ui_icon_uav")]
    public string UIIconUAV { get; set; }

    [JsonPropertyName("ui_icon_player")]
    public char UIIconPlayer { get; set; }

    [JsonPropertyName("ui_icon_attack")]
    public char UIIconAttack { get; set; }

    [JsonPropertyName("ui_icon_defend")]
    public char UIIconDefend { get; set; }

    [JsonPropertyName("ui_icon_locked")]
    public char UIIconLocked { get; set; }

    [JsonPropertyName("effect_lock_on_1")]
    public IAssetLink<EffectAsset> EffectLockOn1 { get; set; }

    [JsonPropertyName("effect_lock_on_2")]
    public IAssetLink<EffectAsset> EffectLockOn2 { get; set; }
    #endregion

    #region General Gamemode Config
    [JsonPropertyName("general_amc_kill_time")]
    public float GeneralAMCKillTime { get; set; }

    [JsonPropertyName("general_leaderboard_delay")]
    public float GeneralLeaderboardDelay { get; set; }

    [JsonPropertyName("general_leaderboard_open_time")]
    public float GeneralLeaderboardTime { get; set; }

    [JsonPropertyName("general_uav_start_delay")]
    public float GeneralUAVStartDelay { get; set; }

    [JsonPropertyName("general_uav_radius")]
    public float GeneralUAVRadius { get; set; }

    /// <summary>In sq m per second.</summary>
    [JsonPropertyName("general_uav_scan_speed")]
    public float GeneralUAVScanSpeed { get; set; }

    [JsonPropertyName("general_uav_alive_time")]
    public float GeneralUAVAliveTime { get; set; }

    [JsonPropertyName("general_allow_crafting_ammo")]
    public bool GeneralAllowCraftingAmmo { get; set; }

    [JsonPropertyName("general_allow_crafting_repair")]
    public bool GeneralAllowCraftingRepair { get; set; }

    [JsonPropertyName("general_allow_crafting_others")]
    public bool GeneralAllowCraftingOthers { get; set; }

    [JsonPropertyName("general_main_check_seconds")]
    public float GeneralMainCheckSeconds { get; set; }

    [JsonPropertyName("general_amc_dmg_power")]
    public double GeneralAMCDamageMultiplierPower { get; set; }
    #endregion

    #region Advance and Secure
    [JsonPropertyName("aas_staging_time")]
    public int AASStagingTime { get; set; }

    [JsonPropertyName("aas_starting_tickets")]
    public int AASStartingTickets { get; set; }

    [JsonPropertyName("aas_evaluate_time")]
    public float AASEvaluateTime { get; set; }

    [JsonPropertyName("aas_ticket_xp_interval")]
    public int AASTicketXPInterval { get; set; }

    [JsonPropertyName("aas_required_capturing_player_difference")]
    public int AASRequiredCapturingPlayerDifference { get; set; }

    [JsonPropertyName("aas_override_contest_difference")]
    public int AASOverrideContestDifference { get; set; }

    [JsonPropertyName("aas_allow_vehicle_capture")]
    public bool AASAllowVehicleCapture { get; set; }

    [JsonPropertyName("aas_discovery_foresight")]
    public int AASDiscoveryForesight { get; set; }

    [JsonPropertyName("aas_flag_tick_interval")]
    public float AASFlagTickSeconds { get; set; }

    [JsonPropertyName("aas_tickets_flag_captured")]
    public int AASTicketsFlagCaptured { get; set; }

    [JsonPropertyName("aas_tickets_flag_lost")]
    public int AASTicketsFlagLost { get; set; }

    [JsonPropertyName("aas_capture_scale")]
    public float AASCaptureScale { get; set; }
    #endregion

    #region Conquest
    [JsonPropertyName("conquest_evaluate_time")]
    public float ConquestEvaluateTime { get; set; }

    [JsonPropertyName("conquest_capture_scale")]
    public float ConquestCaptureScale { get; set; }

    [JsonPropertyName("conquest_point_count_low_pop")]
    public int ConquestPointCountLowPop { get; set; }
    
    [JsonPropertyName("conquest_point_count_medium_pop")]
    public int ConquestPointCountMediumPop { get; set; }
    
    [JsonPropertyName("conquest_point_count_high_pop")]
    public int ConquestPointCountHighPop { get; set; }

    [JsonPropertyName("conquest_flag_tick_seconds")]
    public float ConquestFlagTickSeconds { get; set; }

    [JsonPropertyName("conquest_ticket_bleed_interval_per_point")]
    public float ConquestTicketBleedIntervalPerPoint { get; set; }

    [JsonPropertyName("conquest_staging_phase_seconds")]
    public int ConquestStagingPhaseSeconds { get; set; }

    [JsonPropertyName("conquest_starting_tickets")]
    public int ConquestStartingTickets { get; set; }
    #endregion

    #region Invasion
    [JsonPropertyName("invasion_staging_time")]
    public int InvasionStagingTime { get; set; }

    [JsonPropertyName("invasion_discovery_foresight")]
    public int InvasionDiscoveryForesight { get; set; }

    [JsonPropertyName("invasion_special_fob_name")]
    public string InvasionSpecialFOBName { get; set; }

    [JsonPropertyName("invasion_tickets_flag_captured")]
    public int InvasionTicketsFlagCaptured { get; set; }

    [JsonPropertyName("invasion_starting_tickets_attack")]
    public int InvasionAttackStartingTickets { get; set; }

    [JsonPropertyName("invasion_ticket_xp_interval")]
    public int InvasionTicketXPInterval { get; set; }

    [JsonPropertyName("invasion_capture_scale")]
    public float InvasionCaptureScale { get; set; }
    #endregion

    #region Insurgency
    [JsonPropertyName("insurgency_starting_caches_min")]
    public int InsurgencyMinStartingCaches { get; set; }

    [JsonPropertyName("insurgency_starting_caches_max")]
    public int InsurgencyMaxStartingCaches { get; set; }

    [JsonPropertyName("insurgency_staging_time")]
    public int InsurgencyStagingTime { get; set; }

    [JsonPropertyName("insurgency_first_cache_spawn_time")]
    public int InsurgencyFirstCacheSpawnTime { get; set; }

    [JsonPropertyName("insurgency_starting_tickets_attack")]
    public int InsurgencyAttackStartingTickets { get; set; }

    [JsonPropertyName("insurgency_discover_range")]
    public int InsurgencyCacheDiscoverRange { get; set; }

    [JsonPropertyName("insurgency_intel_to_spawn")]
    public int InsurgencyIntelPointsToSpawn { get; set; }

    [JsonPropertyName("insurgency_intel_to_discover")]
    public int InsurgencyIntelPointsToDiscovery { get; set; }

    [JsonPropertyName("insurgency_tickets_cache")]
    public int InsurgencyTicketsCache { get; set; }

    [JsonPropertyName("insurgency_starting_build")]
    public int InsurgencyCacheStartingBuild { get; set; }
    #endregion

    #region Hardpoint
    [JsonPropertyName("hardpoint_flag_tick_interval")]
    public float HardpointFlagTickSeconds { get; set; }
    
    [JsonPropertyName("hardpoint_flag_amount")]
    public int HardpointFlagAmount { get; set; }

    [JsonPropertyName("hardpoint_flag_tolerance")]
    public int HardpointFlagTolerance { get; set; }

    [JsonPropertyName("hardpoint_objective_change_time_seconds")]
    public float HardpointObjectiveChangeTime { get; set; }

    [JsonPropertyName("hardpoint_objective_change_time_tolerance")]
    public float HardpointObjectiveChangeTimeTolerance { get; set; }

    [JsonPropertyName("hardpoint_ticket_tick_seconds")]
    public float HardpointTicketTickSeconds { get; set; }

    [JsonPropertyName("hardpoint_starting_tickets")]
    public int HardpointStartingTickets { get; set; }
    
    [JsonPropertyName("hardpoint_staging_phase_seconds")]
    public float HardpointStagingPhaseSeconds { get; set; }
    #endregion

    public override void SetDefaults()
    {
        #region Barricades and Structures
        BarricadeInsurgencyCache = AssetLink.Create<ItemBarricadeAsset>("39051f33f24449b4b3417d0d666a4f27");
        BarricadeFOBRadioDamaged = AssetLink.Create<ItemBarricadeAsset>("07e68489e3b547879fa26f94ea227522");
        BarricadeFOBBunker = AssetLink.Create<ItemBarricadeAsset>("61c349f10000498fa2b92c029d38e523");
        BarricadeFOBBunkerBase = AssetLink.Create<ItemBarricadeAsset>("1bb17277dd8148df9f4c53d1a19b2503");
        BarricadeAmmoCrate = AssetLink.Create<ItemBarricadeAsset>("6fe208519d7c45b0be38273118eea7fd");
        BarricadeAmmoCrateBase = AssetLink.Create<ItemBarricadeAsset>("eccfe06e53d041d5b83c614ffa62ee59");
        BarricadeRepairStation = AssetLink.Create<ItemBarricadeAsset>("c0d11e0666694ddea667377b4c0580be");
        BarricadeRepairStationBase = AssetLink.Create<ItemBarricadeAsset>("26a6b91cd1944730a0f28e5f299cebf9");
        BarricadeAmmoBag = AssetLink.Create<ItemBarricadeAsset>("16f55b999e9b4f158be12645e41dd753");
        StructureVehicleBay = AssetLink.Create<ItemAsset>("c076f9e9f35f42a4b8b5711dfb230010");
        BarricadeUAV = AssetLink.Create<ItemBarricadeAsset>("fb8f84e2617b480aadfd77bbf4a6c3ec");
        TimeLimitedStorages =
        [
            BarricadeAmmoCrate,
            BarricadeRepairStation,
            AssetLink.Create<ItemBarricadeAsset>("a2eb76590cf74401aeb7ff4b4b79fd86"), // supply crate
            AssetLink.Create<ItemBarricadeAsset>("2193aa0b272f4cc1938f719c8e8badb1")  // supply roll
        ];
        FOBRadios =
        [
            AssetLink.Create<ItemBarricadeAsset>("7715ad81f1e24f60bb8f196dd09bd4ef"), // USA
            AssetLink.Create<ItemBarricadeAsset>("fb910102ad954169abd4b0cb06a112c8"), // Russia
            AssetLink.Create<ItemBarricadeAsset>("c7754ac78083421da73006b12a56811a"), // MEC
            AssetLink.Create<ItemBarricadeAsset>("439c32cced234f358e101294ea0ce3e4"), // Germany
            AssetLink.Create<ItemBarricadeAsset>("7bde55f70c494418bdd81926fb7d6359")  //China
        ];
        RallyPoints =
        [
            AssetLink.Create<ItemBarricadeAsset>("5e1db525179341d3b0c7576876212a81"), // USA
            AssetLink.Create<ItemBarricadeAsset>("0d7895360c80440fbe4a45eba28b2007"), // Russia
            AssetLink.Create<ItemBarricadeAsset>("c03352d9e6bb4e2993917924b604ee76"), // MEC
            AssetLink.Create<ItemBarricadeAsset>("49663078b594410b98b8a51e8eff3609"), // Germany
            AssetLink.Create<ItemBarricadeAsset>("7720ced42dba4c1eac16d14453cd8bc4")  //China
        ];
        #endregion

        #region Items
        ItemEntrenchingTool = AssetLink.Create<ItemMeleeAsset>("6cee2662e8884d7bad3a5d743f8222da");
        ItemLaserDesignator = AssetLink.Create<ItemGunAsset>("3879d9014aca4a17b3ed749cf7a9283e");
        ItemAprilFoolsBarrel = AssetLink.Create<ItemBarrelAsset>("c3d3123823334847a9fd294e5d764889");
        #endregion

        #region UI and Effects
        UICapture = AssetLink.Create<EffectAsset>("76a9ffb4659a494080d98c8ef7733815");
        UIFlagList = AssetLink.Create<EffectAsset>("c01fe46d9b794364aca6a3887a028164");
        UIHeader = AssetLink.Create<EffectAsset>("c14fe9ffee6d4f8dbe7f57885f678edd");
        UIFOBList = AssetLink.Create<EffectAsset>("2c01a36943ea45189d866f5463f8e5e9");
        UISquadList = AssetLink.Create<EffectAsset>("5acd091f1e7b4f93ac9f5431729ac5cc");
        UISquadMenu = AssetLink.Create<EffectAsset>("98154002fbcd4b7499552d6497db8fc5");
        UIRally = AssetLink.Create<EffectAsset>("a280ac3fe8c1486cadc8eca331e8ce32");
        UITeamSelector = AssetLink.Create<EffectAsset>("b5924bc83eb24d7298a47f933d3f16d9");
        UIMuted = AssetLink.Create<EffectAsset>("c5e31c7357134be09732c1930e0e4ff0");
        UIInjured = AssetLink.Create<EffectAsset>("27b84636ed8d4c0fb557a67d89254b00");
        UIToastProgress = AssetLink.Create<EffectAsset>("a113a0f2d0af4db8b5e5bcbc17fc96c9");
        UIToastTip = AssetLink.Create<EffectAsset>("abbf74e86f1c4665925884c70b9433ba");
        UIXPPanel = AssetLink.Create<EffectAsset>("d6de0a8025de44d29a99a41937a58a59");
        UICreditsPanel = AssetLink.Create<EffectAsset>("3195b96457d04b9e80699777d2809b4c");
        UIConventionalLeaderboard = AssetLink.Create<EffectAsset>("b83389df1245438db18889af94f04960");
        UINearbyResources = AssetLink.Create<EffectAsset>("3775a1e7d84b47e79cacecd5e6b2a224");
        UITickets = AssetLink.Create<EffectAsset>("aba88eedb84448e8a30bb803a53a7236");
        UIBuffs = AssetLink.Create<EffectAsset>("f298af0b4d34405b98a539b8d2ff0505");
        UIActionMenu = AssetLink.Create<EffectAsset>("bf4bc4e1a6a849c29e9f7a6de3a943e4");
        UIToastXP = AssetLink.Create<EffectAsset>("a213915d61ad41cebab34fb12fe6870c");
        UIToastMedium = AssetLink.Create<EffectAsset>("5f695955f0da4d19adacac39140da797");
        UIToastLarge = AssetLink.Create<EffectAsset>("9de82ffea13946b391090eb918bf3991");
        UIToastWin = AssetLink.Create<EffectAsset>("1f3ce50c120042c390f5c42522bd0fcd");
        UIKitMenu = AssetLink.Create<EffectAsset>("c0155ea486d8427d9c70541abc875e78");
        UIVehicleHUD = AssetLink.Create<EffectAsset>("1e1762d6f01442e89d159d4cd0ae7587");
        UIFlashingWarning = AssetLink.Create<EffectAsset>("4f8a6ca089a7499793c6076da9807273");
        UIModerationMenu = AssetLink.Create<EffectAsset>("80aee6c3f43f4c7facb2f2ffbb545d20");
        UIPopup = AssetLink.Create<EffectAsset>("bc6bbe8ce1d2464bb828d01ba1c5d461");
        EffectSpottedMarkerInfantry = AssetLink.Create<EffectAsset>("79add0f1b07c478f87207d30fe5a5f4f");
        EffectSpottedMarkerFOB = AssetLink.Create<EffectAsset>("39dce42142074b46b819feba9ce83353");
        EffectSpottedMarkerAA = AssetLink.Create<EffectAsset>("0e90e68eff624456b76fee28a4875d14");
        EffectSpottedMarkerAPC = AssetLink.Create<EffectAsset>("31d1404b7b3a465b8631308cdb48e3b2");
        EffectSpottedMarkerATGM = AssetLink.Create<EffectAsset>("b20a7d914f92492fb1588f7baac80239");
        EffectSpottedMarkerAttackHeli = AssetLink.Create<EffectAsset>("3f2c6776ba484f8ea443719161ec6ce5");
        EffectSpottedMarkerHMG = AssetLink.Create<EffectAsset>("2315e6ed970542499fec1b06df87ffd2");
        EffectSpottedMarkerHumvee = AssetLink.Create<EffectAsset>("99a84b82f9bd433891fdb99e80394bf3");
        EffectSpottedMarkerIFV = AssetLink.Create<EffectAsset>("f2c29856b4f64146afd9872ab528c242");
        EffectSpottedMarkerJet = AssetLink.Create<EffectAsset>("08f2cc6ed558459ea2caf3477b40df64");
        EffectSpottedMarkerMBT = AssetLink.Create<EffectAsset>("983c6510c13042bf983e81f49cffca39");
        EffectSpottedMarkerMortar = AssetLink.Create<EffectAsset>("c377810f849c4c7d84391b491406918b");
        EffectSpottedMarkerScoutCar = AssetLink.Create<EffectAsset>("b0937aff90b94a588b70bc96ece49f53");
        EffectSpottedMarkerTransportAir = AssetLink.Create<EffectAsset>("91b9f175b84849268d861eb0f0567788");
        EffectSpottedMarkerLogisticsGround = AssetLink.Create<EffectAsset>("fa226268e87b4ec89664eca5b22b4d3d");
        EffectSpottedMarkerTransportGround = AssetLink.Create<EffectAsset>("fa226268e87b4ec89664eca5b22b4d3d");
        EffectMarkerAmmo = AssetLink.Create<EffectAsset>("827b0c00724b466d8d33633fe2a7743a");
        EffectMarkerRepair = AssetLink.Create<EffectAsset>("bcfda6fb871f42cd88597c8ac5f7c424");
        EffectMarkerRadio = AssetLink.Create<EffectAsset>("bc6f0e7d5d9340f39ca4968bc3f7a132");
        EffectMarkerRadioDamaged = AssetLink.Create<EffectAsset>("37d5c48597ea4b61a7a87ed85a4c9b39");
        EffectMarkerBunker = AssetLink.Create<EffectAsset>("d7452e8671c14e93a5e9d69e077d999c");
        EffectMarkerCacheAttack = AssetLink.Create<EffectAsset>("26b60044bc1442eb9d0521bfea306517");
        EffectMarkerCacheDefend = AssetLink.Create<EffectAsset>("06efa2c2f9ec413aa417c717a7be3364");
        EffectMarkerBuildable = AssetLink.Create<EffectAsset>("35ab4b71bfb74755b318ce62935f58c9");
        EffectActionNeedMedic = AssetLink.Create<EffectAsset>("4d9167abea4f4f009c6db4417e7efcdf");
        EffectActionNearbyMedic = AssetLink.Create<EffectAsset>("ab5cb35e4ae14411a6190a87e72d50ba");
        EffectActionNeedAmmo = AssetLink.Create<EffectAsset>("6e7dadbfafbe46ecbe565499f901670f");
        EffectActionNearbyAmmo = AssetLink.Create<EffectAsset>("8caccb6344924fbab842625e5b1f5932");
        EffectActionNeedRide = AssetLink.Create<EffectAsset>("2a4748c3e5464b2d93132e2ed15b57b2");
        EffectActionNeedSupport = AssetLink.Create<EffectAsset>("eec285561d6040c7bbfcfa6b48f4b5ba");
        EffectActionHeliPickup = AssetLink.Create<EffectAsset>("b6d1841936824065a99bcaa8152a7877");
        EffectActionHeliDropoff = AssetLink.Create<EffectAsset>("c320d02aca914efb96dfbda6663940c5");
        EffectActionSuppliesBuild = AssetLink.Create<EffectAsset>("d9861b9123bb4da9a72851b20ecab236");
        EffectActionSuppliesAmmo = AssetLink.Create<EffectAsset>("50dbb9c23ae647b8adb829a771742d4c");
        EffectActionAirSupport = AssetLink.Create<EffectAsset>("706671c9e5c24bae8eb74c9c4631cc59");
        EffectActionArmorSupport = AssetLink.Create<EffectAsset>("637f0f25fd4b4180a04b2ecd1541ca37");
        EffectActionAttack = AssetLink.Create<EffectAsset>("7ae60fdeeff447fdb1f0eb6582537f12");
        EffectActionDefend = AssetLink.Create<EffectAsset>("16371fab7e8247619c6a6ec9a3e48e41");
        EffectActionMove = AssetLink.Create<EffectAsset>("4077d5eea255435d8ed0133ec833b86a");
        EffectActionBuild = AssetLink.Create<EffectAsset>("793bd80007f3484882284a6994e80bb3");
        EffectUnloadAmmo = AssetLink.Create<EffectAsset>("8a2740cfc6f64ca68410145a83027735");
        EffectUnloadBuild = AssetLink.Create<EffectAsset>("066c35a3e97e476a9f0a75218b4f6683");
        EffectUnlockVehicle = AssetLink.Create<EffectAsset>("bc41e0feaebe4e788a3612811b8722d3");
        EffectDig = AssetLink.Create<EffectAsset>("f894dff1d7ef453887182accd14dc9f9");
        EffectRepair = AssetLink.Create<EffectAsset>("84347b13028340b8976033c08675d458");
        EffectRefuel = AssetLink.Create<EffectAsset>("1d4fa25996114b7b89b061021fcc688f");
        EffectLaserGuidedNoSound = AssetLink.Create<EffectAsset>("64c3a204acd441679b9322be04016cbf");
        EffectLaserGuidedSound = AssetLink.Create<EffectAsset>("12209d06336142f8a0c4e49999759189");
        EffectGuidedMissileNoSound = AssetLink.Create<EffectAsset>("5e7e9379872a40059cdc7e6d189d10dd");
        EffectGuidedMissileSound = AssetLink.Create<EffectAsset>("7b458028c9de4a449c30ed5fc201bd37");
        EffectHeatSeekingMissileNoSound = AssetLink.Create<EffectAsset>("39abdc11cd68477fa3c9b44aaf299760");
        EffectHeatSeekingMissileSound = AssetLink.Create<EffectAsset>("5552f714ca744ab7bd0687fba0d541d3");
        EffectLockOn1 = AssetLink.Create<EffectAsset>("45d4cf4e11664402b8cce2808a7b8d91");
        EffectLockOn2 = AssetLink.Create<EffectAsset>("022fb707288a4e3cb6847fdd242e4092");
        EffectPurchase = AssetLink.Create<EffectAsset>("5e2a0073025849d39322932d88609777");
        EffectAmmo = AssetLink.Create<EffectAsset>("03caec479dd2475c92e1c326e1720140");
        EffectBuildSuccess = AssetLink.Create<EffectAsset>("43c2ae01755540d4b99ce076aa6731eb");
        UICaptureEnablePlayerCount = true;
        UICaptureShowPointCount = false;
        UICircleFontCharacters = "ĀāĂăĄąĆćĈĉĊċČčĎďĐđĒēĔĕĖėĘęĚěĜĝĞğĠġĢģĤĥĦħĨĩĪīĬĭĮįİıĲĳĴĵĶķĸĹĺĻļĽľĿŀ";
        UIIconPlayer = '³';
        UIIconAttack = 'µ';
        UIIconDefend = '´';
        UIIconLocked = '²';
        UIIconUAV = "ŀ";
        UIIconSpotted = "£";
        UIIconVehicleSpotted = "£";
        #endregion

        #region General Gamemode Config
        GeneralAMCDamageMultiplierPower = 2f;
        GeneralMainCheckSeconds = 0.25f;
        GeneralAMCKillTime = 10f;
        GeneralLeaderboardDelay = 8f;
        GeneralLeaderboardTime = 30f;
        GeneralUAVStartDelay = 15f;
        GeneralUAVRadius = 350f;
        GeneralUAVScanSpeed = 38484.51f;
        GeneralUAVAliveTime = 60f;
        GeneralAllowCraftingAmmo = true;
        GeneralAllowCraftingRepair = true;
        GeneralAllowCraftingOthers = false;
        #endregion

        #region Advance and Secure
        AASStagingTime = 90;
        AASStartingTickets = 300;
        AASEvaluateTime = 0.25f;
        AASTicketXPInterval = 10;
        AASOverrideContestDifference = 2;
        AASAllowVehicleCapture = false;
        AASDiscoveryForesight = 2;
        AASFlagTickSeconds = 4f;
        AASTicketsFlagCaptured = 40;
        AASTicketsFlagLost = -10;
        AASRequiredCapturingPlayerDifference = 2;
        AASCaptureScale = 3.222f;
        #endregion

        #region Conquest
        ConquestEvaluateTime = 0.25f;
        ConquestCaptureScale = 3.222f;
        ConquestPointCountLowPop = 3;
        ConquestPointCountMediumPop = 3;
        ConquestPointCountHighPop = 4;
        ConquestFlagTickSeconds = 4f;
        ConquestTicketBleedIntervalPerPoint = 12f;
        ConquestStagingPhaseSeconds = 60;
        ConquestStartingTickets = 250;
        #endregion

        #region Invasion
        InvasionStagingTime = 120;
        InvasionDiscoveryForesight = 2;
        InvasionSpecialFOBName = "VCP";
        InvasionTicketsFlagCaptured = 100;
        InvasionAttackStartingTickets = 250;
        InvasionTicketXPInterval = 10;
        InvasionCaptureScale = 3.222f;
        #endregion

        #region Insurgency
        InsurgencyMinStartingCaches = 3;
        InsurgencyMaxStartingCaches = 4;
        InsurgencyStagingTime = 150;
        InsurgencyFirstCacheSpawnTime = 240;
        InsurgencyAttackStartingTickets = 180;
        InsurgencyCacheDiscoverRange = 75;
        InsurgencyIntelPointsToDiscovery = 20;
        InsurgencyIntelPointsToSpawn = 20;
        InsurgencyTicketsCache = 70;
        InsurgencyCacheStartingBuild = 15;
        #endregion

        #region Hardpoint
        HardpointFlagTickSeconds = 2f;
        HardpointFlagAmount = 6;
        HardpointFlagTolerance = 1;
        HardpointObjectiveChangeTime = 450;
        HardpointObjectiveChangeTimeTolerance = 90;
        HardpointTicketTickSeconds = 45f;
        HardpointStartingTickets = 300;
        HardpointStagingPhaseSeconds = 60;
        #endregion
    }
}