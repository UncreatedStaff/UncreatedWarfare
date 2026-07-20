using System;
using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Data;
using Uncreated.Framework.UI.Patterns;
using Uncreated.Framework.UI.Presets;
using Uncreated.Framework.UI.Reflection;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.FOBs.SupplyCrates;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.StrategyMaps.MapTacks;

[UnturnedUI(BasePath = "UI")]
internal sealed class MapTackInfoUI : UnturnedUI, IEventListener<PlayerLeft>
{
    private static readonly List<KeyValuePair<MapTackVehicleType, int>> WorkingVehicleCounts = new List<KeyValuePair<MapTackVehicleType, int>>();

    private readonly MapTackInfoUITranslations _translations;

    public UnturnedUIElement LogicClose { get; } = new UnturnedUIElement("~/Logic_Close");
    public UnturnedUIElement LogicOpen { get; } = new UnturnedUIElement("~/Logic_Open");
    
    public UnturnedLabel Title { get; } = new UnturnedLabel("Hdr");
    public UnturnedLabel Location { get; } = new UnturnedLabel("Loc");

    public UnturnedLabel DestroyedAttribute { get; } = new UnturnedLabel("Attributes/Destroyed");
    public UnturnedLabel ProxiedAttribute { get; } = new UnturnedLabel("Attributes/Proxied");
    public UnturnedLabel NotBuiltAttribute { get; } = new UnturnedLabel("Attributes/NotBuilt");
    public UnturnedLabel LowBuildAttribute { get; } = new UnturnedLabel("Attributes/LowBuild");
    public UnturnedLabel LowAmmoAttribute { get; } = new UnturnedLabel("Attributes/LowAmmo");

    public UnturnedUIElement HealthBarRoot { get; } = new UnturnedUIElement("HP");
    public UnturnedLabel HealthBar { get; } = new UnturnedLabel("HP/Bar/Foreground");
    public UnturnedLabel HealthBarIcon { get; } = new UnturnedLabel("HP/Icon");

    public VehicleCount[] Counts { get; } =
    [
        ElementPatterns.Create<VehicleCount>("Emplacements/Infantry"),
        ElementPatterns.Create<VehicleCount>("Emplacements/AA"),
        ElementPatterns.Create<VehicleCount>("Emplacements/Mortar"),
        ElementPatterns.Create<VehicleCount>("Emplacements/HMG"),

        ElementPatterns.Create<VehicleCount>("Air/AttackHeli"),
        ElementPatterns.Create<VehicleCount>("Air/Jet"),
        ElementPatterns.Create<VehicleCount>("Air/TransportHeli"),

        ElementPatterns.Create<VehicleCount>("Ground/APC"),
        ElementPatterns.Create<VehicleCount>("Ground/Humvee"),
        ElementPatterns.Create<VehicleCount>("Ground/IFV"),
        ElementPatterns.Create<VehicleCount>("Ground/MBT"),
        ElementPatterns.Create<VehicleCount>("Ground/ScoutCar"),
        ElementPatterns.Create<VehicleCount>("Ground/Truck")
    ];

    public UnturnedUIElement SuppliesRoot { get; } = new UnturnedUIElement("Supplies");
    public UnturnedLabel BuildSupply { get; } = new UnturnedLabel("Supplies/Bld");
    public UnturnedLabel AmmoSupply { get; } = new UnturnedLabel("Supplies/Ammo");

#nullable disable
    internal class VehicleCount : PatternRoot
    {
        [Pattern("Ct")]
        public UnturnedLabel Count { get; set; }
    }
#nullable restore

    private readonly Func<CSteamID, Data> _addData;

    public MapTackInfoUI(AssetConfiguration assetConfig, TranslationInjection<MapTackInfoUITranslations> translations)
        : base(assetConfig.GetAssetLink<EffectAsset>("UI:MapTackInfoUI"), reliable: false)
    {
        _translations = translations.Value;
        _addData = id => new Data(id, this);
    }

    internal Data GetOrAddData(CSteamID player)
    {
        return GetOrAddData(player, _addData);
    }

