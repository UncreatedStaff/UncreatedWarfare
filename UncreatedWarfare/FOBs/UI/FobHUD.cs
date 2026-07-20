using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Data;
using Uncreated.Framework.UI.Patterns;
using Uncreated.Framework.UI.Reflection;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Fobs;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Util;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Fobs.UI;

[UnturnedUI(BasePath = "FobList")]
public class FobHUD : 
    UnturnedUI,
    IEventListener<IPlayerNeedsFobUIUpdateEvent>,
    IEventListener<IFobNeedsUIUpdateEvent>,
    IHudUIListener
{
    /// <summary>
    /// Maximum number of FOBs that can be displayed on this UI.
    /// </summary>
    public static readonly int MaximumFOBs = 12;

    private readonly HudManager _hudManager;
    private readonly FobManager _fobManager;
    private readonly Func<CSteamID, UIData> _getData;
    private readonly IPlayerService _playerService;
    private readonly FobTranslations _translations;
    public UnturnedLabel Title { get; } = new UnturnedLabel("Title");
    public FobElement[] Fobs { get; } = ElementPatterns.CreateArray<FobElement>("Fob_{0}/Fob{1}_{0}", 0, MaximumFOBs);
    public UnturnedUIElement[] LogicSquadMemberPositions { get; } = ElementPatterns.CreateArray<UnturnedUIElement>("SquadMembers_{0}", 0, to: 6);

    public FobHUD(
        IServiceProvider serviceProvider,
        AssetConfiguration assetConfig,
        ILoggerFactory loggerFactory,
        HudManager hudManager,
        TranslationInjection<FobTranslations> translations)
        : base(loggerFactory, assetConfig.GetAssetLink<EffectAsset>("UI:FobHUD"), debugLogging: false, staticKey: true)
    {
        _hudManager = hudManager;
        _translations = translations.Value;
        _fobManager = serviceProvider.GetRequiredService<FobManager>();
        _playerService = serviceProvider.GetRequiredService<IPlayerService>();
        _getData = id => new UIData { Owner = this, Player = id };
    }

    private UIData GetOrAddData(WarfarePlayer player)
    {
        CSteamID id = player.Steam64;
        return GetOrAddData(id, _getData);
    }

    /// <inheritdoc />
    public void Hide(WarfarePlayer? player)
    {
        if (player != null)
        {
            ClearFromPlayer(player.Connection);
            return;
        }

        ClearFromAllPlayers();
        foreach (WarfarePlayer p in _playerService.OnlinePlayers)
        {
            if (GetData<UIData>(p.Steam64) is { } data)
                data.HasUI = false;
        }
    }

    /// <inheritdoc />
    public void Restore(WarfarePlayer? player)
    {
        if (player != null)
        {
            UpdateForPlayer(player);
            return;
        }

        foreach (WarfarePlayer pl in _playerService.OnlinePlayers)
        {
            UpdateForPlayer(pl);
        }
    }

    private void CloseUI(WarfarePlayer player, UIData data)
    {
        if (!data.HasUI)
            return;

        data.HasUI = false;
        player.Locale.LocaleUpdated -= OnLocaleUpdated;
        ClearFromPlayer(player.Connection);
    }

    private void OnLocaleUpdated(WarfarePlayerLocale locale)
    {
        UIData data = GetOrAddData(locale.Player);
        if (data.HasUI)
            SendConstantText(locale.Player);
    }

    private void UpdateForPlayer(WarfarePlayer player)
    {
        GameThread.AssertCurrent();

        UIData data = GetOrAddData(player);
        if (_hudManager.IsHidden(player))
        {
            CloseUI(player, data);
            return;
        }

        using IEnumerator<IFob> enumerator = _fobManager.Fobs.Where(f => f.IsVisibleToPlayer(player)).GetEnumerator();
        if (!enumerator.MoveNext())
        {
            // no FOBs
            CloseUI(player, data);
            return;
        }

        if (!data.HasUI)
        {
            SendToPlayer(player.Connection);
            SendConstantText(player);
            player.Locale.LocaleUpdated += OnLocaleUpdated;
        }

        bool isDone = false;
        bool isFirst = true;
        for (int i = 0; i < Fobs.Length; i++)
        {
            ref FOBRowInfo info = ref data.Cache[i];

            FobElement element = Fobs[i];
            if (isDone || !(isFirst || enumerator.MoveNext()))
            {
                isFirst = false;
                isDone = true;
                if (info.Visible)
                {
                    element.Root.Hide(player);
                    info.Visible = false;
                }
                continue;
            }

            isFirst = false;
            IFob fob = enumerator.Current!;

            FOBRowInfo newInfo;
            newInfo.Name = fob.GetUIDisplay(player.Team);
            newInfo.Color = fob.GetColor(player.Team);
            newInfo.Location = fob.GetClosestLocation(shortName: true) ?? string.Empty;
            newInfo.Visible = true;
            if (fob is IResourceFob resourceFob)
            {
                newInfo.Build = Mathf.CeilToInt(resourceFob.BuildCount);
                newInfo.Ammo = Mathf.CeilToInt(resourceFob.AmmoCount);
            }
            else
            {
                newInfo.Build = -1;
                newInfo.Ammo = -1;
            }

            info.UpdateDifferences(in newInfo, element, player);
        }
    }

    private void SendConstantText(WarfarePlayer player)
    {
        bool isDefaultLang = player.Locale.IsDefaultLanguage;

        if (!isDefaultLang || !_translations.FobListTitle.HasDefaultValue)
            Title.SetText(player.Connection, _translations.FobListTitle.Translate(player));
    }

    public void HandleEvent(IPlayerNeedsFobUIUpdateEvent e, IServiceProvider serviceProvider)
    {
        if (e.Player != null)
            UpdateForPlayer(e.Player);
    }

    public void HandleEvent(IFobNeedsUIUpdateEvent e, IServiceProvider serviceProvider)
    {
        if (e.Fob == null)
            return;

        foreach (WarfarePlayer player in _playerService.OnlinePlayers.Where(e.Fob.IsVisibleToPlayer))
        {
            UpdateForPlayer(player);
        }
    }

#nullable disable
    public class FobElement : PatternRoot
    {
        [Pattern("Name")]
        public UnturnedLabel Name { get; set; }
        
        [Pattern("Location")]
        public UnturnedLabel Location { get; set; }

        [Pattern("Build")]
        public UnturnedLabel BuildCount { get; set; }
        [Pattern("Ammo")]
        public UnturnedLabel AmmoCount { get; set; }
    }
#nullable restore
    private sealed class UIData : IUnturnedUIData
    {
        internal bool HasUI;
        internal FOBRowInfo[] Cache = new FOBRowInfo[MaximumFOBs];

        public required CSteamID Player { get; init; }
        public required UnturnedUI Owner { get; init; }
        public UnturnedUIElement Element => null;
    }

    private static readonly Color32 DefaultFobNameColor = new Color32(230, 230, 230, 255);

    private struct FOBRowInfo
    {
        public int Build, Ammo;
        public Color32 Color;
        public string Name;
        public string Location;
        public bool Visible;

        public void Reset(int index)
        {
            Build = 0;
            Ammo = 0;
            Color = DefaultFobNameColor;
            Location = string.Empty;
            Name = string.Empty;
            Visible = index <= 0;
        }

        public void UpdateDifferences(in FOBRowInfo data, FobElement ui, WarfarePlayer player)
        {
            if (!data.Visible)
            {
                if (Visible)
                    ui.Hide(player);
                Visible = false;
                return;
            }

            if (!Visible)
            {
                ui.Show(player);
                Visible = true;
            }

            if (data.Build != Build)
            {
                Build = data.Build;
                ui.BuildCount.SetText(player, Build >= 0 ? Build.ToString(player.Locale.CultureInfo) : string.Empty);
            }

            if (data.Ammo != Ammo)
            {
                Ammo = data.Ammo;
                ui.AmmoCount.SetText(player, Ammo >= 0 ? Ammo.ToString(player.Locale.CultureInfo) : string.Empty);
            }

            if (data.Color != Color || !string.Equals(data.Name, Name, StringComparison.OrdinalIgnoreCase))
            {
                Color = data.Color;
                Name = data.Name?.ToUpper() ?? string.Empty;

                string text = DefaultFobNameColor == Color || Name.Length == 0 ? Name : TranslationFormattingUtility.Colorize(Name, Color);
                ui.Name.SetText(player, text);
            }

            if (!string.Equals(data.Location, Location, StringComparison.Ordinal))
            {
                Location = data.Location;
                ui.Location.SetText(player, Location ?? string.Empty);
            }
        }
    }
}
