using Uncreated.Warfare.Players.Cooldowns;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Addons;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.FOBs.Deployment;
public class DeploymentTranslations : TranslationCollection
{
    public override string Name => "Deployment";

    [TranslationData("Sent to a player after they deploy to a location.", "The location name.")]
    public readonly Translation<IDeployable> DeploySuccess = new Translation<IDeployable>("<#fae69c>You have arrived at {0}.", arg0Fmt: Flags.ColorNameFormat);

    [TranslationData("Sent to a player after they request deployment to a location and pass initial checks.", "The location name", "Seconds left")]
    public readonly Translation<IDeployable, int> DeployStandby = new Translation<IDeployable, int>("<#fae69c>Now deploying to {0}. You will arrive in <#eee>{1} ${p:1:second}</color>.", arg0Fmt: Flags.ColorNameFormat);

    [TranslationData("Sent to a player while after they request deployment to/from a location that is still being built.", "The location name")]
    public readonly Translation<IDeployable> DeployNotBuilt = new Translation<IDeployable>("<#ffa238>{0} is still under construction.", arg0Fmt: Flags.ColorNameFormat);

    [TranslationData("Sent to a player while after they request deployment to/from a location that is destroyed.", "The location name")]
    public readonly Translation<IDeployable> DeployDestroyed = new Translation<IDeployable>("<#ffa238>{0} is destroyed.", arg0Fmt: Flags.ColorNameFormat);

    [TranslationData("Sent to a player while they're waiting for deployment and the location get's destroyed.", "The location name")]
    public readonly Translation<IDeployable> DeployDestroyedTick = new Translation<IDeployable>("<#ffa238>{0} was destroyed.", arg0Fmt: Flags.ColorNameFormat);

    [TranslationData("Sent to a player who tried to deploy to/from a proxied FOB.", "The location name")]
    public readonly Translation<IDeployable> DeployProxied = new Translation<IDeployable>("<#ffa238>{0} is currently being overtaken by enemies.", arg0Fmt: Flags.ColorNameFormat);

    [TranslationData("Sent to a player while they're waiting for deployment and the location get's destroyed.", "The location name")]
    public readonly Translation<IDeployable> DeployProxiedTick = new Translation<IDeployable>("<#ffaa42>You cannot deploy to {0} - it is being overtaken by enemies.");

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

    [TranslationData("Sent to a player while they try to deploy to a Rally Point map tack but they aren't in the correct squad.")]
    public readonly Translation<int, Squad> DeployRallyPointWrongSquad = new Translation<int, Squad>("<#ffa238>Join Squad <#9effc6>{0}</color> - {1} in order to use their <#67ff85>Rally Point</color>.", arg0Fmt: UppercaseAddon.Instance);

    [TranslationData("Sent to a player after they request deployment to a FOB if they're already on the FOB.", "The location name")]
    public readonly Translation<IDeployable> DeployableAlreadyOnFOB = new Translation<IDeployable>("<#ffa238>You are already on <#e3c27f>{0}</color>.", arg0Fmt: Flags.ColorNameFormat);

    [TranslationData("Sent to a player after they request deployment to a location if they're not near another FOB or in main (in non-insurgency gamemodes).")]
    public readonly Translation DeployNotNearFOB = new Translation("<#ffa238>You must be near a friendly <#cedcde>FOB</color> in order to deploy.");

    [TranslationData("Sent to a player after they try to cancel deployment when they aren't deploying.")]
    public readonly Translation DeployCancelNotDeploying = new Translation("<#ffa238>You aren't deploying anywhere.");

    [TranslationData("Sent to a player after they request deployment to a location if they're not near another FOB or in main (in the insurgency gamemode).")]
    public readonly Translation DeployNotNearFOBInsurgency = new Translation("<#ffa238>You must be near a friendly <#cedcde>FOB</color> or <#e8d1a7>CACHE</color> in order to deploy.");

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

    [TranslationData("Sent to a player after they try to deploy to main while not on a valid team.")]
    public readonly Translation DeployNotOnTeam = new Translation("<#ffaa42>You are not on a team.");

    [TranslationData("Sent to a player after they try to deploy to main while already in main.")]
    public readonly Translation DeployAlreadyInMain = new Translation("<#ffaa42>You are already at the main base.");
}
