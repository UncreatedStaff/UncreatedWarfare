using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Players.Management.Legacy;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Squads;

public static class RallyManager
{
    public const float TELEPORT_HEIGHT_OFFSET = 2f;
    public static void OnBarricadePlaced(BarricadeDrop drop, BarricadeRegion region)
    {
        BarricadeData data = drop.GetServersideData();

        if (IsRally(drop.asset))
        {
            UCPlayer? player = UCPlayer.FromID(data.owner);
            if (player?.Squad != null)
            {
                if (player.Squad.RallyPoint != null)
                {
                    player.Squad.RallyPoint.Destroy();
                }

                ActionLog.Add(ActionLogType.PlacedRally, "AT " + drop.model.position.ToString("F1"), player.Squad.Leader);

                RallyPoint rallypoint = drop.model.gameObject.AddComponent<RallyPoint>();
                rallypoint.Initialize(player.Squad);

            }
        }
    }
    public static void OnBarricadePlaceRequested(
        Barricade barricade,
        ItemBarricadeAsset asset,
        Transform? hit,
        ref Vector3 point,
        ref float angle_x,
        ref float angle_y,
        ref float angle_z,
        ref ulong owner,
        ref ulong group,
        ref bool shouldAllow
    )
    {
        if (!IsRally(barricade.asset))
            return;

        UCPlayer? player = UCPlayer.FromID(owner);
        if (player == null) return;
        if (player.Squad != null && player.Squad.Leader.Steam64 == player.Steam64)
        {
            if (player.Squad.Members.Count > 1 || player.OnDuty())
            {
                int nearbyEnemiesCount = 0;
                float sqrdst = SquadManager.Config.RallyDespawnDistance *
                               SquadManager.Config.RallyDespawnDistance;
                if (player.IsTeam1)
                    nearbyEnemiesCount = PlayerManager.OnlinePlayers.Count(p => p.Player.quests.groupID.m_SteamID == 2 && (p.Position - player.Position).sqrMagnitude < sqrdst);
                else if (player.IsTeam2)
                    nearbyEnemiesCount = PlayerManager.OnlinePlayers.Count(p => p.Player.quests.groupID.m_SteamID == 1 && (p.Position - player.Position).sqrMagnitude < sqrdst);

                if (nearbyEnemiesCount > 0)
                {
                    player.SendChat(T.RallyEnemiesNearby);
                    shouldAllow = false;
                }
                else if (!F.CanStandAtLocation(new Vector3(point.x, point.y + TELEPORT_HEIGHT_OFFSET, point.z)))
                {
                    player.SendChat(T.RallyObstructedPlace);
                    shouldAllow = false;
                }
            }
            else
            {
                player.SendChat(T.RallyNoSquadmates);
                shouldAllow = false;
            }
        }
        else
        {
            player.SendChat(T.RallyNotSquadleader);
            shouldAllow = false;
        }
    }

    public static void WipeAllRallies()
    {
        try
        {
            SquadManager.RallyUI.ClearFromAllPlayers();

            foreach (BarricadeInfo barricade in GetRallyPointBarricades().ToList())
            {
                BarricadeManager.destroyBarricade(barricade.Drop, barricade.Coord.x, barricade.Coord.y, barricade.Plant);
            }
        }
        catch (Exception ex)
        {
            L.LogError("Error wiping rally points.");
            L.LogError(ex);
        }
    }

    private static IEnumerable<BarricadeInfo> GetRallyPointBarricades()
    {
        if (BarricadeManager.regions == null)
            return Array.Empty<BarricadeInfo>();

        return BarricadeUtility.EnumerateBarricades().Where(b => IsRally(b.Drop.asset));
    }

    public static bool IsRally(ItemBarricadeAsset asset)
    {
        return Gamemode.Config.RallyPoints.ContainsAsset(asset);
    }
}

public class RallyPoint : MonoBehaviour, IManualOnDestroy
{
    public List<UCPlayer> AwaitingPlayers { get; private set; } // list of players currently waiting to teleport to the rally
    public Squad Squad { get; private set; }
    public bool IsDeploying { get; private set; }
    public bool IsActive { get; set; }
    public string NearestLocation { get; private set; }
    public int SecondsLeft { get; private set; }
    public void Initialize(Squad squad)
    {
        Squad = squad;
        Squad.RallyPoint = this;
        AwaitingPlayers = new List<UCPlayer>(6);
        IsActive = true;    
        IsDeploying = false;
        NearestLocation = F.GetClosestLocationName(transform.position);
        SecondsLeft = 0;
        Squad.Leader?.SendChat(T.RallyActiveSL);
        ShowUIForSquad();
    }

