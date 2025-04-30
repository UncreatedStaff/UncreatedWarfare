using DanielWillett.ReflectionTools;
using System;
using System.Globalization;
using System.Linq;
using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Data;
using Uncreated.Framework.UI.Reflection;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Events.Models.Vehicles;
using Uncreated.Warfare.Interaction.UI;
using Uncreated.Warfare.Layouts;
using Uncreated.Warfare.Layouts.Phases;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Translations.Util;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.Timing;

namespace Uncreated.Warfare.Stats;

[UnturnedUI(BasePath = "Container/Box")]
public class PointsUI : UnturnedUI,
    IEventListener<PlayerUseableEquipped>,
    IEventListener<VehicleSwappedSeat>,
    IEventListener<EnterVehicle>,
    IEventListener<ExitVehicle>,
    ILayoutHostedService,
    IHudUIListener
{
    private static readonly InstanceGetter<VehicleAsset, bool>? GetUsesEngineRpmAndGears = Accessor.GenerateInstancePropertyGetter<VehicleAsset, bool>("UsesEngineRpmAndGears", allowUnsafeTypeBinding: true);

    private readonly Func<CSteamID, PointsUIData> _createData;

    private readonly IPlayerService _playerService;
    private readonly WarfareModule _module;
    private PointsService? _pointsService;

    private ILoopTicker? _loopTicker;
    private int _currentStatIndex;
    private bool _isHidden;
    private LeaderboardPhaseStatInfo? _currentStat;

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

    private static readonly Color32 StatColor = new Color32(255, 153, 102, 255);

    public PointsUI(PointsConfiguration config, AssetConfiguration assetConfig, ILoggerFactory loggerFactory, ILoopTickerFactory loopTickerFactory, IPlayerService playerService, WarfareModule module)
        : base(loggerFactory, assetConfig.GetAssetLink<EffectAsset>("UI:Points"), staticKey: true, debugLogging: false)
    {
        _config = config;

        _playerService = playerService;
        _module = module;
        _loopTicker = loopTickerFactory.CreateTicker(TimeSpan.FromMinutes(1d), false, true, (_, _, _) => ChooseNewCycledStat());

        _createData = steam64 => new PointsUIData(steam64, this);
    }

    public void ChooseNewCycledStat()
    {
        if (!_module.IsLayoutActive())
        {
            _currentStatIndex = -1;
            _currentStat = null;
        }
        else
        {
            Layout layout = _module.GetActiveLayout();
            LeaderboardPhase? lbPhase = layout.Phases.OfType<LeaderboardPhase>().LastOrDefault();
            if (lbPhase is not { PlayerStats.Length: > 0 })
            {
                _currentStatIndex = -1;
                _currentStat = null;
            }
            else
            {
                int randomOffset = RandomUtility.GetInteger(1, lbPhase.PlayerStats.Length);
                int newStatIndex = (_currentStatIndex + randomOffset) % lbPhase.PlayerStats.Length;
                int startIndex = newStatIndex != 0 ? newStatIndex - 1 : lbPhase.PlayerStats.Length;

                // keep offsetting while disabled
                while (lbPhase.PlayerStats[newStatIndex].DisablePointsUIDisplay && newStatIndex != startIndex)
                {
                    newStatIndex = (newStatIndex + 1) % lbPhase.PlayerStats.Length;
                }

                _currentStatIndex = newStatIndex;
                _currentStat = lbPhase.PlayerStats[newStatIndex];

                if (_currentStat.DisablePointsUIDisplay)
                {
                    _currentStatIndex = -1;
                    _currentStat = null;
                }
            }
        }

        foreach (WarfarePlayer player in _playerService.OnlinePlayers)
        {
            UpdatePointsUI(player);
        }
    }

    /// <inheritdoc />
    public void Hide(WarfarePlayer? player)
    {
        if (player != null)
        {
            ClearFromPlayer(player.Connection);
            return;
        }

        _isHidden = true;
        ClearFromAllPlayers();
        foreach (WarfarePlayer pl in _playerService.OnlinePlayers)
        {
            GetUIData(pl.Steam64).HasUI = false;
        }
    }

    /// <inheritdoc />
    public void Restore(WarfarePlayer? player)
    {
        if (player != null)
        {
            UpdatePointsUI(player);
            return;
        }

        _isHidden = false;
        foreach (WarfarePlayer pl in _playerService.OnlinePlayers)
        {
            UpdatePointsUI(pl);
        }
    }

    public bool IsStatRelevant(LeaderboardPhaseStatInfo stat)
    {
        if (_currentStat == null)
            return false;

        return _currentStat == stat || _currentStat.CachedExpression != null;
    }

    UniTask ILayoutHostedService.StartAsync(CancellationToken token)
    {
        ChooseNewCycledStat();
        return UniTask.CompletedTask;
    }

    UniTask ILayoutHostedService.StopAsync(CancellationToken token)
    {
        return UniTask.CompletedTask;
    }

    private PointsUIData GetUIData(CSteamID steam64)
    {
        return GetOrAddData(steam64, _createData);
    }

    [EventListener(MustRunInstantly = true)]
    void IEventListener<PlayerUseableEquipped>.HandleEvent(PlayerUseableEquipped e, IServiceProvider serviceProvider)
    {
        if (e.Useable is UseableGun)
            DoubleUpdate(e.Player);
        else
            UpdatePointsUI(e.Player);
    }

    [EventListener(MustRunInstantly = true)]
    void IEventListener<VehicleSwappedSeat>.HandleEvent(VehicleSwappedSeat e, IServiceProvider serviceProvider)
    {
        DoubleUpdate(e.Player);
    }
    
    [EventListener(MustRunInstantly = true)]
    void IEventListener<EnterVehicle>.HandleEvent(EnterVehicle e, IServiceProvider serviceProvider)
    {
        UpdatePointsUI(e.Player);
    }
    
    [EventListener(MustRunInstantly = true)]
    void IEventListener<ExitVehicle>.HandleEvent(ExitVehicle e, IServiceProvider serviceProvider)
    {
        UpdatePointsUI(e.Player);
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
    public void UpdatePointsUI(WarfarePlayer player)
    {
        GameThread.AssertCurrent();

        // circular reference
        _pointsService ??= _module.ServiceProvider.Resolve<PointsService>();

        PointsUIData data = GetUIData(player.Steam64);

        if (!player.Team.IsValid || _isHidden)
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
            data.Position = -1;
            _lblUsername.SetText(player.Connection, TranslationFormattingUtility.Colorize(player.Names.GetDisplayNameOrCharacterName(), player.Team.Faction.Color));
        }

        WarfareRank rank = _pointsService.GetRankFromExperience(player.CachedPoints.XP);

        int displayedXp = (int)Math.Round(player.CachedPoints.XP);
        int displayedPartialXp = (int)Math.Round(player.CachedPoints.XP - rank.CumulativeExperience);

        if (data.LastExperienceValue != displayedXp)
        {
            data.LastExperienceValue = displayedXp;
            _xpBar.SetProgress(player.Connection, (float)rank.GetProgress(player.CachedPoints.XP));

            string label = displayedPartialXp.ToString(player.Locale.CultureInfo);
            if (rank.Experience != 0)
                label += "/" + rank.Experience.ToString(player.Locale.CultureInfo);

            _xpBar.Label.SetText(player.Connection, label);
        }

        if (data.LastRank != rank.RankIndex)
        {
            data.LastRank = rank.RankIndex;
            _lblCurrentRank.SetText(player.Connection, rank.Name);
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

        LeaderboardPhaseStatInfo? stat = _currentStat;
        if (stat != null)
        {
            PlayerGameStatsComponent statComponent = player.Component<PlayerGameStatsComponent>();
            double[] stats = statComponent.Stats;
            double value = 0d;
            if (stat.Index >= 0 && stat.Index < stats.Length)
            {
                value = statComponent.GetStatValue(stat);
            }

            if (wasJustSent || data.Stat != stat || Math.Abs(data.StatValue - value) > 0.01)
            {
                string statName = stat.DisplayName?.Translate(player.Locale.LanguageInfo, stat.Name) ?? stat.Name;
                string statValue = value.ToString(stat.NumberFormat ?? "0.##", player.Locale.CultureInfo);

                _lblStatistic.SetText(player.Connection, statName + ": " + TranslationFormattingUtility.Colorize(statValue, StatColor, false));

                data.Stat = stat;
                data.StatValue = value;
            }
        }
        else if (data.Stat != null || wasJustSent)
        {
            _lblStatistic.SetText(player.Connection, string.Empty);
            data.Stat = null;
            data.StatValue = 0;
        }
    }

    private void DoubleUpdate(WarfarePlayer player)
    {
        UpdatePointsUI(player);
        UniTask.Create(async () =>
        {
            await UniTask.NextFrame();
            if (player.IsOnline)
                UpdatePointsUI(player);
        });
    }

    /// <inheritdoc />
    protected override void OnDisposing()
    {
        _loopTicker?.Dispose();
        _loopTicker = null;
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
        public LeaderboardPhaseStatInfo? Stat { get; set; }
        public double StatValue { get; set; }

        UnturnedUIElement? IUnturnedUIData.Element => null;
    }
}