using DanielWillett.ReflectionTools;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Specialized;
using System.Globalization;
using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Data;
using Uncreated.Framework.UI.Reflection;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Events.Models.Vehicles;
using Uncreated.Warfare.Interaction.UI;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Translations.Util;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Stats;

[UnturnedUI(BasePath = "Container/Box")]
public class PointsUI : UnturnedUI,
    IEventListener<PlayerUseableEquipped>,
    IEventListener<VehicleSwappedSeat>,
    IEventListener<EnterVehicle>,
    IEventListener<ExitVehicle>
{
    private static readonly InstanceGetter<VehicleAsset, bool>? GetUsesEngineRpmAndGears = Accessor.GenerateInstancePropertyGetter<VehicleAsset, bool>("UsesEngineRpmAndGears", allowUnsafeTypeBinding: true);

    private readonly Func<CSteamID, PointsUIData> _createData;

    private readonly UnturnedUIElement[] _positionElements =
    [ 
        new UnturnedUIElement("~/LogicPositionBase"),
        new UnturnedUIElement("~/LogicPositionGun"),
        new UnturnedUIElement("~/LogicPositionVehicle0"),
        new UnturnedUIElement("~/LogicPositionVehicle1"),
        new UnturnedUIElement("~/LogicPositionVehicle2"),
        new UnturnedUIElement("~/LogicPositionVehicle3"),
        new UnturnedUIElement("~/LogicPositionVehicle4"),
        new UnturnedUIElement("~/LogicPositionTurret0"),
        new UnturnedUIElement("~/LogicPositionTurret1"),
        new UnturnedUIElement("~/LogicPositionTurret2"),
        new UnturnedUIElement("~/LogicPositionTurret3"),
        new UnturnedUIElement("~/LogicPositionTurret4")
    ];

    private readonly PointsConfiguration _config;
    private readonly ImageProgressBar _xpBar       = new ImageProgressBar("XpProgress") { NeedsToSetLabel = false };
    private readonly UnturnedLabel _lblCurrentRank = new UnturnedLabel("LabelCurrentRank");
    private readonly UnturnedLabel _lblNextRank    = new UnturnedLabel("LabelNextRank");
    private readonly UnturnedLabel _lblCredits     = new UnturnedLabel("LabelCredits");
    private readonly UnturnedLabel _lblUsername    = new UnturnedLabel("LabelUsername");
    private readonly UnturnedLabel _lblStatistic   = new UnturnedLabel("LabelStatistic");

    public PointsUI(PointsConfiguration config, AssetConfiguration assetConfig, ILoggerFactory loggerFactory)
        : base(loggerFactory, assetConfig.GetAssetLink<EffectAsset>("UI:Points"), staticKey: true, debugLogging: false)
    {
        _config = config;

        _createData = steam64 => new PointsUIData(steam64, this);
    }

    private PointsUIData GetUIData(CSteamID steam64)
    {
        return GetOrAddData(steam64, _createData);
    }

    [EventListener(MustRunInstantly = true)]
    void IEventListener<PlayerUseableEquipped>.HandleEvent(PlayerUseableEquipped e, IServiceProvider serviceProvider)
    {
        UpdatePointsUI(e.Player, serviceProvider.GetRequiredService<PointsService>());
    }

    [EventListener(MustRunInstantly = true)]
    void IEventListener<VehicleSwappedSeat>.HandleEvent(VehicleSwappedSeat e, IServiceProvider serviceProvider)
    {
        UpdatePointsUI(e.Player, serviceProvider.GetRequiredService<PointsService>());
    }
    
    [EventListener(MustRunInstantly = true)]
    void IEventListener<EnterVehicle>.HandleEvent(EnterVehicle e, IServiceProvider serviceProvider)
    {
        UpdatePointsUI(e.Player, serviceProvider.GetRequiredService<PointsService>());
    }
    
    [EventListener(MustRunInstantly = true)]
    void IEventListener<ExitVehicle>.HandleEvent(ExitVehicle e, IServiceProvider serviceProvider)
    {
        UpdatePointsUI(e.Player, serviceProvider.GetRequiredService<PointsService>());
    }

    private static int GetPositionLogicIndex(WarfarePlayer player)
    {
        bool gunBoxVisible = player.UnturnedPlayer.equipment.useable is UseableGun;

        InteractableVehicle? vehicle = player.UnturnedPlayer.movement.getVehicle();

        bool vehicleBoxVisible = vehicle != null &&
                                 vehicle is { isDead: false, isUnderwater: false } &&
                                 player.UnturnedPlayer.movement.getSeat() == 0 &&
                                 player.UnturnedPlayer.isPluginWidgetFlagActive(EPluginWidgetFlags.ShowVehicleStatus);

        if (!vehicleBoxVisible)
        {
            return gunBoxVisible ? 1 : 0;
        }

        int index = 2;
        index += (vehicle!.usesFuel || vehicle.asset.isStaminaPowered) ? 1 : 0;
        index += vehicle.usesHealth ? 1 : 0;
        index += vehicle.usesBattery ? 1 : 0;
        index += (GetUsesEngineRpmAndGears != null && GetUsesEngineRpmAndGears(vehicle.asset) && vehicle.asset.AllowsEngineRpmAndGearsInHud) ? 1 : 0;

        if (gunBoxVisible)
        {
            // keeps it from going too high up for one frame if the player's holding a gun when they enter a vehicle
            gunBoxVisible = vehicle.passengers[0].turret != null;
        }

        return (gunBoxVisible ? 1 : 0) * 5 + index;
    }

    /// <summary>
    /// Updates all elements on the points UI if they need to be updated.
    /// </summary>
    /// <remarks>The UI will be cleared if the player is not on a team.</remarks>
    public void UpdatePointsUI(WarfarePlayer player, PointsService pointsService)
    {
        GameThread.AssertCurrent();

        PointsUIData data = GetUIData(player.Steam64);

        if (!player.Team.IsValid)
        {
            if (!data.HasUI)
                return;

            data.HasUI = false;
            ClearFromPlayer(player.Connection);
            return;
        }

        bool wasJustSent = false;
        if (!data.HasUI)
        {
            wasJustSent = true;
            SendToPlayer(player.Connection);
            data.HasUI = true;
            data.LastExperienceValue = -1;
            data.LastCreditsValue = -1;
            data.LastRank = -1;
            data.Position = 0;
        }

        WarfareRank rank = pointsService.GetRankFromExperience(player.CachedPoints.XP);

        int displayedXp = (int)Math.Round(player.CachedPoints.XP);
        int displayedPartialXp = (int)Math.Round(player.CachedPoints.XP - rank.CumulativeExperience);

        if (data.LastExperienceValue != displayedXp)
        {
            data.LastExperienceValue = displayedPartialXp;
            _xpBar.SetProgress(player.Connection, displayedXp);
            _xpBar.Label.SetText(player.Connection, displayedPartialXp.ToString(player.Locale.CultureInfo) + "/" + rank.Experience.ToString(player.Locale.CultureInfo));
        }

        if (data.LastRank != rank.RankIndex)
        {
            data.LastRank = rank.RankIndex;
            _lblCurrentRank.SetText(player.Connection, rank.Name); // todo icons
            _lblNextRank.SetText(player.Connection, rank.Next?.Name ?? string.Empty);
        }

        int displayedCredits = (int)Math.Round(player.CachedPoints.Credits);
        if (data.LastCreditsValue != displayedCredits)
        {
            data.LastCreditsValue = displayedCredits;
            HexStringHelper.TryParseColor32(_config["CreditsColor"], CultureInfo.InvariantCulture, out Color32 color);
            
            string creditString = "<#" + HexStringHelper.FormatHexColor(color) + ">C</color> " + displayedCredits.ToString(CultureInfo.InvariantCulture);

            _lblCredits.SetText(player.Connection, creditString);
        }

        int expectedPosition = GetPositionLogicIndex(player);
        
        if (data.Position != expectedPosition)
        {
            data.Position = expectedPosition;
            _positionElements[expectedPosition].Show(player);
        }

        if (!wasJustSent)
            return;

        _lblUsername.SetText(player.Connection, TranslationFormattingUtility.Colorize(player.Names.GetDisplayNameOrCharacterName(), player.Team.Faction.Color));
        _lblStatistic.SetText(player.Connection, "Score: 0"); // todo
    }

    private class PointsUIData(CSteamID steam64, PointsUI ui) : IUnturnedUIData
    {
        public CSteamID Player => steam64;
        public UnturnedUI Owner => ui;
        
        public bool HasUI { get; set; }
        public int LastExperienceValue { get; set; }
        public int LastCreditsValue { get; set; }
        public int LastRank { get; set; }
        public int Position { get; set; }

        UnturnedUIElement? IUnturnedUIData.Element => null;
    }
}