    public void UpdateUIForAwaitingPlayers(int secondsLeft)
    {
        if (!IsActive)
            return;
        TimeSpan seconds = TimeSpan.FromSeconds(secondsLeft);

        for (int i = AwaitingPlayers.Count - 1; i >= 0; i--)
        {
            UCPlayer? player = AwaitingPlayers[i];
            if (!player.IsOnline || player.Squad != Squad)
            {
                AwaitingPlayers.RemoveAt(i);
                continue;
            }

            SquadManager.RallyUI.SendToPlayer(player.Connection, T.RallyUITimer.Translate(player, false, secondsLeft >= 0 ? seconds : TimeSpan.Zero, NearestLocation));
        }
    }
    public void ShowUIForPlayer(UCPlayer player)
    {
        if (!player.IsOnline)
            return;

        SquadManager.RallyUI.SendToPlayer(player.Connection, T.RallyUI.Translate(player, false, NearestLocation));
    }
    public void ShowUIForSquad()
    {
        foreach (UCPlayer member in Squad.Members)
            ShowUIForPlayer(member);
    }
    public void ClearUIForPlayer(UCPlayer player)
    {
        if (!player.IsOnline)
            return;

        SquadManager.RallyUI.ClearFromPlayer(player.Connection);
    }
    public void ClearUIForSquad ()
    {
        foreach (UCPlayer member in Squad.Members)
            ClearUIForPlayer(member);
    }
    public void TeleportPlayer(UCPlayer player)
    {
        if (player.IsOnline && !player.Player.life.isDead && player.Player.movement.getVehicle() == null)
        {
            player.Player.teleportToLocation(new Vector3(transform.position.x, transform.position.y + RallyManager.TELEPORT_HEIGHT_OFFSET, transform.position.z), transform.rotation.eulerAngles.y);
            ActionLog.Add(ActionLogType.TeleportedToRally, "AT " + transform.position.ToString() + " PLACED BY " + Squad.Leader.Steam64.ToString(), player);
            player.SendChat(T.RallySuccess);
        }

        ShowUIForPlayer(player);
    }
    public void Destroy()
    {
        var drop = BarricadeManager.FindBarricadeByRootTransform(transform.root);
        if (drop != null && Regions.tryGetCoordinate(drop.model.position, out byte x, out byte y))  
            BarricadeManager.destroyBarricade(drop, x, y, ushort.MaxValue);
    }
    public void Deactivate()
    {
        Squad.RallyPoint?.ClearUIForSquad();
        Squad.RallyPoint = null;
        AwaitingPlayers.Clear();
    }
    public void StartDeployment()
    {
        StartCoroutine(RallyRoutine());
    }
    private IEnumerator<WaitForSecondsRealtime> RallyRoutine()
    {
        SecondsLeft = SquadManager.Config.RallyTimer;
        IsDeploying = true;

        foreach (var player in Squad.Members)
        {
            AwaitingPlayers.Add(player);
            if (player.IsSquadLeader())
                player.SendChat(T.RallyWaitSL, SecondsLeft);
            else
                player.SendChat(T.RallyWait, SecondsLeft);
            Tips.TryGiveTip(player, 5, T.RallyToast, SecondsLeft);
        }

        while (SecondsLeft > 0)
        {

            UpdateUIForAwaitingPlayers(SecondsLeft);

            yield return new WaitForSecondsRealtime(1);
            SecondsLeft--;
        }

        foreach (UCPlayer player in AwaitingPlayers)
        {
            if (player.IsOnline && player.Squad == Squad)
                TeleportPlayer(player);
        }

        AwaitingPlayers.Clear();
        IsDeploying = false;
        QuestManager.OnRallyActivated(this);
    }

    private float _timeLastTick;
    private void FixedUpdate()
    {
        if (Time.realtimeSinceStartup - _timeLastTick > 5)
        {
            _timeLastTick = Time.realtimeSinceStartup;
            ulong enemyTeam = TeamManager.Other(Squad.Team);
            if (enemyTeam == 0)
                return;

            List<UCPlayer> enemies = PlayerManager.OnlinePlayers.Where(p =>
                p.GetTeam() == enemyTeam &&
                (p.Position - transform.position).sqrMagnitude < Math.Pow(SquadManager.Config.RallyDespawnDistance, 2)
            ).ToList();

            if (enemies.Count > 0)
            {
                foreach (UCPlayer member in Squad.Members)
                {
                    if (member is { IsOnline: true }) // was throwing an error for some reason
                        member.SendChat(T.RallyEnemiesNearbyTp);
                }

                Destroy();
            }
        }
    }

    void IManualOnDestroy.ManualOnDestroy()
    {
        if (this != null)
        {
            Deactivate();
            Destroy(this);
        }
    }
}