    public void TryClose(WarfarePlayer player, bool instant = false)
    {
        GameThread.AssertCurrent();

        Data? data = GetData<Data>(player.Steam64);
        if (data is not { HasUI: true })
        {
            return;
        }

        if (data.CurrentMapTack != null)
        {
            data.CurrentMapTack?.RemoveUIUpdateListener(player);
            data.CurrentMapTack = null;
        }

        if (data.IsClosing && !instant)
            return;

        if (instant)
        {
            data.HasUI = false;
            data.IsClosing = false;
            data.HealthBarCount = 0;
            data.StoredHealth = 0;
            player.Locale.LocaleUpdated -= OnLocaleUpdated;
            ClearFromPlayer(player.SteamPlayer);
        }
        else
        {
            LogicClose.Show(player);
            data.IsClosing = true;
            UniTask.Create(async () =>
            {
                await UniTask.Delay(TimeSpan.FromSeconds(0.15d));

                if (!data.IsClosing || !player.IsOnline)
                    return;
                
                data.IsClosing = false;
                if (!data.HasUI)
                    return;

                data.HasUI = false;
                data.HealthBarCount = 0;
                data.StoredHealth = 0;
                player.Locale.LocaleUpdated -= OnLocaleUpdated;
                ClearFromPlayer(player.SteamPlayer);
            });
        }
    }

    private void OnLocaleUpdated(WarfarePlayerLocale locale)
    {
        Data data = GetOrAddData(locale.Player.Steam64);
        if (!data.HasUI || data.CurrentMapTack?.UIHandler is not { } uiHandler)
            return;

        ITransportConnection c = locale.Player.Connection;
        LanguageSet set = new LanguageSet(locale.Player);

        string title = uiHandler.GetTitle(in set);
        Title.SetText(c, title);

        string? location = uiHandler.GetLocation(in set);
        if (!string.IsNullOrEmpty(location))
        {
            Location.SetText(c, location);
            Location.Show(c);
        }
        else
        {
            Location.Hide(c);
        }
    }

    public void Open(WarfarePlayer player, MapTack tack)
    {
        GameThread.AssertCurrent();

        IMapTackUIHandler? uiHandler = tack.UIHandler;
        if (uiHandler == null)
        {
            TryClose(player);
            return;
        }

        Data data = GetOrAddData(player.Steam64);
        LanguageSet set = new LanguageSet(player);

        ITransportConnection c = player.Connection;

        string title = uiHandler.GetTitle(in set);

        bool isDefaultState;

        if (!data.HasUI)
        {
            SendToPlayer(c, title);
            isDefaultState = true;
            data.HasUI = true;
            player.Locale.LocaleUpdated += OnLocaleUpdated;
            data.HealthBarCount = 0;
            data.StoredHealth = 0;
        }
        else
        {
            if (data.IsClosing)
            {
                LogicOpen.Show(c);
            }
            if (data.CurrentMapTack != tack)
            {
                Title.SetText(c, title);
            }
            isDefaultState = false;
        }

        data.IsClosing = false;

        string? location = uiHandler.GetLocation(in set);
        if (!string.IsNullOrEmpty(location))
        {
            Location.SetText(c, location);
            if (!isDefaultState)
            {
                Location.Show(c);
            }
        }
        else
        {
            Location.Hide(c);
        }

        MapTackAttributes attributes = uiHandler.GetAttributes();

        if ((attributes & MapTackAttributes.Destroyed) != 0)
            DestroyedAttribute.Show(c);
        else if (!isDefaultState)
            DestroyedAttribute.Hide(c);

        if ((attributes & MapTackAttributes.NotBuilt) != 0)
        {
            HealthBarIcon.SetText(c, "ˎ");
            //NotBuiltAttribute.Show(c);
        }
        else if (!isDefaultState)
        {
            HealthBarIcon.SetText(c, "¢");
            // NotBuiltAttribute.Hide(c);
        }

        if ((attributes & MapTackAttributes.Proxied) != 0)
            ProxiedAttribute.Show(c);
        else if (!isDefaultState)
            ProxiedAttribute.Hide(c);

        if ((attributes & MapTackAttributes.LowBuild) != 0)
            LowBuildAttribute.Show(c);
        else if (!isDefaultState)
            LowBuildAttribute.Hide(c);

        if ((attributes & MapTackAttributes.LowAmmo) != 0)
            LowAmmoAttribute.Show(c);
        else if (!isDefaultState)
            LowAmmoAttribute.Hide(c);

        data.Attributes = attributes;

        uiHandler.CountVehicles(WorkingVehicleCounts);
        try
        {
            int vehicleMask = 0;
            foreach ((MapTackVehicleType type, int count) in WorkingVehicleCounts)
            {
                VehicleCount ui = GetCount(type);
                int mask = 1 << (int)type;
                bool hasUi = !isDefaultState && (data.VehicleMask & mask) != 0;
                if (count > 0)
                {
                    vehicleMask |= mask;
                    if (!hasUi)
                        ui.Show(c);

                    ui.Count.SetText(c, count.ToString(player.Locale.CultureInfo));
                }
                else if (hasUi)
                {
                    ui.Hide(c);
                }
            }

            for (MapTackVehicleType type = MapTackVehicleType.Infantry; type < MapTackVehicleType.Truck; ++type)
            {
                int mask = 1 << (int)type;
                if ((vehicleMask & mask) != 0)
                    continue;

                if (!isDefaultState && (data.VehicleMask & mask) != 0)
                {
                    GetCount(type).Hide(c);
                }
            }

            data.VehicleMask = vehicleMask;

            double? health = uiHandler.GetHealth();
            if (health.HasValue)
            {
                SetHealth(c, health.Value, data);
                if (!isDefaultState)
                {
                    HealthBarRoot.Show(c);
                    data.HasHealthBar = true;
                }
            }
            else
            {
                HealthBarRoot.Hide(c);
                data.HasHealthBar = false;
            }

        }
        finally
        {
            WorkingVehicleCounts.Clear();
        }

        if (isDefaultState)
        {
            data.HasBuildSupply = true;
            data.HasAmmoSupply = true;
            data.HasSupplies = true;
        }

        UpdateSupply(player, SupplyType.Build, uiHandler, data);
        UpdateSupply(player, SupplyType.Ammo, uiHandler, data);

        if (data.CurrentMapTack != tack)
        {
            data.CurrentMapTack?.RemoveUIUpdateListener(player);
            data.CurrentMapTack = tack;
            tack.AddUIUpdateListener(player);
        }
    }

