using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Addons;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.FOBs.Deployment;
public class DeploymentTranslations : PropertiesTranslationCollection
{
    protected override string FileName => "Deployment";

    [TranslationData("Sent to a player after they deploy to a location.", "The location name.")]
    public readonly Translation<IDeployable> DeploySuccess = new Translation<IDeployable>("<#fae69c>You have arrived at {0}.", arg0Fmt: Flags.ColorNameFormat);

    [TranslationData("Sent to a player after they request deployment to a location and pass initial checks.", "The location name", "Seconds left")]
    public readonly Translation<IDeployable, int> DeployStandby = new Translation<IDeployable, int>("<#fae69c>Now deploying to {0}. You will arrive in <#eee>{1} ${p:1:second}</color>", arg0Fmt: Flags.ColorNameFormat);

    [TranslationData("Sent to a player after they request deployment to a location and the location isn't valid, gets destroyed, etc.", "The location name")]
    public readonly Translation<IDeployable> DeployNotSpawnable = new Translation<IDeployable>("<#ffa238>{0} is not active.", arg0Fmt: Flags.ColorNameFormat);

    [TranslationData("Sent to a player while they're waiting for deployment and the location becomes invalid.", "The location name")]
    public readonly Translation<IDeployable> DeployNotSpawnableTick = new Translation<IDeployable>("<#ffa238>{0} is no longer active.", arg0Fmt: Flags.ColorNameFormat);

    [TranslationData("Sent to a player while they're waiting for deployment and the location get's destroyed.", "The location name")]
    public readonly Translation<IDeployable> DeployDestroyed = new Translation<IDeployable>("<#ffa238>{0} was destroyed.", arg0Fmt: Flags.ColorNameFormat);

    [TranslationData("Sent to a player after they request deployment to a FOB if it doesn't have a Bunker.", "The location name")]
    public readonly Translation<IDeployable> DeployNoBunker = new Translation<IDeployable>("<#ffaa42>{0} doesn't have a <#cedcde>FOB BUNKER</color>. Your team must build one to use the <#cedcde>FOB</color> as a spawnpoint.", arg0Fmt: Flags.ColorNameFormat);

    [TranslationData("Sent to a player after they request deployment to a FOB if it's radio is damaged.", "The location name")]
    public readonly Translation<IDeployable> DeployRadioDamaged = new Translation<IDeployable>("<#ffaa42>The <#cedcde>FOB RADIO</color> at {0} is damaged. Repair it with an <#cedcde>ENTRENCHING TOOL</color>.", arg0Fmt: Flags.ColorNameFormat);

    [TranslationData("Sent to a player while they're waiting for deployment if they move mid-deployment.")]
    public readonly Translation DeployMoved = new Translation("<#ffa238>You moved and can no longer deploy.");

    [TranslationData("Sent to a player while they're waiting for deployment if they get damaged mid-deployment.")]
    public readonly Translation DeployDamaged = new Translation("<#ffa238>You were damaged and can no longer deploy.");

    [TranslationData("Sent to a player while they're waiting for deployment if enemies go within their range.", "The location name")]
    public readonly Translation<IDeployable> DeployEnemiesNearbyTick = new Translation<IDeployable>("<#ffa238>You no longer deploy to {0} - there are enemies nearby.", arg0Fmt: Flags.ColorNameFormat);

    [TranslationData("Sent to a player after they request deployment to a FOB if there are enemies nearby.", "The location name")]
    public readonly Translation<IDeployable> DeployEnemiesNearby = new Translation<IDeployable>("<#ffaa42>You cannot deploy to {0} - there are enemies nearby.");

    [TranslationData("Sent to a player while they're waiting for deployment if they cancel it.")]
    public readonly Translation DeployCancelled = new Translation("<#fae69c>Active deployment cancelled.");

    [TranslationData("Sent to a player while they're waiting for deployment if they cancel it.")]
    public readonly Translation<string> DeployableNotFound = new Translation<string>("<#ffa238>There is no location by the name of <#e3c27f>{0}</color>.", arg0Fmt: UppercaseAddon.Instance);

    [TranslationData("Sent to a player after they request deployment to a FOB if they're already on the FOB.", "The location name")]
    public readonly Translation<IDeployable> DeployableAlreadyOnFOB = new Translation<IDeployable>("<#ffa238>You are already on <#e3c27f>{0}</color>.", arg0Fmt: Flags.ColorNameFormat);

    [TranslationData("Sent to a player after they request deployment to a location if they're not near another FOB or in main (in non-insurgency gamemodes).")]
    public readonly Translation DeployNotNearFOB = new Translation("<#ffa238>You must be near a friendly <#cedcde>FOB</color> or in <#cedcde>MAIN BASE</color> in order to deploy.");

    [TranslationData("Sent to a player after they try to cancel deployment when they aren't deploying.")]
    public readonly Translation DeployCancelNotDeploying = new Translation("<#ffa238>You aren't deploying anywhere.");

    [TranslationData("Sent to a player after they request deployment to a location if they're not near another FOB or in main (in the insurgency gamemode).")]
    public readonly Translation DeployNotNearFOBInsurgency = new Translation("<#ffa238>You must be near a friendly <#cedcde>FOB</color> or <#e8d1a7>CACHE</color>, or in <#cedcde>MAIN BASE</color> in order to deploy.");

    [TranslationData("Sent to a player after they request deployment to a location if they're on cooldown.", "Time left")]
    public readonly Translation<Cooldown> DeployCooldown = new Translation<Cooldown>("<#ffa238>You can deploy again in: <#e3c27f>{0}</color>", arg0Fmt: Cooldown.FormatTimeLong);

    [TranslationData("Sent to a player after they request deployment to a location if they're already deploying somewhere.", "Location already being deployed to")]
    public readonly Translation<IDeployable> DeployAlreadyActive = new Translation<IDeployable>("<#b5a591>You're already deploying to {0}, do <#fff>/deploy abort</color> before deploying again.");

    [TranslationData("Sent to a player after they request deployment to a location if they're on combat cooldown.", "Time left")]
    public readonly Translation<Cooldown> DeployInCombat = new Translation<Cooldown>("<#ffaa42>You are still in combat! You can deploy in another: <#e3987f>{0}</color>.", arg0Fmt: Cooldown.FormatTimeLong);

    [TranslationData("Sent to a player after they request deployment to a location if they're injured.")]
    public readonly Translation DeployInjured = new Translation("<#ffaa42>You can not deploy while injured, get a medic to revive you or give up.");

    [TranslationData("Sent to a player after they request deployment to the lobby (this feature was moved to /teams).")]
    public readonly Translation DeployLobbyRemoved = new Translation("<#fae69c>The lobby has been removed, use <#e3c27f>/teams</color> to switch teams instead.");

    [TranslationData("Sent to a player after they were busy deploying to a rally point which then got burned.")]
    public readonly Translation RallyPointBurned = new Translation("<#ffaa42>Rallypoint is no longer available - there are are enemies nearby.");
}
