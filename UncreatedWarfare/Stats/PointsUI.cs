using System;
using System.Globalization;
using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Data;
using Uncreated.Framework.UI.Reflection;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Interaction.UI;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Translations.Util;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Stats;

[UnturnedUI(BasePath = "Container/Box")]
public class PointsUI : UnturnedUI
{
    private static readonly UnturnedUIElement[] PositionElements =
    [ 
        new UnturnedUIElement("LogicPositionBase"),
        new UnturnedUIElement("LogicPositionGun"),
        new UnturnedUIElement("LogicPositionVehicle"),
        new UnturnedUIElement("LogicPositionGunAndVehicle")
    ];

    private readonly PointsConfiguration _config;
    private readonly PointsService _points;
    private readonly ImageProgressBar _xpBar       = new ImageProgressBar("XpProgress") { NeedsToSetLabel = false };
    private readonly UnturnedLabel _lblCurrentRank = new UnturnedLabel("LabelCurrentRank");
    private readonly UnturnedLabel _lblNextRank    = new UnturnedLabel("LabelNextRank");
    private readonly UnturnedLabel _lblCredits     = new UnturnedLabel("LabelCredits");
    private readonly UnturnedLabel _lblUsername    = new UnturnedLabel("LabelUsername");
    private readonly UnturnedLabel _lblStatistic   = new UnturnedLabel("LabelStatistic");

    public PointsUI(PointsConfiguration config, PointsService points, AssetConfiguration assetConfig, ILoggerFactory loggerFactory)
        : base(loggerFactory, assetConfig.GetAssetLink<EffectAsset>("UI:Points"))
    {
        _config = config;
        _points = points;
    }

    private PointsUIData GetUIData(CSteamID steam64)
    {
        return GetOrAddData(steam64, (steam64) => new PointsUIData(steam64, this));
    }

    private static int GetPositionLogicIndex(WarfarePlayer player)
    {
        InteractableVehicle? vehicle = player.UnturnedPlayer.movement.getVehicle();

        bool vehicleBoxVisible = vehicle != null &&
                                 vehicle is { isDead: false, isUnderwater: false } &&
                                 player.UnturnedPlayer.movement.getSeat() == 0 &&
                                 player.UnturnedPlayer.isPluginWidgetFlagActive(EPluginWidgetFlags.ShowVehicleStatus);

        bool gunBoxVisible = player.UnturnedPlayer.equipment.useable is UseableGun;

        return (gunBoxVisible ? 1 : 0) + (vehicleBoxVisible ? 1 : 0) * 2;
    }

    /// <summary>
    /// Updates all elements on the points UI if they need to be updated.
    /// </summary>
    /// <remarks>The UI will be cleared if the player is not on a team.</remarks>
    public void UpdatePointsUI(WarfarePlayer player)
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

        WarfareRank rank = _points.GetRankFromExperience(player.CachedPoints.XP);

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
            PositionElements[expectedPosition].Show(player);
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