    private void UpdateSupply(WarfarePlayer player, SupplyType type, IMapTackUIHandler uiHandler, Data data, int? amount = null)
    {
        UnturnedLabel label;
        ref bool has = ref data.HasBuildSupply;
        Translation<int> translation;
        if (type == SupplyType.Build)
        {
            label = BuildSupply;
            // has = ref data.HasBuildSupply;
            translation = _translations.BuildSupplies;
        }
        else
        {
            label = AmmoSupply;
            has = ref data.HasAmmoSupply;
            translation = _translations.AmmoSupplies;
        }

        int? supply = amount ?? uiHandler.GetSupplyCount(type);

        if (supply.HasValue)
        {
            label.SetText(player, translation.Translate(supply.Value, player));
            if (!has)
            {
                label.Show(player);
                has = true;
            }
        }
        else if (has)
        {
            label.Hide(player);
            has = false;
        }

        if (!data.HasAmmoSupply && !data.HasBuildSupply)
        {
            if (data.HasSupplies)
            {
                SuppliesRoot.Hide(player);
                data.HasSupplies = false;
            }
        }
        else if (!data.HasSupplies)
        {
            SuppliesRoot.Show(player);
            data.HasSupplies = true;
        }
    }

    private VehicleCount GetCount(MapTackVehicleType type)
    {
        if (type is <= MapTackVehicleType.Other or > MapTackVehicleType.Truck)
            throw new ArgumentOutOfRangeException(nameof(type), type, "Out of range vehicle type.");

        return Counts[(int)type - 1];
    }

    private void SetHealth(ITransportConnection c, double health, Data data)
    {
        health = Math.Clamp(health, 0d, 1d);

        int amt = GetMaxHealthBarCount(data.VehicleMask);
        data.StoredHealth = health;
        if (data.HealthBarCount == amt)
            return;

        int charCt = (int)Math.Round(amt * health);
        string str = new string('█', charCt);
        HealthBar.SetText(c, str);
    }

