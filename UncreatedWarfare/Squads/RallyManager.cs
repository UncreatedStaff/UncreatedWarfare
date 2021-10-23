﻿using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Officers;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.Squads
{
    public class RallyManager
    {
        private static readonly List<RallyPoint> rallypoints = new List<RallyPoint>();
        public const float TELEPORT_HEIGHT_OFFSET = 2f;
        public static void OnBarricadePlaced(BarricadeDrop drop, BarricadeRegion region)
        {
            SDG.Unturned.BarricadeData data = drop.GetServersideData();

            if (data.barricade.id == SquadManager.config.Data.Team1RallyID || data.barricade.id == SquadManager.config.Data.Team2RallyID)
            {
                UCPlayer player = UCPlayer.FromID(data.owner);
                if (player?.Squad != null)
                {
                    RegisterNewRallyPoint(data, player.Squad);
                }
            }
        }

        public static void OnBarricadePlaceRequested(
            Barricade barricade,
            ItemBarricadeAsset asset,
            Transform hit,
            ref Vector3 point,
            ref float angle_x,
            ref float angle_y,
            ref float angle_z,
            ref ulong owner,
            ref ulong group,
            ref bool shouldAllow
            )
        {
            if (barricade.id == SquadManager.config.Data.Team1RallyID || barricade.id == SquadManager.config.Data.Team2RallyID)
            {
                UCPlayer player = UCPlayer.FromID(owner);
                if (player.Squad != null && player.Squad.Leader.Steam64 == player.Steam64)
                {
                    if (player.Squad.Members.Count > 1)
                    {
                        int nearbyEnemiesCount = 0;
                        if (player.IsTeam1())
                            nearbyEnemiesCount = PlayerManager.Team2Players.Count(p => (p.Position - player.Position).sqrMagnitude < Math.Pow(SquadManager.config.Data.RallyDespawnDistance, 2));
                        if (player.IsTeam2())
                            nearbyEnemiesCount = PlayerManager.Team1Players.Count(p => (p.Position - player.Position).sqrMagnitude < Math.Pow(SquadManager.config.Data.RallyDespawnDistance, 2));

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
        public static void OnBarricadeDestroyed(SDG.Unturned.BarricadeData data, BarricadeDrop drop, uint instanceID, ushort plant)
        {
            if (data.barricade.id == SquadManager.config.Data.Team1RallyID || data.barricade.id == SquadManager.config.Data.Team2RallyID)
            {
                TryDeleteRallyPoint(instanceID);
            }
        }

        public static void WipeAllRallies()
        {
            rallypoints.Clear();
            foreach (SteamPlayer player in Provider.clients)
            {
                EffectManager.askEffectClearByID(SquadManager.config.Data.rallyUI, player.transportConnection);
            }

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
        public static void RegisterNewRallyPoint(SDG.Unturned.BarricadeData data, Squad squad)
        {
            if (!rallypoints.Exists(r => r.structure.instanceID == data.instanceID))
            {
                RallyPoint existing = rallypoints.Find(r => r.squad.Name == squad.Name);
                if (existing != null)
                {
                    existing.ClearUIForSquad();
                    existing.IsActive = false;
                    rallypoints.RemoveAll(r => r.structure.instanceID == existing.structure.instanceID);
                    BarricadeDrop drop = BarricadeManager.FindBarricadeByRootTransform(existing.drop.model.transform);
                    if (drop != null && Regions.tryGetCoordinate(drop.model.position, out byte x, out byte y))
                        BarricadeManager.destroyBarricade(drop, x, y, ushort.MaxValue);
                }

                RallyPoint rallypoint = new RallyPoint(data, UCBarricadeManager.GetDropFromBarricadeData(data), squad);
                rallypoint.drop.model.transform.gameObject.AddComponent<RallyComponent>().Initialize(rallypoint);

                rallypoints.Add(rallypoint);

                foreach (UCPlayer member in rallypoint.squad.Members)
                    member.Message("rally_active");

                rallypoint.ShowUIForSquad();

                if (UCWarfare.Config.Debug)
                    foreach (RallyPoint rally in rallypoints)
                        F.Log($"Rally point: Squad: {rally.squad.Name}, Active: {rally.IsActive}, Structure: {rally.structure.instanceID}, Drop: {rally.drop.instanceID}." + rally.squad.Name, ConsoleColor.DarkGray);
            }
        }
        public static bool HasRally(UCPlayer player, out RallyPoint rallypoint)
        {
            rallypoint = rallypoints.Find(r => r.squad.Name == player.Squad.Name);
            return rallypoint != null;
        }
        public static bool HasRally(Squad squad, out RallyPoint rallypoint)
        {
            rallypoint = rallypoints.Find(r => r.squad == squad);
            return rallypoint != null;
        }

        public static IEnumerable<BarricadeDrop> GetRallyPointBarricades()
        {
            IEnumerable<BarricadeDrop> barricadeDrops = BarricadeManager.regions.Cast<BarricadeRegion>().SelectMany(brd => brd.drops);

            return barricadeDrops.Where(b =>
                b.GetServersideData().barricade.id == SquadManager.config.Data.Team1RallyID ||
                b.GetServersideData().barricade.id == SquadManager.config.Data.Team2RallyID);
        }
    }

    public class RallyPoint
    {
        public SDG.Unturned.BarricadeData structure; // physical barricade structure of the rallypoint
        public BarricadeDrop drop;
        public List<UCPlayer> AwaitingPlayers; // list of players currently waiting to teleport to the rally
        public Squad squad;
        public bool IsActive;
        public int timer;
        public string nearestLocation;
        public RallyPoint(SDG.Unturned.BarricadeData structure, BarricadeDrop drop, Squad squad)
        {
            this.structure = structure;
            this.drop = drop;
            this.squad = squad;
            AwaitingPlayers = new List<UCPlayer>();
            IsActive = true;
            timer = SquadManager.config.Data.RallyTimer;

            List<Node> locations = LevelNodes.nodes.Where(n => n.type == ENodeType.LOCATION).ToList();
            Node nearerstLocation = locations.Aggregate((n1, n2) => (n1.point - structure.point).sqrMagnitude <= (n2.point - structure.point).sqrMagnitude ? n1 : n2);
            nearestLocation = $"{((LocationNode)nearerstLocation).name}";
        }

        public void UpdateUIForAwaitingPlayers()
        {
            if (!IsActive)
                return;
            TimeSpan seconds = TimeSpan.FromSeconds(timer);
            foreach (UCPlayer member in squad.Members)
            {
                if (AwaitingPlayers.Contains(member))
                {
                    string line = F.Translate("rally_ui", member.Steam64, timer >= 0 ? F.ObjectTranslate("rally_time_value", member.Steam64, seconds) : string.Empty) + " " + nearestLocation;
                    EffectManager.sendUIEffect(SquadManager.config.Data.rallyUI, (short)SquadManager.config.Data.rallyUI, member.Player.channel.owner.transportConnection, true,
                    line);
                }
            }
        }
        public void ShowUIForPlayer(UCPlayer player)
        {
            EffectManager.sendUIEffect(SquadManager.config.Data.rallyUI, (short)SquadManager.config.Data.rallyUI, player.Player.channel.owner.transportConnection, true,
                        F.Translate("rally_ui", player.Steam64, $"({nearestLocation})"
                        ));
        }
        public void ShowUIForSquad()
        {
            foreach (UCPlayer member in squad.Members)
                ShowUIForPlayer(member);
        }
        public void ClearUIForPlayer(UCPlayer player)
        {
            EffectManager.askEffectClearByID(SquadManager.config.Data.rallyUI, player.Player.channel.owner.transportConnection);
        }
        public void ClearUIForSquad()
        {
            foreach (UCPlayer member in squad.Members)
                ClearUIForPlayer(member);
        }
        public void TeleportPlayer(UCPlayer player)
        {
            if (player.Player.life.isDead || player.Player.movement.getVehicle() != null)
                return;

            player.Player.teleportToLocation(new Vector3(structure.point.x, structure.point.y + RallyManager.TELEPORT_HEIGHT_OFFSET, structure.point.z), structure.angle_y);

            player.Message("rally_success");

            ShowUIForPlayer(player);

            OfficerManager.AddOfficerPoints(squad.Leader.Player, OfficerManager.config.Data.SpawnOnRallyPoints, F.Translate("ofp_rally_used", squad.Leader.Steam64));
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
                parent.timer--;
                if (parent.timer <= 0)
                {
                    // rally is now spawnable

                    foreach (UCPlayer player in parent.AwaitingPlayers)
                    {
                        parent.TeleportPlayer(player);
                    }
                    parent.AwaitingPlayers.Clear();

                    parent.timer = SquadManager.config.Data.RallyTimer;
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
                            (p.Position - parent.structure.point).sqrMagnitude < Math.Pow(SquadManager.config.Data.RallyDespawnDistance, 2)
                            ).ToList();

                        if (enemies.Count > 0)
                        {
                            if (parent.drop != null && Regions.tryGetCoordinate(parent.drop.model.position, out byte x, out byte y))
                                BarricadeManager.destroyBarricade(parent.drop, x, y, ushort.MaxValue);

                            RallyManager.TryDeleteRallyPoint(parent.structure.instanceID);

                            foreach (UCPlayer member in parent.squad.Members)
                                member.Message("rally_cancelled");

                            yield break;
                        }
                    }
                }

                yield return new WaitForSeconds(1);
            }
        }
    }
}
