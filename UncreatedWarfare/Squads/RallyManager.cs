using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.Squads
{
    public static class RallyManager
    {
        // TODO: Remove old rally and ammo bag when new one is placed.
        private static readonly List<RallyPoint> rallypoints = new List<RallyPoint>();
        public const float TELEPORT_HEIGHT_OFFSET = 2f;
        public static void OnBarricadePlaced(BarricadeDrop drop, BarricadeRegion region)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            SDG.Unturned.BarricadeData data = drop.GetServersideData();

            if (TeamManager.Team1Faction.RallyPoint.MatchGuid(data.barricade.asset.GUID) || TeamManager.Team2Faction.RallyPoint.MatchGuid(data.barricade.asset.GUID))
            {
                UCPlayer? player = UCPlayer.FromID(data.owner);
                if (player?.Squad != null)
                {
                    RegisterNewRallyPoint(drop, player.Squad);
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
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (TeamManager.Team1Faction.RallyPoint.MatchGuid(barricade.asset.GUID) || TeamManager.Team2Faction.RallyPoint.MatchGuid(barricade.asset.GUID))
            {
                UCPlayer? player = UCPlayer.FromID(owner);
                if (player == null) return;
                if (player.Squad != null && player.Squad.Leader.Steam64 == player.Steam64)
                {
                    if (player.Squad.Members.Count > 1)
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
        }
        public static void OnBarricadeDestroyed(BarricadeData data, BarricadeDrop drop, uint instanceID, ushort plant)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (TeamManager.Team1Faction.RallyPoint.MatchGuid(data.barricade.asset.GUID) || TeamManager.Team2Faction.RallyPoint.MatchGuid(data.barricade.asset.GUID))
            {
                TryDeleteRallyPoint(instanceID);
            }
        }

        public static void WipeAllRallies()
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            rallypoints.Clear();
            SquadManager.RallyUI.ClearFromAllPlayers();

            IEnumerator<BarricadeDrop> barricades = GetRallyPointBarricades().GetEnumerator();
            while (barricades.MoveNext())
            {
                if (Regions.tryGetCoordinate(barricades.Current.model.position, out byte x, out byte y))
                    BarricadeManager.destroyBarricade(barricades.Current, x, y, ushort.MaxValue);
            }
            barricades.Dispose();
        }
        public static void TryDeleteRallyPoint(uint instanceID)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            for (int i = 0; i < rallypoints.Count; i++)
            {
                if (rallypoints[i].Drop.instanceID == instanceID)
                {
                    rallypoints[i].IsActive = false;
                    rallypoints[i].ClearUIForSquad();

                    rallypoints.RemoveAt(i);
                    return;
                }
            }
        }
        public static void RegisterNewRallyPoint(BarricadeDrop drop, Squad squad)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (!rallypoints.Exists(r => r.Drop.instanceID == drop.instanceID))
            {
                RallyPoint existing = rallypoints.Find(r => r.Squad.Name == squad.Name && r.Squad.Team == squad.Team);
                if (existing != null)
                {
                    existing.ClearUIForSquad();
                    existing.IsActive = false;
                    rallypoints.RemoveAll(r => r.Drop.instanceID == existing.Drop.instanceID);
                    BarricadeDrop drop2 = BarricadeManager.FindBarricadeByRootTransform(existing.Drop.model.transform);
                    if (drop2 != null && Regions.tryGetCoordinate(drop2.model.position, out byte x, out byte y))
                        BarricadeManager.destroyBarricade(drop2, x, y, ushort.MaxValue);
                }

                ActionLogger.Add(ActionLogType.PLACED_RALLY, "AT " + drop.model.position.ToString("F1"), squad.Leader);

                RallyPoint rallypoint = new RallyPoint(drop, squad);
                rallypoint.Drop.model.transform.gameObject.AddComponent<RallyComponent>().Initialize(rallypoint);

                rallypoints.Add(rallypoint);

                foreach (UCPlayer member in rallypoint.Squad.Members)
                    member.SendChat(T.RallyActive);

                rallypoint.ShowUIForSquad();

                if (UCWarfare.Config.Debug)
                    foreach (RallyPoint rally in rallypoints)
                        L.Log($"Rally point: Squad: {rally.Squad.Name}, Active: {rally.IsActive}, Structure: {rally.Drop.instanceID}, Drop: {rally.Drop.instanceID}." + rally.Squad.Name, ConsoleColor.DarkGray);
            }
        }
        public static bool HasRally(UCPlayer player, out RallyPoint rallypoint)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (player.Squad != null)
            {
                rallypoint = rallypoints.Find(r => r.Squad.Name == player.Squad.Name && r.Squad.Team == player.Squad.Team);
                return rallypoint != null;
            }
            rallypoint = null!;
            return false;
        }
        public static bool HasRally(Squad squad, out RallyPoint rallypoint)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            rallypoint = rallypoints.Find(r => r.Squad.Name == squad.Name && r.Squad.Team == squad.Team);
            return rallypoint != null;
        }
        public static IEnumerable<BarricadeDrop> GetRallyPointBarricades()
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (BarricadeManager.regions is null) return new BarricadeDrop[0];
            IEnumerable<BarricadeDrop> barricadeDrops = BarricadeManager.regions.Cast<BarricadeRegion>().SelectMany(brd => brd.drops);

            return barricadeDrops.Where(b =>
                TeamManager.Team1Faction.RallyPoint.MatchGuid(b.asset.GUID) ||
                TeamManager.Team2Faction.RallyPoint.MatchGuid(b.asset.GUID));
        }
    }

    public class RallyPoint
    {
        public readonly BarricadeDrop Drop;
        public readonly List<UCPlayer> AwaitingPlayers; // list of players currently waiting to teleport to the rally
        public readonly Squad Squad;
        public bool IsActive;
        public int Timer;
        public readonly string NearestLocation;
        public RallyPoint(BarricadeDrop drop, Squad squad)
        {
            this.Drop = drop;
            this.Squad = squad;
            AwaitingPlayers = new List<UCPlayer>(6);
            IsActive = true;
            Timer = SquadManager.Config.RallyTimer;
            NearestLocation = F.GetClosestLocation(drop.model.position);
        }

        public void UpdateUIForAwaitingPlayers()
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (!IsActive)
                return;
            TimeSpan seconds = TimeSpan.FromSeconds(Timer);
            for (int i = 0; i < Squad.Members.Count; i++)
            {
                UCPlayer member = Squad.Members[i];
                if (AwaitingPlayers.Contains(member))
                    SquadManager.RallyUI.SendToPlayer(member.Connection, T.RallyUITimer.Translate(member, Timer >= 0 ? seconds : TimeSpan.Zero, NearestLocation));
            }
        }
        public void ShowUIForPlayer(UCPlayer player)
        {
            SquadManager.RallyUI.SendToPlayer(player.Connection, T.RallyUI.Translate(player, NearestLocation));
        }
        public void ShowUIForSquad()
        {
            foreach (UCPlayer member in Squad.Members)
                ShowUIForPlayer(member);
        }
        public void ClearUIForPlayer(UCPlayer player)
        {
            SquadManager.RallyUI.ClearFromPlayer(player.Connection);
        }
        public void ClearUIForSquad()
        {
            foreach (UCPlayer member in Squad.Members)
                ClearUIForPlayer(member);
        }
        public void TeleportPlayer(UCPlayer player)
        {
            if (!player.IsOnline || player.Player.life.isDead || player.Player.movement.getVehicle() != null)
                return;

            ActionLogger.Add(ActionLogType.TELEPORTED_TO_RALLY, "AT " + Drop.model.position.ToString() + " PLACED BY " + Squad.Leader.Steam64.ToString(), player);

            player.Player.teleportToLocation(new Vector3(Drop.model.position.x, Drop.model.position.y + RallyManager.TELEPORT_HEIGHT_OFFSET, Drop.model.position.z), Drop.model.rotation.eulerAngles.y);

            player.SendChat(T.RallySuccess);

            ShowUIForPlayer(player);
        }
    }

    public class RallyComponent : MonoBehaviour
    {
        public RallyPoint parent;

        public void Initialize(RallyPoint rallypoint)
        {
            parent = rallypoint;
            StartCoroutine(RallyPointLoop());
        }
        private IEnumerator<WaitForSeconds> RallyPointLoop()
        {
            while (parent.IsActive)
            {
#if DEBUG
                IDisposable profiler = ProfilingUtils.StartTracking();
#endif
                parent.Timer--;
                if (parent.Timer <= 0)
                {
                    // rally is now spawnable

                    foreach (UCPlayer player in parent.AwaitingPlayers)
                    {
                        parent.TeleportPlayer(player);
                    }

                    QuestManager.OnRallyActivated(parent);
                    parent.AwaitingPlayers.Clear();

                    parent.Timer = SquadManager.Config.RallyTimer;
                }
                else
                {
                    parent.UpdateUIForAwaitingPlayers();

                    if (parent.Timer % 5 == 0)
                    {
                        ulong enemyTeam = 0;
                        if (parent.Squad.Team == TeamManager.Team1ID)
                            enemyTeam = TeamManager.Team2ID;
                        else if (parent.Squad.Team == TeamManager.Team2ID)
                            enemyTeam = TeamManager.Team1ID;

                        // check for enemies nearby rally points every 5 seconds
                        List<UCPlayer> enemies = PlayerManager.OnlinePlayers.Where(p =>
                            p.GetTeam() == enemyTeam &&
                            (p.Position - parent.Drop.model.position).sqrMagnitude < Math.Pow(SquadManager.Config.RallyDespawnDistance, 2)
                            ).ToList();

                        if (enemies.Count > 0)
                        {
                            if (parent.Drop != null && Regions.tryGetCoordinate(parent.Drop.model.position, out byte x, out byte y))
                                BarricadeManager.destroyBarricade(parent.Drop, x, y, ushort.MaxValue);

                            RallyManager.TryDeleteRallyPoint(parent.Drop!.instanceID);

                            foreach (UCPlayer member in parent.Squad.Members)
                                member.SendChat(T.RallyEnemiesNearbyTp);

#if DEBUG
                            profiler.Dispose();
#endif
                            yield break;
                        }
                    }
                }
#if DEBUG
                profiler.Dispose();
#endif
                yield return new WaitForSeconds(1);
            }
        }
    }
}