    /// <summary>
    /// Gets the number of characters needed to fill the bar depending on how many vehicles are displayed (affecting the width).
    /// </summary>
    /// <param name="vehicleMask">A bit mask where each bit corresponds to the position in the <see cref="MapTackVehicleType"/> enum.</param>
    public static int GetMaxHealthBarCount(int vehicleMask)
    {
        // these can't reach 200 anyways so no point in calculating them
        // int emplLength = (vehicleMask & (1 << (int)MapTackVehicleType.Infantry))
        //                  + (vehicleMask & (1 << (int)MapTackVehicleType.AA))
        //                  + (vehicleMask & (1 << (int)MapTackVehicleType.Mortar))
        //                  + (vehicleMask & (1 << (int)MapTackVehicleType.HMG));
        
        // int airLength = (vehicleMask & (1 << (int)MapTackVehicleType.AttackHeli))
        //                 + (vehicleMask & (1 << (int)MapTackVehicleType.Jet))
        //                 + (vehicleMask & (1 << (int)MapTackVehicleType.TransportHeli));

        int gndLength = ((vehicleMask & (1 << (int)MapTackVehicleType.APC)) != 0 ? 1 : 0)
                        + ((vehicleMask & (1 << (int)MapTackVehicleType.Humvee)) != 0 ? 1 : 0)
                        + ((vehicleMask & (1 << (int)MapTackVehicleType.IFV)) != 0 ? 1 : 0)
                        + ((vehicleMask & (1 << (int)MapTackVehicleType.MBT)) != 0 ? 1 : 0)
                        + ((vehicleMask & (1 << (int)MapTackVehicleType.ScoutCar)) != 0 ? 1 : 0)
                        + ((vehicleMask & (1 << (int)MapTackVehicleType.Truck)) != 0 ? 1 : 0);

        // int lenByEmpl = 20 + emplLength * 40;
        // int lenByAir = 20 + airLength * 48;
        int lenByGnd = 20 + gndLength * 48;

        // 142 = 103
        // 154 = 112
        // 202 = 147
        // 250 = 183

        // int width = Math.Max(Math.Max(Math.Max(lenByEmpl, lenByAir), lenByGnd), 200);
        int width = Math.Max(200, lenByGnd);

        int barWidth = width - 58;

        const double charactersPerPixelRatio = 35d / 48;

        return (int)Math.Round(charactersPerPixelRatio * barWidth);
    }

    public void HandleVehicleUpdated(MapTack tack, MapTackVehicleType type, int amount)
    {
        int mask = 1 << (int)type;

        using List<WarfarePlayer>.Enumerator enumerator = tack.EnumerateWatchers();
        while (enumerator.MoveNext())
        {
            WarfarePlayer player = enumerator.Current!;
            Data data = GetOrAddData(player.Steam64);
            ITransportConnection c = player.Connection;
            VehicleCount ui = GetCount(type);
            bool hidden = amount <= 0;
            if (!hidden)
            {
                if ((data.VehicleMask & mask) == 0)
                {
                    ui.Show(c);
                    data.VehicleMask |= mask;
                    OnVehiclesResized(c, data);
                }

                ui.Count.SetText(c, amount.ToString(player.Locale.CultureInfo));
            }
            else if ((data.VehicleMask & mask) != 0)
            {
                ui.Hide(c);
                data.VehicleMask &= ~mask;
                OnVehiclesResized(c, data);
            }
        }
    }

    private void OnVehiclesResized(ITransportConnection c, Data data)
    {
        if (data.HasHealthBar)
        {
            // health bar can resize when vehicles are added/removed
            SetHealth(c, data.StoredHealth, data);
        }
    }

    public void HandleSuppliesUpdated(MapTack tack, SupplyType type, int amount)
    {
        int? amt = amount;

        using List<WarfarePlayer>.Enumerator enumerator = tack.EnumerateWatchers();
        while (enumerator.MoveNext())
        {
            Data data = GetOrAddData(enumerator.Current!.Steam64);
            UpdateSupply(enumerator.Current!, type, tack.UIHandler!, data, amt);
        }
    }

