using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Point;
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
                    RegisterNewRallyPoint(data, player.Squad);
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
                        if (player.IsTeam1())
                            nearbyEnemiesCount = PlayerManager.OnlinePlayers.Count(p => p.Player.quests.groupID.m_SteamID == 2 && (p.Position - player.Position).sqrMagnitude < sqrdst);
                        if (player.IsTeam2())
                            nearbyEnemiesCount = PlayerManager.OnlinePlayers.Count(p => p.Player.quests.groupID.m_SteamID == 1 && (p.Position - player.Position).sqrMagnitude < sqrdst);

                        if (nearbyEnemiesCount > 0)
                        {
                            player.Message("rally_e_enemies");
                            shouldAllow = false;
                        }
                        else if (!F.CanStandAtLocation(new Vector3(point.x, point.y + TELEPORT_HEIGHT_OFFSET, point.z)))
                        {
                            player.Message("rally_e_obstructed");
                            shouldAllow = false;
                        }
                    }
                    else
                    {
                        player.Message("rally_e_nosquadmember");
                        shouldAllow = false;
                    }
                }
                else
                {
                    player.Message("rally_e_notsquadleader");
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
                if (rallypoints[i].structure.instanceID == instanceID)
                {
                    rallypoints[i].IsActive = false;
                    rallypoints[i].ClearUIForSquad();

                    rallypoints.RemoveAt(i);
                    return;
                }
            }
        }
        public static void RegisterNewRallyPoint(BarricadeData data, Squad squad)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (!rallypoints.Exists(r => r.structure.instanceID == data.instanceID))
            {
                RallyPoint existing = rallypoints.Find(r => r.squad.Name == squad.Name && r.squad.Team == squad.Team);
                if (existing != null)
                {
                    existing.ClearUIForSquad();
                    existing.IsActive = false;
                    rallypoints.RemoveAll(r => r.structure.instanceID == existing.structure.instanceID);
                    BarricadeDrop drop = BarricadeManager.FindBarricadeByRootTransform(existing.drop.model.transform);
                    if (drop != null && Regions.tryGetCoordinate(drop.model.position, out byte x, out byte y))
                        BarricadeManager.destroyBarricade(drop, x, y, ushort.MaxValue);
                }

                ActionLog.Add(EActionLogType.PLACED_RALLY, "AT " + data.point.ToString(), squad.Leader);

                RallyPoint rallypoint = new RallyPoint(data, UCBarricadeManager.GetDropFromBarricadeData(data), squad);
                rallypoint.drop.model.transform.gameObject.AddComponent<RallyComponent>().Initialize(rallypoint);

                rallypoints.Add(rallypoint);

                foreach (UCPlayer member in rallypoint.squad.Members)
                    member.Message("rally_active");

                rallypoint.ShowUIForSquad();

                if (UCWarfare.Config.Debug)
                    foreach (RallyPoint rally in rallypoints)
                        L.Log($"Rally point: Squad: {rally.squad.Name}, Active: {rally.IsActive}, Structure: {rally.structure.instanceID}, Drop: {rally.drop.instanceID}." + rally.squad.Name, ConsoleColor.DarkGray);
            }
        }
        public static bool HasRally(UCPlayer player, out RallyPoint rallypoint)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (player.Squad != null)
            {
                rallypoint = rallypoints.Find(r => r.squad.Name == player.Squad.Name && r.squad.Team == player.Squad.Team);
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
            rallypoint = rallypoints.Find(r => r.squad.Name == squad.Name && r.squad.Team == squad.Team);
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
        public readonly BarricadeData structure; // physical barricade structure of the rallypoint
        public readonly BarricadeDrop drop;
        public readonly List<UCPlayer> AwaitingPlayers; // list of players currently waiting to teleport to the rally
        public readonly Squad squad;
        public bool IsActive;
        public int timer;
        public readonly string nearestLocation;
        public RallyPoint(BarricadeData structure, BarricadeDrop drop, Squad squad)
        {
            this.structure = structure;
            this.drop = drop;
            this.squad = squad;
            AwaitingPlayers = new List<UCPlayer>(6);
            IsActive = true;
            timer = SquadManager.Config.RallyTimer;
            nearestLocation = F.GetClosestLocation(structure.point);
        }

        public void UpdateUIForAwaitingPlayers()
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (!IsActive)
                return;
            TimeSpan seconds = TimeSpan.FromSeconds(timer);
            foreach (UCPlayer member in squad.Members)
            {
                if (AwaitingPlayers.Contains(member))
                {
                    string line = Translation.Translate("rally_ui", member.Steam64, timer >= 0 ? Translation.ObjectTranslate("rally_time_value", member.Steam64, seconds) : string.Empty) + " " + nearestLocation;
                    SquadManager.RallyUI.SendToPlayer(member.Connection, line);
                }
            }
        }
        public void ShowUIForPlayer(UCPlayer player)
        {
            SquadManager.RallyUI.SendToPlayer(player.Connection, Translation.Translate("rally_ui", player.Steam64, $"({nearestLocation})"));
        }
        public void ShowUIForSquad()
        {
            foreach (UCPlayer member in squad.Members)
                ShowUIForPlayer(member);
        }
        public void ClearUIForPlayer(UCPlayer player)
        {
            SquadManager.RallyUI.ClearFromPlayer(player.Connection);
        }
        public void ClearUIForSquad()
        {
            foreach (UCPlayer member in squad.Members)
                ClearUIForPlayer(member);
        }
        public void TeleportPlayer(UCPlayer player)
        {
            if (!player.IsOnline || player.Player.life.isDead || player.Player.movement.getVehicle() != null)
                return;

            ActionLog.Add(EActionLogType.TELEPORTED_TO_RALLY, "AT " + drop.model.position.ToString() + " PLACED BY " + squad.Leader.Steam64.ToString(), player);

            player.Player.teleportToLocation(new Vector3(structure.point.x, structure.point.y + RallyManager.TELEPORT_HEIGHT_OFFSET, structure.point.z), structure.angle_y);

            player.Message("rally_success");

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
                parent.timer--;
                if (parent.timer <= 0)
                {
                    // rally is now spawnable

                    foreach (UCPlayer player in parent.AwaitingPlayers)
                    {
                        parent.TeleportPlayer(player);
                    }

                    QuestManager.OnRallyActivated(parent);
                    parent.AwaitingPlayers.Clear();

                    parent.timer = SquadManager.Config.RallyTimer;
                }
                else
                {
                    parent.UpdateUIForAwaitingPlayers();

                    if (parent.timer % 5 == 0)
                    {
                        ulong enemyTeam = 0;
                        if (parent.squad.Team == TeamManager.Team1ID)
                            enemyTeam = TeamManager.Team2ID;
                        else if (parent.squad.Team == TeamManager.Team2ID)
                            enemyTeam = TeamManager.Team1ID;

                        // check for enemies nearby rally points every 5 seconds
                        List<UCPlayer> enemies = PlayerManager.OnlinePlayers.Where(p =>
                            p.GetTeam() == enemyTeam &&
                            (p.Position - parent.structure.point).sqrMagnitude < Math.Pow(SquadManager.Config.RallyDespawnDistance, 2)
                            ).ToList();

                        if (enemies.Count > 0)
                        {
                            if (parent.drop != null && Regions.tryGetCoordinate(parent.drop.model.position, out byte x, out byte y))
                                BarricadeManager.destroyBarricade(parent.drop, x, y, ushort.MaxValue);

                            RallyManager.TryDeleteRallyPoint(parent.structure.instanceID);

                            foreach (UCPlayer member in parent.squad.Members)
                                member.Message("rally_cancelled");

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
