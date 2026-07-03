using System;
using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Data;
using Uncreated.Framework.UI.Patterns;
using Uncreated.Framework.UI.Presets;
using Uncreated.Framework.UI.Reflection;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.FOBs.SupplyCrates;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.StrategyMaps.MapTacks;

[UnturnedUI(BasePath = "UI")]
internal sealed class MapTackInfoUI : UnturnedUI
{
    private static readonly List<KeyValuePair<MapTackVehicleType, int>> WorkingVehicleCounts = new List<KeyValuePair<MapTackVehicleType, int>>();

    private readonly MapTackInfoUITranslations _translations;

    public UnturnedLabel Title { get; } = new UnturnedLabel("Hdr");
    public UnturnedLabel Location { get; } = new UnturnedLabel("Loc");

    public UnturnedLabel DestroyedAttribute { get; } = new UnturnedLabel("Attributes/Destroyed");
    public UnturnedLabel ProxiedAttribute { get; } = new UnturnedLabel("Attributes/Proxied");
    public UnturnedLabel LowBuildAttribute { get; } = new UnturnedLabel("Attributes/LowBuild");
    public UnturnedLabel LowAmmoAttribute { get; } = new UnturnedLabel("Attributes/LowAmmo");

    public UnturnedUIElement HealthBarRoot { get; } = new UnturnedUIElement("HP");
    public UnturnedLabel HealthBar { get; } = new UnturnedLabel("HP/Bar/Foreground");

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

    public void TryClose(WarfarePlayer player)
    {
        GameThread.AssertCurrent();

        Data? data = GetData<Data>(player.Steam64);
        if (data == null || !data.HasUI)
        {
            return;
        }

        data.HasUI = false;
        ClearFromPlayer(player.SteamPlayer);
    }

    public void Open(WarfarePlayer player, IMapTackUIHandler uiHandler)
    {
        GameThread.AssertCurrent();

        Data data = GetOrAddData(player.Steam64);
        LanguageSet set = new LanguageSet(player);

        ITransportConnection c = player.Connection;

        string title = uiHandler.GetTitle(in set);

        bool isDefaultState;

        if (!data.HasUI)
        {
            SendToPlayer(c, title);
            isDefaultState = true;
        }
        else
        {
            Title.SetText(c, title);
            isDefaultState = false;
        }

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

        uiHandler.CountVehicles(WorkingVehicleCounts);
        try
        {
            int vehicleMask = 0;
            foreach ((MapTackVehicleType tack, int count) in WorkingVehicleCounts)
            {
                VehicleCount ui = GetCount(tack);
                int mask = 1 << (int)tack;
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
                SetHealth(c, health.Value, vehicleMask);
                if (!isDefaultState)
                {
                    HealthBarRoot.Show(c);
                }
            }
            else
            {
                HealthBarRoot.Hide(c);
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
        }

        UpdateSupply(player, SupplyType.Build, uiHandler, data);
        UpdateSupply(player, SupplyType.Ammo, uiHandler, data);
    }

    private void UpdateSupply(WarfarePlayer player, SupplyType type, IMapTackUIHandler uiHandler, Data data)
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

        int? supply = uiHandler.GetSupplyCount(type);

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
    }

    private VehicleCount GetCount(MapTackVehicleType type)
    {
        if (type is <= MapTackVehicleType.Other or > MapTackVehicleType.Truck)
            throw new ArgumentOutOfRangeException(nameof(type), type, "Out of range vehicle type.");

        return Counts[(int)type - 1];
    }

    private void SetHealth(ITransportConnection c, double health, int vehicleMask)
    {
        health = Math.Clamp(health, 0d, 1d);

        int amt = GetMaxHealthBarCount(vehicleMask);
        string str = new string('█', (int)Math.Round(amt * health));

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

        int gndLength = (vehicleMask & (1 << (int)MapTackVehicleType.APC))
                        + (vehicleMask & (1 << (int)MapTackVehicleType.Humvee))
                        + (vehicleMask & (1 << (int)MapTackVehicleType.IFV))
                        + (vehicleMask & (1 << (int)MapTackVehicleType.MBT))
                        + (vehicleMask & (1 << (int)MapTackVehicleType.ScoutCar))
                        + (vehicleMask & (1 << (int)MapTackVehicleType.Truck));

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

    public class Data : IUnturnedUIData
    {
        public CSteamID Player { get; }
        public UnturnedUI Owner { get; }
        UnturnedUIElement? IUnturnedUIData.Element => null;

        public bool HasUI;
        public int VehicleMask;
        public bool HasBuildSupply;
        public bool HasAmmoSupply;

        public Data(CSteamID player, MapTackInfoUI owner)
        {
            Player = player;
            Owner = owner;
        }
    }
}

internal sealed class MapTackInfoUITranslations : TranslationCollection
{
    public override string Name => "UI/Map Tack Info";

    [TranslationData("Translation for the \"# BUILD\" part of the map tack UI.", "Build supply count.")]
    public readonly Translation<int> BuildSupplies = new Translation<int>("{0} BUILD", options: TranslationOptions.TMProUI | TranslationOptions.NoRichText);

    [TranslationData("Translation for the \"# AMMO\" part of the map tack UI.", "Ammo supply count.")]
    public readonly Translation<int> AmmoSupplies = new Translation<int>("{0} AMMO", options: TranslationOptions.TMProUI | TranslationOptions.NoRichText);
}