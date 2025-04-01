using Microsoft.Extensions.DependencyInjection;
using System;
using System.Globalization;
using Uncreated.Warfare.FOBs.Deployment;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Locations;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Commands;

[Command("go", "goto", "tp", "teleport", "warp"), SubCommandOf(typeof(ZoneCommand))]
internal sealed class ZoneGoCommand : IExecutableCommand
{
    private readonly ZoneStore _zoneStore;
    private readonly DeploymentService _deploymentService;
    private readonly ITranslationService _translationService;
    private readonly ZoneCommandTranslations _translations;
    private readonly DeploymentTranslations _deployTranslations;

    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    public ZoneGoCommand(
        ZoneStore zoneStore,
        TranslationInjection<ZoneCommandTranslations> translations,
        TranslationInjection<DeploymentTranslations> deployTranslations,
        DeploymentService deploymentService,
        ITranslationService translationService
    )
    {
        _zoneStore = zoneStore;
        _deploymentService = deploymentService;
        _translationService = translationService;
        _translations = translations.Value;
        _deployTranslations = deployTranslations.Value;
    }

    /// <inheritdoc />
    public UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        string teamNumberStr;
        Zone? zone = null;
        string? zname = null;

        // t1, t2, etc
        if (Context.HasArgs(1)
            && (teamNumberStr = Context.Get(0) ?? string.Empty).StartsWith("t", StringComparison.OrdinalIgnoreCase)
            && uint.TryParse(teamNumberStr.AsSpan(1), NumberStyles.Number, CultureInfo.InvariantCulture, out uint teamNumber))
        {
            ITeamManager<Team> teamManager = Context.ServiceProvider.GetRequiredService<ITeamManager<Team>>();

            Team team = teamManager.GetTeam(new CSteamID(teamNumber));
            if (team.IsValid)
            {
                zone = _zoneStore.SearchZone(ZoneType.MainBase, team.Faction);
                zname = zone?.Name;
            }
        }

        if (zone == null)
        {
            if (Context.TryGetRange(0, out zname))
            {
                zone = _zoneStore.SearchZone(zname);
            }
            else
            {
                zone = _zoneStore.FindInsideZone(Context.Player.Position, false);
                zname = zone?.Name;
            }
        }

        Vector3 pos;
        float yaw = Context.Player.UnturnedPlayer.transform.rotation.eulerAngles.y;
        GridLocation location = default;
        IDeployable? deployable = null;
        LocationDevkitNode? loc = null;
        if (zone == null)
        {
            if (GridLocation.TryParse(zname, out location))
            {
                Vector2 center2d = location.Center;
                pos = new Vector3(center2d.x, 0f, center2d.y);

                pos.y = TerrainUtility.GetHighestPoint(in pos, 0f) + 0.75f;
            }
            //else if (Data.Singletons.GetSingleton<FOBManager>() != null && FOBManager.TryFindFOB(zname!, Context.Player.GetTeam(), out deployable))
            //{
            //    pos = deployable.SpawnPosition;
            //    yaw = deployable.Yaw;
            //}
            //else if (F.StringFind(LocationDevkitNodeSystem.Get().GetAllNodes(), loc => loc.locationName, loc => loc.locationName.Length, zname!) is { } location2)
            //{
            //    pos = location2.transform.position;
            //    yaw = location2.transform.rotation.eulerAngles.y;
            //    loc = location2;
            //}
            else throw Context.Reply(_translations.ZoneNoResultsName);
        }
        else
        {
            yaw = zone.SpawnYaw;
            pos = zone.Spawn;
            pos.y += 0.5f;
        }

        if (deployable != null)
        {
            DeploySettings settings = default;
            _deploymentService.TryStartDeployment(Context.Player, deployable, in settings);
            Context.Reply(_deployTranslations.DeploySuccess, deployable);
            // todo: Context.LogAction(ActionLogType.Teleport, deployable.Translate(_translationService));
        }
        else
        {
            Context.Player.UnturnedPlayer.teleportToLocationUnsafe(pos, yaw);
            if (zone != null)
                Context.Reply(_translations.ZoneGoSuccess, zone);
            else if (loc != null)
                Context.Reply(_translations.ZoneGoSuccessRaw, loc.locationName);
            else
                Context.Reply(_translations.ZoneGoSuccessGridLocation, location);
            // todo: Context.LogAction(ActionLogType.Teleport, loc == null ? zone == null ? location.ToString() : zone.Name.ToUpper() : loc.locationName);
        }

        return UniTask.CompletedTask;
    }
}