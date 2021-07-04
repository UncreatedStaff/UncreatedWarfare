using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Officers;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.Squads
{
    public class RallyManager
    {
        private static List<RallyPoint> rallypoints = new List<RallyPoint>();

        public static void OnBarricadePlaced(BarricadeRegion region, BarricadeData data, ref Transform location)
        {
            if (data.barricade.id == SquadManager.config.data.Team1RallyID || data.barricade.id == SquadManager.config.data.Team2RallyID)
            {
                var player = UCPlayer.FromID(data.owner);
                if (player.Squad != null)
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
            if (barricade.id == SquadManager.config.data.Team1RallyID || barricade.id == SquadManager.config.data.Team2RallyID)
            {
                var player = UCPlayer.FromID(owner);
                if (player.Squad != null && player.Squad.Leader.Steam64 == player.Steam64)
                {
                    if (player.Squad.Members.Count > 1)
                    {
                        int nearbyEnemiesCount = 0;
                        if (player.IsTeam1())
                            nearbyEnemiesCount = PlayerManager.Team2Players.Where(p => (p.Position - player.Position).sqrMagnitude < Math.Pow(100, 2)).Count();
                        if (player.IsTeam2())
                            nearbyEnemiesCount = PlayerManager.Team1Players.Where(p => (p.Position - player.Position).sqrMagnitude < Math.Pow(100, 2)).Count();

                        if (nearbyEnemiesCount > 0)
                        {
                            player.Message("rally_e_enemies");
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
        public static void OnBarricadeDestroyed(BarricadeRegion region, BarricadeData data, BarricadeDrop drop, uint instanceID, ushort plant, ushort index)
        {
            if (data.barricade.id == SquadManager.config.data.Team1RallyID || data.barricade.id == SquadManager.config.data.Team2RallyID)
            {
                TryDeleteRallyPoint(instanceID);
            }
        }

        public static void WipeAllRallies()
        {
            rallypoints.Clear();

            var barricades = GetRallyPointBarricades();

            foreach (var barricade in barricades)
            {
                if (BarricadeManager.tryGetInfo(UCBarricadeManager.GetDropFromBarricadeData(barricade).model.transform, out byte x, out byte y, out ushort plant, out ushort index, out BarricadeRegion region))
                    BarricadeManager.destroyBarricade(region, x, y, plant, index);
            }
        }

        public static void LoadRallyPoints()
        {
            rallypoints.Clear();
            var barricades = GetRallyPointBarricades();

            foreach (var barricade in barricades)
            {
                var player = UCPlayer.FromID(barricade.owner);
                if (player != null && player.Squad != null)
                {
                    var rallypoint = new RallyPoint(barricade, UCBarricadeManager.GetDropFromBarricadeData(barricade), player.Squad);
                    rallypoints.Add(rallypoint);
                    rallypoint.UpdateUIForSquad();
                }
            }
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
        public static void RegisterNewRallyPoint(BarricadeData data, Squad squad)
        {
            if (!rallypoints.Exists(r => r.structure.instanceID == data.instanceID))
            {
                var existing = rallypoints.Find(r => r.squad.Name == squad.Name);
                if (existing != null)
                {
                    existing.ClearUIForSquad();
                    existing.IsActive = false;
                    rallypoints.RemoveAll(r => r.structure.instanceID == existing.structure.instanceID);
                    if (BarricadeManager.tryGetInfo(existing.drop.model.transform, out byte x, out byte y, out ushort plant, out ushort index, out BarricadeRegion region))
                        BarricadeManager.destroyBarricade(region, x, y, plant, index);
                }

                var rallypoint = new RallyPoint(data, UCBarricadeManager.GetDropFromBarricadeData(data), squad);
                rallypoint.drop.model.transform.gameObject.AddComponent<RallyComponent>().Initialize(rallypoint);

                rallypoints.Add(rallypoint);

                foreach (var member in rallypoint.squad.Members)
                    member.Message("rally_active");

                rallypoint.UpdateUIForSquad();

                foreach (var rally in rallypoints)
                {
                    F.Log("Rally point: " + rally.squad.Name);
                    F.Log("Rally point: " + rally.IsActive);
                    F.Log("Rally point: " + rally.structure.instanceID);
                    F.Log("Rally point: " + rally.drop.instanceID);
                }
            }
        }
        public static bool HasRally(UCPlayer player, out RallyPoint rallypoint)
        {
            rallypoint = rallypoints.Find(r => r.squad.Name == player.Squad.Name);
            return rallypoint != null;
        }

        public static List<BarricadeData> GetRallyPointBarricades()
        {
            List<BarricadeRegion> barricadeRegions = BarricadeManager.regions.Cast<BarricadeRegion>().ToList();
            List<BarricadeData> barricadeDatas = barricadeRegions.SelectMany(brd => brd.barricades).ToList();

            return barricadeDatas.Where(b =>
                (b.barricade.id == SquadManager.config.data.Team1RallyID ||
                b.barricade.id == SquadManager.config.data.Team2RallyID )   // All barricades that are RallyPoints
                ).ToList();
        }
    }

    public class RallyPoint
    {
        public BarricadeData structure; // physical barricade structure of the rallypoint
        public BarricadeDrop drop;
        public List<UCPlayer> AwaitingPlayers; // list of players currently waiting to teleport to the rally
        public Squad squad;
        public bool IsActive;
        public int timer;

        public RallyPoint(BarricadeData structure, BarricadeDrop drop, Squad squad)
        {
            this.structure = structure;
            this.drop = drop;
            this.squad = squad;
            AwaitingPlayers = new List<UCPlayer>();
            IsActive = true;
            timer = 0;
        }

        public void UpdateUIForSquad()
        {
            if (!IsActive)
                return;

            //List<Node> locations = LevelNodes.nodes.Where(n => n.type == ENodeType.LOCATION).ToList();
            //Node nearerstLocation = locations.Aggregate((n1, n2) => (n1.point - rallypoint.structure.point).sqrMagnitude <= (n2.point - rallypoint.structure.point).sqrMagnitude ? n1 : n2);

            string line = "";

            if (timer >= 0)
                line = $"<color=#5eff87>RALLY</color> {timer}:00";
            else
                line = $"<color=#5eff87>RALLY</color>";

            //line += $" ({((LocationNode)nearerstLocation).name})";

            foreach (var member in squad.Members)
                EffectManager.sendUIEffect(SquadManager.config.data.rallyUI, (short)SquadManager.config.data.rallyUI, member.Player.channel.owner.transportConnection, true,
                line);
        }
        public void ClearUIForSquad()
        {
            foreach (var member in squad.Members)
                EffectManager.askEffectClearByID(SquadManager.config.data.rallyUI, member.Player.channel.owner.transportConnection);
        }
        public void TeleportPlayer(UCPlayer player)
        {
            player.Player.teleportToLocation(new Vector3(structure.point.x, structure.point.y + 2, structure.point.z), structure.angle_y);

            player.Message("rally_success");

            OfficerManager.AddOfficerPoints(squad.Leader.Player, squad.Leader.GetTeam(), OfficerManager.config.data.SpawnOnRallyPoints).GetAwaiter().GetResult();
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

                    foreach (var player in parent.AwaitingPlayers)
                    {
                        parent.TeleportPlayer(player);
                    }
                }
                if (parent.timer <= -10)
                    parent.timer = 60;

                parent.UpdateUIForSquad();

                if (parent.timer % 5 == 0)
                {
                    ulong enemyTeam = 0;
                    if (parent.squad.Team == TeamManager.Team1ID)
                        enemyTeam = TeamManager.Team2ID;
                    else if (parent.squad.Team == TeamManager.Team2ID)
                        enemyTeam = TeamManager.Team1ID;

                    var enemies = PlayerManager.OnlinePlayers.Where(p =>
                        p.GetTeam() == enemyTeam &&
                        (p.Position - parent.structure.point).sqrMagnitude < Math.Pow(70, 2)
                        ).ToList();

                    if (enemies.Count > 0)
                    {
                        if (BarricadeManager.tryGetInfo(parent.drop.model.transform, out byte x, out byte y, out ushort plant, out ushort index, out BarricadeRegion region))
                            BarricadeManager.destroyBarricade(region, x, y, plant, index);

                        RallyManager.TryDeleteRallyPoint(parent.structure.instanceID);

                        foreach (var member in parent.squad.Members)
                            member.Message("rally_cancelled");

                        yield break;
                    }
                }

                yield return new WaitForSeconds(1);
            }
        }
    }
}
