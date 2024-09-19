using System;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.ValueFormatters;

namespace Uncreated.Warfare.Fobs;

/// <summary>
/// Data structure to store information about items and vehicles that can be built on FOBs.
/// </summary>
public class BuildableData : ITranslationArgument
{
    /// <summary>
    /// The foundation barricade or structure that is placed before building.
    /// </summary>
    public IAssetLink<ItemPlaceableAsset> Foundation { get; set; }

    /// <summary>
    /// The structure or barricade that is actually built when building is completed.
    /// </summary>
    public IAssetLink<ItemPlaceableAsset>? FullBuildable { get; set; }

    /// <summary>
    /// Type of buildable.
    /// </summary>
    public BuildableType Type { get; set; }

    /// <summary>
    /// Number of shovels required to build.
    /// </summary>
    public int RequiredHits { get; set; }

    /// <summary>
    /// Number of building supplies required to build.
    /// </summary>
    public int RequiredBuild { get; set; }

    /// <summary>
    /// Single faction allowed to build.
    /// </summary>
    public string Faction { get; set; }
    
    /// <summary>
    /// List of factions allowed to build.
    /// </summary>
    public string[] Factions { get; set; }

    /// <summary>
    /// Maximum number of this buildable that can be on a FOB.
    /// </summary>
    public int Limit { get; set; }

    /// <summary>
    /// If this buildable is temporarily disabled.
    /// </summary>
    public bool Disabled { get; set; }

    /// <summary>
    /// Vehicle spawning data for this buildable.
    /// </summary>
    public EmplacementData? Emplacement { get; set; }

    /// <summary>
    /// If this buildable shouldn't be auto-whitelisted.
    /// </summary>
    public bool DontAutoWhitelist { get; set; }

    /// <summary>
    /// Checks if a faction is matched by the filter.
    /// </summary>
    public bool IsFactionEnabled(FactionInfo faction)
    {
        if (faction.FactionId.Equals(Faction, StringComparison.Ordinal))
            return true;

        if (Factions == null)
            return false;

        for (int i = 0; i < Factions.Length; ++i)
        {
            if (faction.FactionId.Equals(Factions[i], StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    /// <inheritdoc />
    public string Translate(ITranslationValueFormatter formatter, in ValueFormatParameters parameters)
    {
        if (Emplacement is not null && Emplacement.EmplacementVehicle.TryGetAsset(out VehicleAsset? vasset))
        {
            string name = vasset.vehicleName;

            return name;
        }

        if (Foundation.TryGetAsset(out ItemAsset? iasset) || FullBuildable.TryGetAsset(out iasset))
        {
            string name = GetItemName(iasset.itemName);

            return name;
        }

        if (Emplacement is not null)
        {
            if (Emplacement.BaseBuildable.TryGetAsset(out iasset))
            {
                string name = GetItemName(iasset.itemName);

                return name;
            }
            if (Emplacement.Ammo.TryGetAsset(out iasset))
            {
                string name = GetItemName(iasset.itemName);

                return name;
            }
        }

        return formatter.FormatEnum(Type, parameters.Language);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        if (Emplacement is not null && Emplacement.EmplacementVehicle.TryGetAsset(out VehicleAsset? vasset))
        {
            return vasset.vehicleName;
        }

        if (Foundation.TryGetAsset(out ItemAsset? iasset) || FullBuildable.TryGetAsset(out iasset))
        {
            return GetItemName(iasset.itemName);
        }

        if (Emplacement is not null)
        {
            if (Emplacement.BaseBuildable.TryGetAsset(out iasset))
            {
                return GetItemName(iasset.itemName);
            }
            if (Emplacement.Ammo.TryGetAsset(out iasset))
            {
                return GetItemName(iasset.itemName);
            }
        }

        return Type.ToString();
    }

    private static string GetItemName(string itemName)
    {
        int ind = itemName.IndexOf(" Built", StringComparison.OrdinalIgnoreCase);
        if (ind != -1)
            itemName = itemName.Substring(0, ind);
        return itemName;
    }
}