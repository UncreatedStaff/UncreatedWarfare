using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.FOBs.Construction;
using Uncreated.Warfare.FOBs.Deployment;
using Uncreated.Warfare.Locations;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Addons;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.FOBs;
public class FobTranslations : PropertiesTranslationCollection
{
    protected override string FileName => "FOBs";

    [TranslationData("Indicates that a buildable can only be placed within a FOB's active radius.")]
    public readonly Translation BuildNotInRadius = new Translation("<#ffab87>This can only be placed inside a <#cedcde>FOB RADIUS</color>.");

    [TranslationData("Indicates that a buildable that is being shoveled can is no longer within a FOB's active radius.")]
    public readonly Translation BuildTickNotInRadius = new Translation("<#ffab87>There's no longer a friendly FOB nearby.");

    [TranslationData("Indicates that a buildable can only be placed within a FOB's active radius, which is currently reduced because it doesn't have a bunker.", "Maximum radius")]
    public readonly Translation<float> BuildSmallRadius = new Translation<float>("<#ffab87>This can only be placed within {0}m of this FOB Radio right now. Expand this range by building a <#cedcde>FOB BUNKER</color>.", arg0Fmt: "N0");

    [TranslationData("Sent to the player when they do /build.")]
    public readonly Translation BuildLegacyExplanation = new Translation("<#ffab87>Hit the foundation with your Entrenching Tool to build it.");

    [TranslationData("Sent to the player when they try to take a supply crate from a trunk.")]
    public readonly Translation CantTakeSupplyCrate = new Translation("<#ffab87>Drop the supply crate from the trunk directly to restock a FOB.");

    [TranslationData("Indicates that the maximum amount of buildables of the given type have already been built on this FOB.", "Maximum amount", "Buildable type")]
    public readonly Translation<int, ShovelableInfo> BuildLimitReached = new Translation<int, ShovelableInfo>("<#ffab87>This FOB already has {0} {1}.", arg0Fmt: "F0", arg1Fmt: PluralAddon.WhenArgument(0));

    [TranslationData("Indicates that the maximum amount of buildables of the given type have already been built in the general area.", "Maximum amount", "Buildable type")]
    public readonly Translation<int, ShovelableInfo> RegionalBuildLimitReached = new Translation<int, ShovelableInfo>("<#ffab87>You cannot place more than {0} {1} in this area.", arg0Fmt: "F0", arg1Fmt: PluralAddon.WhenArgument(0));

    [TranslationData("Indicates that the maximum amount of buildables of the given type have already been built on this FOB while digging out a new building.", "Buildable type")]
    public readonly Translation<ShovelableInfo> BuildTickStructureExists = new Translation<ShovelableInfo>("<#ffab87>Too many {0} have already been built on this FOB.", arg0Fmt: PluralAddon.Always());

    [TranslationData("Indicates that a buildable can only be placed within a friendly FOB.")]
    public readonly Translation BuildEnemy = new Translation("<#ffab87>You may not build on an enemy FOB.");
    
    [TranslationData("Indicates that a player cannot shovel up an enemy fob, emplacement or fortification.")]
    public readonly Translation ShovelableNotFriendly = new Translation("<#ffab87>You cannot shovel up enemy fortifications.");

    [TranslationData("Indicates that a buildable needs more building supplies to be built.", "Progress shovel hits", "Required shovel hits")]
    public readonly Translation<float, float> BuildMissingSupplies = new Translation<float, float>("<#ffab87>You're missing nearby building supplies! <#f3ce82>Build required: <#e0d8b8>{0}/{1}</color></color>.");

    [TranslationData("Indicates that a buildable can only be placed nearby a supply crate.")]
    public readonly Translation BuildFOBNoSupplyCrate = new Translation("<#ffab87>You must be near a friendly <#cedcde>SUPPLY CRATE</color> in order to build a FOB.");

    [TranslationData("Indicates that a FOB can't be placed in a weird position, like on a vehicle.")]
    public readonly Translation BuildFOBInvalidPosition = new Translation("<#ffab87>You can not build FOBs here.");