    public void HandleHealthUpdated(MapTack tack, double? health)
    {
        using List<WarfarePlayer>.Enumerator enumerator = tack.EnumerateWatchers();
        while (enumerator.MoveNext())
        {
            WarfarePlayer player = enumerator.Current!;
            Data data = GetOrAddData(player.Steam64);
            if (health.HasValue)
            {
                if (!data.HasHealthBar)
                {
                    HealthBarRoot.Show(player);
                }
                SetHealth(player.Connection, health.Value, data);
            }
            else if (data.HasHealthBar)
            {
                HealthBarRoot.Hide(player);
            }
        }
    }

    public void HandleAttributesUpdated(MapTack tack, MapTackAttributes attributes)
    {
        using List<WarfarePlayer>.Enumerator enumerator = tack.EnumerateWatchers();
        while (enumerator.MoveNext())
        {
            WarfarePlayer player = enumerator.Current!;
            Data data = GetOrAddData(player.Steam64);

            MapTackAttributes updateMask = data.Attributes ^ attributes;
            ITransportConnection c = player.Connection;

            if ((updateMask & MapTackAttributes.Destroyed) != 0)
                DestroyedAttribute.SetVisibility(c, (attributes & MapTackAttributes.Destroyed) != 0);

            if ((updateMask & MapTackAttributes.Proxied) != 0)
                ProxiedAttribute.SetVisibility(c, (attributes & MapTackAttributes.Proxied) != 0);

            if ((updateMask & MapTackAttributes.LowBuild) != 0)
                LowBuildAttribute.SetVisibility(c, (attributes & MapTackAttributes.LowBuild) != 0);

            if ((updateMask & MapTackAttributes.LowAmmo) != 0)
                LowAmmoAttribute.SetVisibility(c, (attributes & MapTackAttributes.LowAmmo) != 0);

            if ((updateMask & MapTackAttributes.NotBuilt) != 0)
            {
                HealthBarIcon.SetText(c, (attributes & MapTackAttributes.NotBuilt) != 0 ? "ˎ" : "¢");
                // LowAmmoAttribute.SetVisibility(c, (attributes & MapTackAttributes.NotBuilt) != 0);
            }

            data.Attributes = attributes;
        }
    }

    public class Data : IUnturnedUIData
    {
        public CSteamID Player { get; }
        public UnturnedUI Owner { get; }
        UnturnedUIElement? IUnturnedUIData.Element => null;

        public bool HasUI;
        public bool IsClosing;

        public int VehicleMask;
        public bool HasBuildSupply;
        public bool HasAmmoSupply;
        public bool HasSupplies;
        public bool HasHealthBar;
        public MapTackAttributes Attributes;
        public bool HasShovelIcon;
        public int HealthBarCount;
        public double StoredHealth;
        public float LastLookAwayTime;

        public MapTack? CurrentMapTack;

        public Data(CSteamID player, MapTackInfoUI owner)
        {
            Player = player;
            Owner = owner;
        }
    }

    [EventListener(MustRunInstantly = true)]
    void IEventListener<PlayerLeft>.HandleEvent(PlayerLeft e, IServiceProvider serviceProvider)
    {
        if (GetData<Data>(e.Player.Steam64) is not { } data)
        {
            return;
        }

        if (data.CurrentMapTack != null)
        {
            data.CurrentMapTack?.RemoveUIUpdateListener(e.Player);
            data.CurrentMapTack = null;
        }

        data.HasUI = false;
    }
}

internal sealed class MapTackInfoUITranslations : TranslationCollection
{
    public override string Name => "UI/Map Tack Info";

    [TranslationData("Translation for the \"# BUILD\" part of the map tack UI.", "Build supply count.")]
    public readonly Translation<int> BuildSupplies = new Translation<int>("{0} BUILD", options: TranslationOptions.TMProUI | TranslationOptions.NoRichText);

    [TranslationData("Translation for the \"# AMMO\" part of the map tack UI.", "Ammo supply count.")]
    public readonly Translation<int> AmmoSupplies = new Translation<int>("{0} AMMO", options: TranslationOptions.TMProUI | TranslationOptions.NoRichText);

    [TranslationData("Title for the main base popup.", "Ammo supply count.")]
    public readonly Translation<FactionInfo> MainBaseTitle = new Translation<FactionInfo>("{0} Main Base", options: TranslationOptions.TMProUI | TranslationOptions.NoRichText, arg0Fmt: FactionInfo.FormatAbbreviation);
}