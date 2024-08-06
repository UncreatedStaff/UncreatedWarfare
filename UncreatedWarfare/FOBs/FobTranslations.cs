using Uncreated.Warfare.Locations;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.FOBs;
public class FobTranslations : PropertiesTranslationCollection
{
    protected override string FileName => "FOBs";

    [TranslationData("Indicates that a buildable can only be placed within a FOB's active radius.")]
    public readonly Translation BuildNotInRadius = new Translation("<#ffab87>This can only be placed inside <#cedcde>FOB RADIUS</color>.");

    [TranslationData("Indicates that a buildable that is being shoveled can is no longer within a FOB's active radius.")]
    public readonly Translation BuildTickNotInRadius = new Translation("<#ffab87>There's no longer a friendly FOB nearby.");

    [TranslationData("Indicates that a buildable can only be placed within a FOB's active radius, which is currently reduced because it doesn't have a bunker.", "Maximum radius")]
    public readonly Translation<float> BuildSmallRadius = new Translation<float>("<#ffab87>This can only be placed within {0}m of this FOB Radio right now. Expand this range by building a <#cedcde>FOB BUNKER</color>.", "N0");

    [TranslationData("Sent to the player when they do /build.")]
    public readonly Translation BuildLegacyExplanation = new Translation("<#ffab87>Hit the foundation with your Entrenching Tool to build it.");

    [TranslationData("Indicates that a buildable can only be placed within the radius of a FOB Radio.", "Maximum radius")]
    public readonly Translation<float> BuildNoRadio = new Translation<float>("<#ffab87>This can only be placed within {0}m of a friendly <#cedcde>FOB RADIO</color>.", "N0");

    [TranslationData("Indicates that the maximum amount of buildables of the given type have already been built on this FOB.", "Maximum amount", "Buildable type")]
    public readonly Translation<int, BuildableData> BuildLimitReached = new Translation<int, BuildableData>("<#ffab87>This FOB already has {0} {1}.", "F0", FormatPlural + "{0}");

    [TranslationData("Indicates that the maximum amount of buildables of the given type have already been built in the general area.", "Maximum amount", "Buildable type")]
    public readonly Translation<int, BuildableData> RegionalBuildLimitReached = new Translation<int, BuildableData>("<#ffab87>You cannot place more than {0} {1} in this area.", "F0", FormatPlural + "{0}");

    [TranslationData("Indicates that the maximum amount of buildables of the given type have already been built on this FOB while digging out a new building.", "Buildable type")]
    public readonly Translation<BuildableData> BuildTickStructureExists = new Translation<BuildableData>("<#ffab87>Too many {0} have already been built on this FOB.", FormatPlural);

    [TranslationData("Indicates that a buildable can only be placed within a friendly FOB.")]
    public readonly Translation BuildEnemy = new Translation("<#ffab87>You may not build on an enemy FOB.");

    [TranslationData("Indicates that a buildable needs more building supplies to be built.", "Total supplies available", "Required supplies")]
    public readonly Translation<int, int> BuildMissingSupplies = new Translation<int, int>("<#ffab87>You're missing nearby build! <#c$build$>Building Supplies: <#e0d8b8>{0}/{1}</color></color>.");

    [TranslationData("Indicates that no more FOBs can be built because the maximum amount has been reached.")]
    public readonly Translation BuildMaxFOBsHit = new Translation("<#ffab87>The max number of FOBs on your team has been reached.");

    [TranslationData("Indicates that FOBs can not be built underwater.")]
    public readonly Translation BuildFOBUnderwater = new Translation("<#ffab87>You can't build a FOB underwater.");

    [TranslationData("Indicates that FOBs can not be built this high above the terrain.", "Maximum distance (in meters) above terrain")]
    public readonly Translation<float> BuildFOBTooHigh = new Translation<float>("<#ffab87>You can't build a FOB more than {0}m above the ground.", "F0");

    [TranslationData("Indicates that FOBs can not be built this close to a main base.")]
    public readonly Translation BuildFOBTooCloseToMain = new Translation("<#ffab87>You can't build a FOB this close to main base.");

    [TranslationData("Indicates that FOBs can only be built near friendly ground/air logistics vehicle.")]
    public readonly Translation BuildNoLogisticsVehicle = new Translation("<#ffab87>You must be near a friendly <#cedcde>LOGISTICS VEHICLE</color> to place a FOB radio.");

    [TranslationData("Indicates that a FOB can't be built because another FOB is already nearby.", "The existing FOB's name", "Distance from the existing FOB", "Minimum distance needed")]
    public readonly Translation<FOB, float, float> BuildFOBTooClose = new Translation<FOB, float, float>("<#ffa238>You are too close to an existing FOB Radio ({0}: {1}m away). You must be at least {2}m away to place a new radio.", FOB.FormatNameColored, "F0", "F0");

    [TranslationData("Indicates that a FOB can't be built because another FOB bunker is already nearby.", "Distance from the existing bunker", "Minimum distance needed")]
    public readonly Translation<float, float> BuildBunkerTooClose = new Translation<float, float>("<#ffa238>You are too close to an existing FOB Bunker ({0}m away). You must be at least {1}m away to place a new radio.", "F0", "F0");

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation BuildInvalidAsset = new Translation("<#ffa238>This buildable has invalid barricade assets (contact devs).");

    [TranslationData("Indicates that a player doesn't have permissions or is missing the required kit, etc. to place a buildable.")]
    public readonly Translation BuildableNotAllowed = new Translation("<#ffa238>You are not allowed to place this buildable.");

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<IDeployable, GridLocation, string> FOBUI = new Translation<IDeployable, GridLocation, string>("{0}  <color=#d6d2c7>{1}</color>  {2}", TranslationOptions.UnityUI, FOB.FormatNameColored);

    [TranslationData("Shows on the HUD when a cache has been destroyed and the player is on attack.")]
    public readonly Translation CacheDestroyedAttack = new Translation("<#e8d1a7>WEAPONS CACHE HAS BEEN ELIMINATED", TranslationOptions.TMProUI);

    [TranslationData("Shows on the HUD when a cache has been destroyed and the player is on defense.")]
    public readonly Translation CacheDestroyedDefense = new Translation("<#deadad>WEAPONS CACHE HAS BEEN DESTROYED", TranslationOptions.TMProUI);

    [TranslationData("Shows on the HUD when a cache has been discovered and the player is on attack.", "The closest location name to the cache.")]
    public readonly Translation<string> CacheDiscoveredAttack = new Translation<string>("<color=#e8d1a7>NEW WEAPONS CACHE DISCOVERED NEAR <color=#e3c59a>{0}</color></color>", TranslationOptions.TMProUI, FormatUppercase);

    [TranslationData("Shows on the HUD when a cache has been discovered and the player is on defense.")]
    public readonly Translation CacheDiscoveredDefense = new Translation("<#d9b9a7>WEAPONS CACHE HAS BEEN COMPROMISED, DEFEND IT", TranslationOptions.TMProUI);

    [TranslationData("Shows on the HUD when a new cache spawns and the player is on defense.")]
    public readonly Translation CacheSpawnedDefense = new Translation("<#a8e0a4>NEW WEAPONS CACHE IS NOW ACTIVE", TranslationOptions.TMProUI);
}