    [TranslationData("Indicates that a FOB buildable can't be placed in a weird position, like on a vehicle.")]
    public readonly Translation BuildFOBBuildableInvalidPosition = new Translation("<#ffab87>You can not build this here.");

    [TranslationData("Indicates that no more FOBs can be built because the maximum amount has been reached.")]
    public readonly Translation BuildMaxFOBsHit = new Translation("<#ffab87>The max number of FOBs on your team has been reached.");

    [TranslationData("Indicates that FOBs can not be built underwater.")]
    public readonly Translation BuildFOBUnderwater = new Translation("<#ffab87>You can't build a FOB underwater.");

    [TranslationData("Indicates that FOBs can not be built this high above the terrain.", "Maximum distance (in meters) above terrain")]
    public readonly Translation<float> BuildFOBTooHigh = new Translation<float>("<#ffab87>You can't build a FOB more than {0}m above the ground.", arg0Fmt: "F0");

    [TranslationData("Indicates that FOBs can not be built this close to a main base.")]
    public readonly Translation BuildFOBTooCloseToMain = new Translation("<#ffab87>You can't build a FOB this close to main base.");

    [TranslationData("Indicates that FOBs can only be built near friendly ground/air logistics vehicle.")]
    public readonly Translation BuildNoLogisticsVehicle = new Translation("<#ffab87>You must be near a friendly <#cedcde>LOGISTICS VEHICLE</color> to place a FOB radio.");

    [TranslationData("Indicates that a FOB can't be built because another FOB is already nearby.", "The existing FOB's name", "Distance from the existing FOB", "Minimum distance needed")]
    public readonly Translation<IFob, float, float> BuildFOBTooClose = new Translation<IFob, float, float>("<#ffa238>You are too close to an existing FOB ({0}: {1}m away). You must be at least {2}m away to place a new FOB.", arg0Fmt: Flags.ColorNameFormat, arg1Fmt: "F0", arg2Fmt: "F0");

    [TranslationData("Indicates that a FOB can't be built because another FOB bunker is already nearby.", "Distance from the existing bunker", "Minimum distance needed")]
    public readonly Translation<float, float> BuildBunkerTooClose = new Translation<float, float>("<#ffa238>You are too close to an existing FOB Bunker ({0}m away). You must be at least {1}m away to place a new radio.", arg0Fmt: "F0", arg1Fmt: "F0");

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation BuildInvalidAsset = new Translation("<#ffa238>This buildable has invalid barricade assets (contact devs).");

    [TranslationData("Indicates that a player doesn't have permissions or is missing the required kit, etc. to place a buildable.")]
    public readonly Translation BuildableNotAllowed = new Translation("<#ffa238>You are not allowed to place this buildable.");
    
    [TranslationData("Indicates that a player isn't able to place traps in a FOB.")]
    public readonly Translation TrapNotAllowed = new Translation("<#ffa238>Traps can not be placed this close to FOBs.");
    
    [TranslationData("Indicates that a player cannot place a rally point because there are enemies nearby.")]
    public readonly Translation PlaceRallyPointNearbyEnemies = new Translation("<#ffaa42>Rally point unavailable - there are enemies nearby.");
    
    [TranslationData("Indicates that a player cannot place a rally point because of environmental restrictions.")]
    public readonly Translation PlaceRallyPointInvalid = new Translation("<#ffaa42>You may not place rally points here.");
    
    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<IDeployable, GridLocation, string> FOBUI = new Translation<IDeployable, GridLocation, string>("{0}  <color=#d6d2c7>{1}</color>  {2}", TranslationOptions.UnityUI, arg0Fmt: Flags.ColorNameFormat);

    [TranslationData(IsPriorityTranslation = false)]
    public Translation<float> ToastGainBuild = new Translation<float>("<color=#f3ce82>+{0} BUILD</color>", TranslationOptions.TMProUI);

    [TranslationData(IsPriorityTranslation = false)]
    public Translation<float> ToastLoseBuild = new Translation<float>("<color=#f3ce82>-{0} BUILD</color>", TranslationOptions.TMProUI);

}
