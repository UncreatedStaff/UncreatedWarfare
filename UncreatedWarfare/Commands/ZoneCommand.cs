using SDG.NetTransport;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Locations;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Permissions;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Commands;

[Command("zone")]
[MetadataFile(nameof(GetHelpMetadata))]
public class ZoneCommand : IExecutableCommand
{
    private readonly ZoneStore _zoneStore;
    private const string Syntax = "/zone <visualize|go|util>";
    private const string Help = "Manage zones.";

    internal static readonly PermissionLeaf PermissionVisualize = new PermissionLeaf("commands.zone.visualize", unturned: false, warfare: true);
    internal static readonly PermissionLeaf PermissionGo        = new PermissionLeaf("commands.zone.go", unturned: false, warfare: true);
    internal static readonly PermissionLeaf PermissionUtil      = new PermissionLeaf("commands.zone.util", unturned: false, warfare: true);
    internal static readonly PermissionLeaf PermissionLocation  = new PermissionLeaf("commands.zone.util.location", unturned: false, warfare: true);

    /// <inheritdoc />
    public CommandContext Context { get; set; }

    public ZoneCommand(ZoneStore zoneStore)
    {
        _zoneStore = zoneStore;
    }

    /// <summary>
    /// Get /help metadata about this command.
    /// </summary>
    public static CommandStructure GetHelpMetadata()
    {
        return new CommandStructure
        {
            Description = Help,
            Parameters =
            [
                new CommandParameter("Visualize")
                {
                    Aliases = [ "vis" ],
                    Description = "Spawns particles highlighting the zone border.",
                    Permission = PermissionVisualize,
                    Parameters =
                    [
                        new CommandParameter("Zone", typeof(Zone))
                    ]
                },
                new CommandParameter("Go")
                {
                    Description = "Teleport to the spawn of a zone.",
                    Permission = PermissionGo,
                    Parameters =
                    [
                        new CommandParameter("Zone", typeof(Zone))
                    ],
                },
                new CommandParameter("Util")
                {
                    Description = "Random zone utilities.",
                    Permission = PermissionUtil,
                    Parameters =
                    [
                        new CommandParameter("Location")
                        {
                            Aliases = [ "position", "loc", "pos" ],
                            Description = "Responds with the player's coordinates and yaw.",
                            Permission = PermissionLocation
                        }
                    ]
                }
            ]
        };
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertHelpCheck(0, Syntax + " - " + Help);

        Context.AssertArgs(1, "zone_syntax");

        if (Context.MatchParameter(0, "visualize", "vis"))
        {
            Context.AssertRanByPlayer();

            await Context.AssertPermissions(PermissionVisualize, token);
            await UniTask.SwitchToMainThread(token);

            Context.ArgumentOffset = 1;
            Visualize();
            Context.ArgumentOffset = 0;
        }
        else if (Context.MatchParameter(0, "go", "tp", "goto", "teleport"))
        {
            Context.AssertRanByPlayer();

            Context.AssertOnDuty();

            await Context.AssertPermissions(PermissionGo, token);
            await UniTask.SwitchToMainThread(token);

            Context.ArgumentOffset = 1;
            Go();
            Context.ArgumentOffset = 0;
        }
        else if (Context.MatchParameter(0, "util", "u", "tools"))
        {
            Context.AssertRanByPlayer();

            await Context.AssertPermissions(PermissionUtil, token);
            await UniTask.SwitchToMainThread(token);

            if (Context.MatchParameter(0, "location", "position", "loc", "pos"))
            {
                Vector3 p = Context.Player.Position;
                throw Context.Reply(T.ZoneUtilLocation, p.x, p.y, p.z, Context.Player.Yaw);
            }
            
            throw Context.SendCorrectUsage("/zone util location");
        }
        else throw Context.SendCorrectUsage(Syntax);
    }
    private void Visualize()
    {
        Zone? zone;
        if (Context.TryGetRange(0, out string? zname))
            zone = _zoneStore.SearchZone(zname);
        else
        {
            Vector3 plpos = Context.Player.Position;
            if (!Context.Player.IsOnline) return; // player got kicked
            zone = _zoneStore.FindInsizeZone(plpos, false);
        }

        if (zone == null) throw Context.Reply(T.ZoneNoResults);

        Vector2[] points = zone.GetParticleSpawnPoints(out Vector2[] corners, out Vector2 center);
        ITransportConnection channel = Context.Player.UnturnedPlayer.channel.owner.transportConnection;
        bool hasui = ZonePlayerComponent.Airdrop != null;

        foreach (Vector2 point in points)
        {   // Border
            Vector3 pos = new Vector3(point.x, 0f, point.y);
            pos.y = F.GetHeight(pos, zone.MinHeight);
            F.TriggerEffectReliable(ZonePlayerComponent.Side, channel, pos);
            if (hasui)
                F.TriggerEffectReliable(ZonePlayerComponent.Airdrop!, channel, pos);
        }

        foreach (Vector2 point in corners)
        {   // Corners
            Vector3 pos = new Vector3(point.x, 0f, point.y);
            pos.y = F.GetHeight(pos, zone.MinHeight);
            F.TriggerEffectReliable(ZonePlayerComponent.Corner, channel, pos);
            if (hasui)
                F.TriggerEffectReliable(ZonePlayerComponent.Airdrop!, channel, pos);
        }

        {   // Center
            Vector3 pos = new Vector3(center.x, 0f, center.y);
            pos.y = F.GetHeight(pos, zone.MinHeight);
            F.TriggerEffectReliable(ZonePlayerComponent.Center, channel, pos);
            if (hasui)
                F.TriggerEffectReliable(ZonePlayerComponent.Airdrop!, channel, pos);
        }

        Context.Player.UnturnedPlayer.StartCoroutine(ClearPoints(Context.Player));
        Context.Reply(T.ZoneVisualizeSuccess, points.Length + corners.Length + 1, zone);
    }
    private IEnumerator ClearPoints(WarfarePlayer player)
    {
        yield return new WaitForSecondsRealtime(60f);
        if (player == null) yield break;
        ITransportConnection channel = player.Connection;
        if (ZonePlayerComponent.Airdrop != null)
            EffectManager.askEffectClearByID(ZonePlayerComponent.Airdrop.id, channel);
        EffectManager.askEffectClearByID(ZonePlayerComponent.Side.id, channel);
        EffectManager.askEffectClearByID(ZonePlayerComponent.Corner.id, channel);
        EffectManager.askEffectClearByID(ZonePlayerComponent.Center.id, channel);
    }
    private void Go()
    {
        Zone? zone;
        if (Context.TryGetRange(0, out string? zname))
            zone = _zoneStore.SearchZone(zname);
        else
        {
            Vector3 plpos = Context.Player.Position;
            if (!Context.Player.IsOnline) return; // player got kicked
            zone = _zoneStore.FindInsizeZone(plpos, false);
            zname = zone?.Name;
        }

        Vector2 pos;
        float yaw = Context.Player.UnturnedPlayer.transform.rotation.eulerAngles.y;
        GridLocation location = default;
        IDeployable? deployable = null;
        LocationDevkitNode? loc = null;
        if (zone == null)
        {
            if (GridLocation.TryParse(zname, out location))
            {
                pos = location.Center;
            }
            else if (Data.Singletons.GetSingleton<FOBManager>() != null && FOBManager.TryFindFOB(zname!, Context.Player.GetTeam(), out deployable))
            {
                pos = deployable.SpawnPosition;
                yaw = deployable.Yaw;
            }
            else if (F.StringFind(LocationDevkitNodeSystem.Get().GetAllNodes(), loc => loc.locationName, loc => loc.locationName.Length, zname!) is { } location2)
            {
                pos = location2.transform.position;
                yaw = location2.transform.rotation.eulerAngles.y;
                loc = location2;
            }
            else throw Context.Reply(T.ZoneNoResultsName);
        }
        else
        {
            pos = zone.Center;
            if (zone == TeamManager.Team1Main)
                yaw = TeamManager.Team1SpawnAngle;
            else if (zone == TeamManager.Team2Main)
                yaw = TeamManager.Team2SpawnAngle;
            else if (zone == TeamManager.LobbyZone)
                yaw = TeamManager.LobbySpawnAngle;
        }

        if (deployable != null)
        {
            Deployment.ForceDeploy(Context.Player, null, deployable, false, false);
            Context.Reply(T.DeploySuccess, deployable);
            Context.LogAction(ActionLogType.Teleport, deployable.Translate(Localization.GetDefaultLanguage(), Context.Player.GetTeam(), FOB.FormatLocationName));
        }
        else if (Physics.Raycast(new Ray(new Vector3(pos.x, Level.HEIGHT, pos.y), Vector3.down), out RaycastHit hit, Level.HEIGHT, RayMasks.BLOCK_COLLISION))
        {
            Context.Player.UnturnedPlayer.teleportToLocationUnsafe(hit.point, yaw);
            if (zone != null)
                Context.Reply(T.ZoneGoSuccess, zone);
            else if (loc != null)
                Context.Reply(T.ZoneGoSuccessRaw, loc.locationName);
            else
                Context.Reply(T.ZoneGoSuccessGridLocation, location);
            Context.LogAction(ActionLogType.Teleport, loc == null ? (zone == null ? location.ToString() : zone.Name.ToUpper()) : loc.locationName);
        }
        else
        {
            Context.SendUnknownError();
            L.LogWarning("Tried to teleport to " + (zone == null ? location.ToString() : zone.Name.ToUpper()) + " and there was no terrain to teleport to at " + pos + ".");
        }
    }
}