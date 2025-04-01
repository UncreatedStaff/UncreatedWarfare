using SDG.Unturned;
using System.IO;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Actions;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes.Flags.Invasion;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Levels;
using Uncreated.Warfare.Maps;
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
        ActionManager.ActionMenuUI.LoadFromConfig(Data.UIActionMenu);
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
    public RotatableConfig<JsonAssetReference<ItemBarricadeAsset>> BarricadeInsurgencyCache { get; set; }

    [JsonPropertyName("barricade_fob_radio_damaged")]
    public RotatableConfig<JsonAssetReference<ItemBarricadeAsset>> BarricadeFOBRadioDamaged { get; set; }

    [JsonPropertyName("barricade_fob_bunker")]
    public RotatableConfig<JsonAssetReference<ItemBarricadeAsset>> BarricadeFOBBunker { get; set; }

    [JsonPropertyName("barricade_fob_bunker_base")]
    public RotatableConfig<JsonAssetReference<ItemBarricadeAsset>> BarricadeFOBBunkerBase { get; set; }

    [JsonPropertyName("barricade_fob_ammo_crate")]
    public RotatableConfig<JsonAssetReference<ItemBarricadeAsset>> BarricadeAmmoCrate { get; set; }

    [JsonPropertyName("barricade_fob_ammo_crate_base")]
    public RotatableConfig<JsonAssetReference<ItemBarricadeAsset>> BarricadeAmmoCrateBase { get; set; }
    
    [JsonPropertyName("barricade_fob_repair_station")]
    public RotatableConfig<JsonAssetReference<ItemBarricadeAsset>> BarricadeRepairStation { get; set; }

    [JsonPropertyName("barricade_fob_repair_station_base")]
    public RotatableConfig<JsonAssetReference<ItemBarricadeAsset>> BarricadeRepairStationBase { get; set; }

    [JsonPropertyName("barricade_ammo_bag")]
    public RotatableConfig<JsonAssetReference<ItemBarricadeAsset>> BarricadeAmmoBag { get; set; }

    [JsonPropertyName("barricade_uav")]
    public RotatableConfig<JsonAssetReference<ItemBarricadeAsset>> BarricadeUAV { get; set; }

    [JsonPropertyName("barricade_time_restricted_storages")]
    public RotatableConfig<JsonAssetReference<ItemBarricadeAsset>[]> TimeLimitedStorages { get; set; }

    [JsonPropertyName("barricade_fob_radios")]
    public RotatableConfig<JsonAssetReference<ItemBarricadeAsset>[]> FOBRadios { get; set; }

    [JsonPropertyName("barricade_rallypoints")]
    public RotatableConfig<JsonAssetReference<ItemBarricadeAsset>[]> RallyPoints { get; set; }

    [JsonPropertyName("structure_vehicle_bay")]
    public RotatableConfig<JsonAssetReference<ItemAsset>> StructureVehicleBay { get; set; }
    #endregion

    #region Items
    [JsonPropertyName("item_entrenching_tool")]
    public RotatableConfig<JsonAssetReference<ItemMeleeAsset>> ItemEntrenchingTool { get; set; }

    [JsonPropertyName("item_laser_designator")]
    public RotatableConfig<JsonAssetReference<ItemGunAsset>> ItemLaserDesignator { get; set; }

    [JsonPropertyName("item_april_fools_barrel")]
    public RotatableConfig<JsonAssetReference<ItemBarrelAsset>> ItemAprilFoolsBarrel { get; set; }
    #endregion

    #region UI and Effects
    [JsonPropertyName("ui_capture")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> UICapture { get; set; }

    [JsonPropertyName("ui_flag_list")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> UIFlagList { get; set; }

    [JsonPropertyName("ui_header")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> UIHeader { get; set; }

    [JsonPropertyName("ui_fob_list")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> UIFOBList { get; set; }

    [JsonPropertyName("ui_squad_list")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> UISquadList { get; set; }

    [JsonPropertyName("ui_squad_menu")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> UISquadMenu { get; set; }

    [JsonPropertyName("ui_rally")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> UIRally { get; set; }

    [JsonPropertyName("ui_muted")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> UIMuted { get; set; }

    [JsonPropertyName("ui_injured")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> UIInjured { get; set; }

    [JsonPropertyName("ui_xp_panel")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> UIXPPanel { get; set; }

    [JsonPropertyName("ui_xp_officer")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> UICreditsPanel { get; set; }

    [JsonPropertyName("ui_leaderboard_conventional")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> UIConventionalLeaderboard { get; set; }

    [JsonPropertyName("ui_nearby_resources")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> UINearbyResources { get; set; }

    [JsonPropertyName("ui_team_selector")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> UITeamSelector { get; set; }

    [JsonPropertyName("ui_tickets")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> UITickets { get; set; }

    [JsonPropertyName("ui_buffs")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> UIBuffs { get; set; }

    [JsonPropertyName("ui_kit_menu")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> UIKitMenu { get; set; }

    [JsonPropertyName("ui_vehicle_hud")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> UIVehicleHUD { get; set; }

    [JsonPropertyName("ui_toast_xp")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> UIToastXP { get; set; }

    [JsonPropertyName("ui_toast_medium")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> UIToastMedium { get; set; }

    [JsonPropertyName("ui_toast_large")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> UIToastLarge { get; set; }

    [JsonPropertyName("ui_toast_progress")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> UIToastProgress { get; set; }

    [JsonPropertyName("ui_toast_tip")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> UIToastTip { get; set; }

    [JsonPropertyName("ui_toast_win")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> UIToastWin { get; set; }

    [JsonPropertyName("ui_action_menu")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> UIActionMenu { get; set; }

    [JsonPropertyName("ui_loading")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> UILoading { get; set; }

    [JsonPropertyName("ui_incoming_mortar_warning")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> UIFlashingWarning { get; set; }

    [JsonPropertyName("ui_moderation_menu")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> UIModerationMenu { get; set; }

    [JsonPropertyName("ui_popup")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> UIPopup { get; set; }

    [JsonPropertyName("effect_spotted_marker_infantry")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> EffectSpottedMarkerInfantry { get; set; }

    [JsonPropertyName("effect_spotted_marker_fob")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> EffectSpottedMarkerFOB { get; set; }

    [JsonPropertyName("effect_spotted_marker_aa")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> EffectSpottedMarkerAA { get; set; }

    [JsonPropertyName("effect_spotted_marker_apc")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> EffectSpottedMarkerAPC { get; set; }

    [JsonPropertyName("effect_spotted_marker_atgm")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> EffectSpottedMarkerATGM { get; set; }

    [JsonPropertyName("effect_spotted_marker_attack_heli")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> EffectSpottedMarkerAttackHeli { get; set; }

    [JsonPropertyName("effect_spotted_marker_hmg")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> EffectSpottedMarkerHMG { get; set; }

    [JsonPropertyName("effect_spotted_marker_humvee")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> EffectSpottedMarkerHumvee { get; set; }

    [JsonPropertyName("effect_spotted_marker_ifv")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> EffectSpottedMarkerIFV { get; set; }

    [JsonPropertyName("effect_spotted_marker_jet")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> EffectSpottedMarkerJet { get; set; }

    [JsonPropertyName("effect_spotted_marker_mbt")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> EffectSpottedMarkerMBT { get; set; }

    [JsonPropertyName("effect_spotted_marker_mortar")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> EffectSpottedMarkerMortar { get; set; }

    [JsonPropertyName("effect_spotted_marker_scout_car")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> EffectSpottedMarkerScoutCar { get; set; }

    [JsonPropertyName("effect_spotted_marker_transport_air")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> EffectSpottedMarkerTransportAir { get; set; }

    [JsonPropertyName("effect_spotted_marker_logistics_ground")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> EffectSpottedMarkerLogisticsGround { get; set; }

    [JsonPropertyName("effect_spotted_marker_transport_ground")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> EffectSpottedMarkerTransportGround { get; set; }

    [JsonPropertyName("effect_marker_ammo")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> EffectMarkerAmmo { get; set; }

    [JsonPropertyName("effect_marker_repair")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> EffectMarkerRepair { get; set; }

    [JsonPropertyName("effect_marker_radio")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> EffectMarkerRadio { get; set; }

    [JsonPropertyName("effect_marker_radio_damaged")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> EffectMarkerRadioDamaged { get; set; }

    [JsonPropertyName("effect_marker_bunker")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> EffectMarkerBunker { get; set; }

    [JsonPropertyName("effect_marker_cache_attack")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> EffectMarkerCacheAttack { get; set; }

    [JsonPropertyName("effect_marker_cache_defend")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> EffectMarkerCacheDefend { get; set; }

    [JsonPropertyName("effect_marker_buildable")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> EffectMarkerBuildable { get; set; }

    [JsonPropertyName("effect_action_need_medic")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> EffectActionNeedMedic { get; set; }

    [JsonPropertyName("effect_action_nearby_medic")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> EffectActionNearbyMedic { get; set; }

    [JsonPropertyName("effect_action_need_ammo")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> EffectActionNeedAmmo { get; set; }

    [JsonPropertyName("effect_action_nearby_ammo")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> EffectActionNearbyAmmo { get; set; }

    [JsonPropertyName("effect_action_need_ride")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> EffectActionNeedRide { get; set; }

    [JsonPropertyName("effect_action_need_support")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> EffectActionNeedSupport { get; set; }

    [JsonPropertyName("effect_action_heli_pickup")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> EffectActionHeliPickup { get; set; }

    [JsonPropertyName("effect_action_heli_dropoff")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> EffectActionHeliDropoff { get; set; }

    [JsonPropertyName("effect_action_supplies_build")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> EffectActionSuppliesBuild { get; set; }

    [JsonPropertyName("effect_action_supplies_ammo")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> EffectActionSuppliesAmmo { get; set; }

    [JsonPropertyName("effect_action_air_support")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> EffectActionAirSupport { get; set; }

    [JsonPropertyName("effect_action_armor_support")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> EffectActionArmorSupport { get; set; }

    [JsonPropertyName("effect_action_attack")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> EffectActionAttack { get; set; }

    [JsonPropertyName("effect_action_defend")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> EffectActionDefend { get; set; }

    [JsonPropertyName("effect_action_move")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> EffectActionMove { get; set; }

    [JsonPropertyName("effect_action_build")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> EffectActionBuild { get; set; }

    [JsonPropertyName("effect_unload_ammo")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> EffectUnloadAmmo { get; set; }

    [JsonPropertyName("effect_unload_build")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> EffectUnloadBuild { get; set; }

    [JsonPropertyName("effect_unlock_vehicle")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> EffectUnlockVehicle { get; set; }

    [JsonPropertyName("effect_dig")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> EffectDig { get; set; }

    [JsonPropertyName("effect_repair")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> EffectRepair { get; set; }

    [JsonPropertyName("effect_refuel")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> EffectRefuel { get; set; }

    [JsonPropertyName("effect_laser_guided_no_sound")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> EffectLaserGuidedNoSound { get; set; }

    [JsonPropertyName("effect_laser_guided_sound")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> EffectLaserGuidedSound { get; set; }

    [JsonPropertyName("effect_guided_missile_no_sound")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> EffectGuidedMissileNoSound { get; set; }

    [JsonPropertyName("effect_guided_missile_sound")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> EffectGuidedMissileSound { get; set; }

    [JsonPropertyName("effect_heat_seeking_missile_no_sound")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> EffectHeatSeekingMissileNoSound { get; set; }

    [JsonPropertyName("effect_heat_seeking_missile_sound")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> EffectHeatSeekingMissileSound { get; set; }

    [JsonPropertyName("effect_purchase")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> EffectPurchase { get; set; }

    [JsonPropertyName("effect_ammo")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> EffectAmmo { get; set; }

    [JsonPropertyName("effect_build_success")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> EffectBuildSuccess { get; set; }

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
    public RotatableConfig<JsonAssetReference<EffectAsset>> EffectLockOn1 { get; set; }

    [JsonPropertyName("effect_lock_on_2")]
    public RotatableConfig<JsonAssetReference<EffectAsset>> EffectLockOn2 { get; set; }
    #endregion

    #region General Gamemode Config
    [JsonPropertyName("general_amc_kill_time")]
    public RotatableConfig<float> GeneralAMCKillTime { get; set; }

    [JsonPropertyName("general_leaderboard_delay")]
    public RotatableConfig<float> GeneralLeaderboardDelay { get; set; }

    [JsonPropertyName("general_leaderboard_open_time")]
    public RotatableConfig<float> GeneralLeaderboardTime { get; set; }

    [JsonPropertyName("general_uav_start_delay")]
    public RotatableConfig<float> GeneralUAVStartDelay { get; set; }

    [JsonPropertyName("general_uav_radius")]
    public RotatableConfig<float> GeneralUAVRadius { get; set; }

    /// <summary>In sq m per second.</summary>
    [JsonPropertyName("general_uav_scan_speed")]
    public RotatableConfig<float> GeneralUAVScanSpeed { get; set; }

    [JsonPropertyName("general_uav_alive_time")]
    public RotatableConfig<float> GeneralUAVAliveTime { get; set; }

    [JsonPropertyName("general_allow_crafting_ammo")]
    public RotatableConfig<bool> GeneralAllowCraftingAmmo { get; set; }

    [JsonPropertyName("general_allow_crafting_repair")]
    public RotatableConfig<bool> GeneralAllowCraftingRepair { get; set; }

    [JsonPropertyName("general_allow_crafting_others")]
    public RotatableConfig<bool> GeneralAllowCraftingOthers { get; set; }

    [JsonPropertyName("general_main_check_seconds")]
    public RotatableConfig<float> GeneralMainCheckSeconds { get; set; }

    [JsonPropertyName("general_amc_dmg_power")]
    public RotatableConfig<double> GeneralAMCDamageMultiplierPower { get; set; }
    #endregion

    #region Advance and Secure
    [JsonPropertyName("aas_staging_time")]
    public RotatableConfig<int> AASStagingTime { get; set; }

    [JsonPropertyName("aas_starting_tickets")]
    public RotatableConfig<int> AASStartingTickets { get; set; }

    [JsonPropertyName("aas_evaluate_time")]
    public RotatableConfig<float> AASEvaluateTime { get; set; }

    [JsonPropertyName("aas_ticket_xp_interval")]
    public RotatableConfig<int> AASTicketXPInterval { get; set; }

    [JsonPropertyName("aas_required_capturing_player_difference")]
    public RotatableConfig<int> AASRequiredCapturingPlayerDifference { get; set; }

    [JsonPropertyName("aas_override_contest_difference")]
    public RotatableConfig<int> AASOverrideContestDifference { get; set; }

    [JsonPropertyName("aas_allow_vehicle_capture")]
    public RotatableConfig<bool> AASAllowVehicleCapture { get; set; }

    [JsonPropertyName("aas_discovery_foresight")]
    public RotatableConfig<int> AASDiscoveryForesight { get; set; }

    [JsonPropertyName("aas_flag_tick_interval")]
    public RotatableConfig<float> AASFlagTickSeconds { get; set; }

    [JsonPropertyName("aas_tickets_flag_captured")]
    public RotatableConfig<int> AASTicketsFlagCaptured { get; set; }

    [JsonPropertyName("aas_tickets_flag_lost")]
    public RotatableConfig<int> AASTicketsFlagLost { get; set; }

    [JsonPropertyName("aas_capture_scale")]
    public RotatableConfig<float> AASCaptureScale { get; set; }
    #endregion

    #region Conquest
    [JsonPropertyName("conquest_evaluate_time")]
    public RotatableConfig<float> ConquestEvaluateTime { get; set; }

    [JsonPropertyName("conquest_capture_scale")]
    public RotatableConfig<float> ConquestCaptureScale { get; set; }

    [JsonPropertyName("conquest_point_count_low_pop")]
    public RotatableConfig<int> ConquestPointCountLowPop { get; set; }
    
    [JsonPropertyName("conquest_point_count_medium_pop")]
    public RotatableConfig<int> ConquestPointCountMediumPop { get; set; }
    
    [JsonPropertyName("conquest_point_count_high_pop")]
    public RotatableConfig<int> ConquestPointCountHighPop { get; set; }

    [JsonPropertyName("conquest_flag_tick_seconds")]
    public RotatableConfig<float> ConquestFlagTickSeconds { get; set; }

    [JsonPropertyName("conquest_ticket_bleed_interval_per_point")]
    public RotatableConfig<float> ConquestTicketBleedIntervalPerPoint { get; set; }

    [JsonPropertyName("conquest_staging_phase_seconds")]
    public RotatableConfig<int> ConquestStagingPhaseSeconds { get; set; }

    [JsonPropertyName("conquest_starting_tickets")]
    public RotatableConfig<int> ConquestStartingTickets { get; set; }
    #endregion

    #region Invasion
    [JsonPropertyName("invasion_staging_time")]
    public RotatableConfig<int> InvasionStagingTime { get; set; }

    [JsonPropertyName("invasion_discovery_foresight")]
    public RotatableConfig<int> InvasionDiscoveryForesight { get; set; }

    [JsonPropertyName("invasion_special_fob_name")]
    public RotatableConfig<string> InvasionSpecialFOBName { get; set; }

    [JsonPropertyName("invasion_tickets_flag_captured")]
    public RotatableConfig<int> InvasionTicketsFlagCaptured { get; set; }

    [JsonPropertyName("invasion_starting_tickets_attack")]
    public RotatableConfig<int> InvasionAttackStartingTickets { get; set; }

    [JsonPropertyName("invasion_ticket_xp_interval")]
    public RotatableConfig<int> InvasionTicketXPInterval { get; set; }

    [JsonPropertyName("invasion_capture_scale")]
    public RotatableConfig<float> InvasionCaptureScale { get; set; }
    #endregion

    #region Insurgency
    [JsonPropertyName("insurgency_starting_caches_min")]
    public RotatableConfig<int> InsurgencyMinStartingCaches { get; set; }

    [JsonPropertyName("insurgency_starting_caches_max")]
    public RotatableConfig<int> InsurgencyMaxStartingCaches { get; set; }

    [JsonPropertyName("insurgency_staging_time")]
    public RotatableConfig<int> InsurgencyStagingTime { get; set; }

    [JsonPropertyName("insurgency_first_cache_spawn_time")]
    public RotatableConfig<int> InsurgencyFirstCacheSpawnTime { get; set; }

    [JsonPropertyName("insurgency_starting_tickets_attack")]
    public RotatableConfig<int> InsurgencyAttackStartingTickets { get; set; }

    [JsonPropertyName("insurgency_discover_range")]
    public RotatableConfig<int> InsurgencyCacheDiscoverRange { get; set; }

    [JsonPropertyName("insurgency_intel_to_spawn")]
    public RotatableConfig<int> InsurgencyIntelPointsToSpawn { get; set; }

    [JsonPropertyName("insurgency_intel_to_discover")]
    public RotatableConfig<int> InsurgencyIntelPointsToDiscovery { get; set; }

    [JsonPropertyName("insurgency_tickets_cache")]
    public RotatableConfig<int> InsurgencyTicketsCache { get; set; }

    [JsonPropertyName("insurgency_starting_build")]
    public RotatableConfig<int> InsurgencyCacheStartingBuild { get; set; }
    #endregion

    #region Hardpoint
    [JsonPropertyName("hardpoint_flag_tick_interval")]
    public RotatableConfig<float> HardpointFlagTickSeconds { get; set; }
    
    [JsonPropertyName("hardpoint_flag_amount")]
    public RotatableConfig<int> HardpointFlagAmount { get; set; }

    [JsonPropertyName("hardpoint_flag_tolerance")]
    public RotatableConfig<int> HardpointFlagTolerance { get; set; }

    [JsonPropertyName("hardpoint_objective_change_time_seconds")]
    public RotatableConfig<float> HardpointObjectiveChangeTime { get; set; }

    [JsonPropertyName("hardpoint_objective_change_time_tolerance")]
    public RotatableConfig<float> HardpointObjectiveChangeTimeTolerance { get; set; }

    [JsonPropertyName("hardpoint_ticket_tick_seconds")]
    public RotatableConfig<float> HardpointTicketTickSeconds { get; set; }

    [JsonPropertyName("hardpoint_starting_tickets")]
    public RotatableConfig<int> HardpointStartingTickets { get; set; }
    
    [JsonPropertyName("hardpoint_staging_phase_seconds")]
    public RotatableConfig<float> HardpointStagingPhaseSeconds { get; set; }
    #endregion

    public override void SetDefaults()
    {
        #region Barricades and Structures
        BarricadeInsurgencyCache = new JsonAssetReference<ItemBarricadeAsset>("39051f33f24449b4b3417d0d666a4f27");
        BarricadeFOBRadioDamaged = new JsonAssetReference<ItemBarricadeAsset>("07e68489e3b547879fa26f94ea227522");
        BarricadeFOBBunker = new JsonAssetReference<ItemBarricadeAsset>("61c349f10000498fa2b92c029d38e523");
        BarricadeFOBBunkerBase = new JsonAssetReference<ItemBarricadeAsset>("1bb17277dd8148df9f4c53d1a19b2503");
        BarricadeAmmoCrate = new JsonAssetReference<ItemBarricadeAsset>("6fe208519d7c45b0be38273118eea7fd");
        BarricadeAmmoCrateBase = new JsonAssetReference<ItemBarricadeAsset>("eccfe06e53d041d5b83c614ffa62ee59");
        BarricadeRepairStation = new JsonAssetReference<ItemBarricadeAsset>("c0d11e0666694ddea667377b4c0580be");
        BarricadeRepairStationBase = new JsonAssetReference<ItemBarricadeAsset>("26a6b91cd1944730a0f28e5f299cebf9");
        BarricadeAmmoBag = new JsonAssetReference<ItemBarricadeAsset>("16f55b999e9b4f158be12645e41dd753");
        StructureVehicleBay = new JsonAssetReference<ItemAsset>("c076f9e9f35f42a4b8b5711dfb230010");
        BarricadeUAV = new JsonAssetReference<ItemBarricadeAsset>("fb8f84e2617b480aadfd77bbf4a6c3ec");
        TimeLimitedStorages = new JsonAssetReference<ItemBarricadeAsset>[]
        {
            BarricadeAmmoCrate,
            BarricadeRepairStation,
            "a2eb76590cf74401aeb7ff4b4b79fd86", // supply crate
            "2193aa0b272f4cc1938f719c8e8badb1"  // supply roll
        };
        FOBRadios = new JsonAssetReference<ItemBarricadeAsset>[]
        {
            "7715ad81f1e24f60bb8f196dd09bd4ef", // USA
            "fb910102ad954169abd4b0cb06a112c8", // Russia
            "c7754ac78083421da73006b12a56811a", // MEC
            "439c32cced234f358e101294ea0ce3e4", // Germany
            "7bde55f70c494418bdd81926fb7d6359"  //China
        };
        RallyPoints = new JsonAssetReference<ItemBarricadeAsset>[]
        {
            "5e1db525179341d3b0c7576876212a81", // USA
            "0d7895360c80440fbe4a45eba28b2007", // Russia
            "c03352d9e6bb4e2993917924b604ee76", // MEC
            "49663078b594410b98b8a51e8eff3609", // Germany
            "7720ced42dba4c1eac16d14453cd8bc4" //China
        };
        #endregion

        #region Items
        ItemEntrenchingTool = new JsonAssetReference<ItemMeleeAsset>("6cee2662e8884d7bad3a5d743f8222da");
        ItemLaserDesignator = new JsonAssetReference<ItemGunAsset>("3879d9014aca4a17b3ed749cf7a9283e");
        ItemAprilFoolsBarrel = new JsonAssetReference<ItemBarrelAsset>("c3d3123823334847a9fd294e5d764889");
        #endregion

        #region UI and Effects
        UICapture = new JsonAssetReference<EffectAsset>("76a9ffb4659a494080d98c8ef7733815");
        UIFlagList = new JsonAssetReference<EffectAsset>("c01fe46d9b794364aca6a3887a028164");
        UIHeader = new JsonAssetReference<EffectAsset>("c14fe9ffee6d4f8dbe7f57885f678edd");
        UIFOBList = new JsonAssetReference<EffectAsset>("2c01a36943ea45189d866f5463f8e5e9");
        UISquadList = new JsonAssetReference<EffectAsset>("5acd091f1e7b4f93ac9f5431729ac5cc");
        UISquadMenu = new JsonAssetReference<EffectAsset>("98154002fbcd4b7499552d6497db8fc5");
        UIRally = new JsonAssetReference<EffectAsset>("a280ac3fe8c1486cadc8eca331e8ce32");
        UITeamSelector = new JsonAssetReference<EffectAsset>("b5924bc83eb24d7298a47f933d3f16d9");
        UIMuted = new JsonAssetReference<EffectAsset>("c5e31c7357134be09732c1930e0e4ff0");
        UIInjured = new JsonAssetReference<EffectAsset>("27b84636ed8d4c0fb557a67d89254b00");
        UIToastProgress = new JsonAssetReference<EffectAsset>("a113a0f2d0af4db8b5e5bcbc17fc96c9");
        UIToastTip = new JsonAssetReference<EffectAsset>("abbf74e86f1c4665925884c70b9433ba");
        UIXPPanel = new JsonAssetReference<EffectAsset>("d6de0a8025de44d29a99a41937a58a59");
        UICreditsPanel = new JsonAssetReference<EffectAsset>("3195b96457d04b9e80699777d2809b4c");
        UIConventionalLeaderboard = new JsonAssetReference<EffectAsset>("b83389df1245438db18889af94f04960");
        UINearbyResources = new JsonAssetReference<EffectAsset>("3775a1e7d84b47e79cacecd5e6b2a224");
        UITickets = new JsonAssetReference<EffectAsset>("aba88eedb84448e8a30bb803a53a7236");
        UIBuffs = new JsonAssetReference<EffectAsset>("f298af0b4d34405b98a539b8d2ff0505");
        UIActionMenu = new JsonAssetReference<EffectAsset>("bf4bc4e1a6a849c29e9f7a6de3a943e4");
        UIToastXP = new JsonAssetReference<EffectAsset>("a213915d61ad41cebab34fb12fe6870c");
        UIToastMedium = new JsonAssetReference<EffectAsset>("5f695955f0da4d19adacac39140da797");
        UIToastLarge = new JsonAssetReference<EffectAsset>("9de82ffea13946b391090eb918bf3991");
        UIToastWin = new JsonAssetReference<EffectAsset>("1f3ce50c120042c390f5c42522bd0fcd");
        UIKitMenu = new JsonAssetReference<EffectAsset>("c0155ea486d8427d9c70541abc875e78");
        UIVehicleHUD = new JsonAssetReference<EffectAsset>("1e1762d6f01442e89d159d4cd0ae7587");
        UIFlashingWarning = new RotatableConfig<JsonAssetReference<EffectAsset>>("4f8a6ca089a7499793c6076da9807273");
        UIModerationMenu = new RotatableConfig<JsonAssetReference<EffectAsset>>("80aee6c3f43f4c7facb2f2ffbb545d20");
        UIPopup = new RotatableConfig<JsonAssetReference<EffectAsset>>("bc6bbe8ce1d2464bb828d01ba1c5d461");
        EffectSpottedMarkerInfantry = new JsonAssetReference<EffectAsset>("79add0f1b07c478f87207d30fe5a5f4f");
        EffectSpottedMarkerFOB = new JsonAssetReference<EffectAsset>("39dce42142074b46b819feba9ce83353");
        EffectSpottedMarkerAA = new JsonAssetReference<EffectAsset>("0e90e68eff624456b76fee28a4875d14");
        EffectSpottedMarkerAPC = new JsonAssetReference<EffectAsset>("31d1404b7b3a465b8631308cdb48e3b2");
        EffectSpottedMarkerATGM = new JsonAssetReference<EffectAsset>("b20a7d914f92492fb1588f7baac80239");
        EffectSpottedMarkerAttackHeli = new JsonAssetReference<EffectAsset>("3f2c6776ba484f8ea443719161ec6ce5");
        EffectSpottedMarkerHMG = new JsonAssetReference<EffectAsset>("2315e6ed970542499fec1b06df87ffd2");
        EffectSpottedMarkerHumvee = new JsonAssetReference<EffectAsset>("99a84b82f9bd433891fdb99e80394bf3");
        EffectSpottedMarkerIFV = new JsonAssetReference<EffectAsset>("f2c29856b4f64146afd9872ab528c242");
        EffectSpottedMarkerJet = new JsonAssetReference<EffectAsset>("08f2cc6ed558459ea2caf3477b40df64");
        EffectSpottedMarkerMBT = new JsonAssetReference<EffectAsset>("983c6510c13042bf983e81f49cffca39");
        EffectSpottedMarkerMortar = new JsonAssetReference<EffectAsset>("c377810f849c4c7d84391b491406918b");
        EffectSpottedMarkerScoutCar = new JsonAssetReference<EffectAsset>("b0937aff90b94a588b70bc96ece49f53");
        EffectSpottedMarkerTransportAir = new JsonAssetReference<EffectAsset>("91b9f175b84849268d861eb0f0567788");
        EffectSpottedMarkerLogisticsGround = new JsonAssetReference<EffectAsset>("fa226268e87b4ec89664eca5b22b4d3d");
        EffectSpottedMarkerTransportGround = new JsonAssetReference<EffectAsset>("fa226268e87b4ec89664eca5b22b4d3d");
        EffectMarkerAmmo = new JsonAssetReference<EffectAsset>("827b0c00724b466d8d33633fe2a7743a");
        EffectMarkerRepair = new JsonAssetReference<EffectAsset>("bcfda6fb871f42cd88597c8ac5f7c424");
        EffectMarkerRadio = new JsonAssetReference<EffectAsset>("bc6f0e7d5d9340f39ca4968bc3f7a132");
        EffectMarkerRadioDamaged = new JsonAssetReference<EffectAsset>("37d5c48597ea4b61a7a87ed85a4c9b39");
        EffectMarkerBunker = new JsonAssetReference<EffectAsset>("d7452e8671c14e93a5e9d69e077d999c");
        EffectMarkerCacheAttack = new JsonAssetReference<EffectAsset>("26b60044bc1442eb9d0521bfea306517");
        EffectMarkerCacheDefend = new JsonAssetReference<EffectAsset>("06efa2c2f9ec413aa417c717a7be3364");
        EffectMarkerBuildable = new JsonAssetReference<EffectAsset>("35ab4b71bfb74755b318ce62935f58c9");
        EffectActionNeedMedic = new JsonAssetReference<EffectAsset>("4d9167abea4f4f009c6db4417e7efcdf");
        EffectActionNearbyMedic = new JsonAssetReference<EffectAsset>("ab5cb35e4ae14411a6190a87e72d50ba");
        EffectActionNeedAmmo = new JsonAssetReference<EffectAsset>("6e7dadbfafbe46ecbe565499f901670f");
        EffectActionNearbyAmmo = new JsonAssetReference<EffectAsset>("8caccb6344924fbab842625e5b1f5932");
        EffectActionNeedRide = new JsonAssetReference<EffectAsset>("2a4748c3e5464b2d93132e2ed15b57b2");
        EffectActionNeedSupport = new JsonAssetReference<EffectAsset>("eec285561d6040c7bbfcfa6b48f4b5ba");
        EffectActionHeliPickup = new JsonAssetReference<EffectAsset>("b6d1841936824065a99bcaa8152a7877");
        EffectActionHeliDropoff = new JsonAssetReference<EffectAsset>("c320d02aca914efb96dfbda6663940c5");
        EffectActionSuppliesBuild = new JsonAssetReference<EffectAsset>("d9861b9123bb4da9a72851b20ecab236");
        EffectActionSuppliesAmmo = new JsonAssetReference<EffectAsset>("50dbb9c23ae647b8adb829a771742d4c");
        EffectActionAirSupport = new JsonAssetReference<EffectAsset>("706671c9e5c24bae8eb74c9c4631cc59");
        EffectActionArmorSupport = new JsonAssetReference<EffectAsset>("637f0f25fd4b4180a04b2ecd1541ca37");
        EffectActionAttack = new JsonAssetReference<EffectAsset>("7ae60fdeeff447fdb1f0eb6582537f12");
        EffectActionDefend = new JsonAssetReference<EffectAsset>("16371fab7e8247619c6a6ec9a3e48e41");
        EffectActionMove = new JsonAssetReference<EffectAsset>("4077d5eea255435d8ed0133ec833b86a");
        EffectActionBuild = new JsonAssetReference<EffectAsset>("793bd80007f3484882284a6994e80bb3");
        EffectUnloadAmmo = new JsonAssetReference<EffectAsset>("8a2740cfc6f64ca68410145a83027735");
        EffectUnloadBuild = new JsonAssetReference<EffectAsset>("066c35a3e97e476a9f0a75218b4f6683");
        EffectUnlockVehicle = new JsonAssetReference<EffectAsset>("bc41e0feaebe4e788a3612811b8722d3");
        EffectDig = new JsonAssetReference<EffectAsset>("f894dff1d7ef453887182accd14dc9f9");
        EffectRepair = new JsonAssetReference<EffectAsset>("84347b13028340b8976033c08675d458");
        EffectRefuel = new JsonAssetReference<EffectAsset>("1d4fa25996114b7b89b061021fcc688f");
        EffectLaserGuidedNoSound = new JsonAssetReference<EffectAsset>("64c3a204acd441679b9322be04016cbf");
        EffectLaserGuidedSound = new JsonAssetReference<EffectAsset>("12209d06336142f8a0c4e49999759189");
        EffectGuidedMissileNoSound = new JsonAssetReference<EffectAsset>("5e7e9379872a40059cdc7e6d189d10dd");
        EffectGuidedMissileSound = new JsonAssetReference<EffectAsset>("7b458028c9de4a449c30ed5fc201bd37");
        EffectHeatSeekingMissileNoSound = new JsonAssetReference<EffectAsset>("39abdc11cd68477fa3c9b44aaf299760");
        EffectHeatSeekingMissileSound = new JsonAssetReference<EffectAsset>("5552f714ca744ab7bd0687fba0d541d3");
        EffectLockOn1 = new JsonAssetReference<EffectAsset>("45d4cf4e11664402b8cce2808a7b8d91");
        EffectLockOn2 = new JsonAssetReference<EffectAsset>("022fb707288a4e3cb6847fdd242e4092");
        EffectPurchase = new JsonAssetReference<EffectAsset>("5e2a0073025849d39322932d88609777");
        EffectAmmo = new JsonAssetReference<EffectAsset>("03caec479dd2475c92e1c326e1720140");
        EffectBuildSuccess = new JsonAssetReference<EffectAsset>("43c2ae01755540d4b99ce076aa6731eb");
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