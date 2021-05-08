using Newtonsoft.Json;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UncreatedWarfare.Teams;

namespace UncreatedWarfare.FOBs
{
    public class FOBManager
    {
        public FOBConfig config;

        List<FOB> Team1FOBs;
        List<FOB> Team2FOBs;

        TeamManager teams => UCWarfare.I.TeamManager;

        public FOBManager()
        {
            Team1FOBs = new List<FOB>();
            Team2FOBs = new List<FOB>();
            Level.onLevelLoaded += OnLevelLoaded;

            config = new FOBConfig(UCWarfare.FOBStorage + "config.json");
            config.LoadDefaults();
        }

        private void OnLevelLoaded(int level)
        {
            LoadFobs();
        }

        public void LoadFobs()
        {
            GetRegionBarricadeLists(
                out List<BarricadeData> Team1FOBs,
                out List<BarricadeData> Team2FOBs
                );

            this.Team1FOBs.Clear();
            this.Team2FOBs.Clear();

            for (int i = 0; i < Team1FOBs.Count; i++)
            {
                this.Team1FOBs.Add(new FOB("FOB" + (i + 1).ToString(), i + 1, Team1FOBs[i]));
            }
            for (int i = 0; i < Team2FOBs.Count; i++)
            {
                this.Team2FOBs.Add(new FOB("FOB" + (i + 1).ToString(), i + 1, Team2FOBs[i]));
            }
        }

        public void RegisterNewFOB(BarricadeData Structure)
        {
            if (teams.IsTeam(Structure.group, ETeam.TEAM1))
            {
                for (int i = 0; i < Team1FOBs.Count; i++)
                {
                    if (Team1FOBs[i].Number != i + 1)
                    {
                        Team1FOBs.Insert(i, new FOB("FOB" + (i + 1).ToString(), i + 1, Structure));
                        return;
                    }
                }

                Team1FOBs.Add(new FOB("FOB" + (Team1FOBs.Count + 1).ToString(), Team1FOBs.Count + 1, Structure));
            }
            else if (teams.IsTeam(Structure.group, ETeam.TEAM2))
            {
                for (int i = 0; i < Team2FOBs.Count; i++)
                {
                    if (Team2FOBs[i].Number != i + 1)
                    {
                        Team2FOBs.Insert(i, new FOB("FOB" + (i + 1).ToString(), i + 1, Structure));
                        return;
                    }
                }

                Team2FOBs.Add(new FOB("FOB" + (Team2FOBs.Count + 1).ToString(), Team2FOBs.Count + 1, Structure));
            }
        }

        public void TryDeleteFOB(uint instanceID)
        {
            Team1FOBs.RemoveAll(f => f.Structure.instanceID == instanceID);
            Team2FOBs.RemoveAll(f => f.Structure.instanceID == instanceID);
        }

        public List<FOB> GetAvailableFobs(UnturnedPlayer player)
        {
            if (teams.IsTeam(player, ETeam.TEAM1))
            {
                return Team1FOBs;
            }
            else if (teams.IsTeam(player, ETeam.TEAM2))
            {
                return Team2FOBs;
            }

            return new List<FOB>();
        }

        public void GetRegionBarricadeLists(
                out List<BarricadeData> Team1Barricades,
                out List<BarricadeData> Team2Barricades
                )
        {
            List<BarricadeRegion> barricadeRegions = BarricadeManager.regions.Cast<BarricadeRegion>().ToList();

            List<BarricadeData> barricadeDatas = barricadeRegions.SelectMany(brd => brd.barricades).ToList();
            List<BarricadeDrop> barricadeDrops = barricadeRegions.SelectMany(brd => brd.drops).ToList();

            Team1Barricades = barricadeDatas.Where(b =>
                b.barricade.id == config.FOBID &&   // All barricades that are FOB Structures
                teams.IsTeam(b.group, ETeam.TEAM1)      // All barricades that are friendly
                ).ToList();
            Team2Barricades = barricadeDatas.Where(b =>
                b.barricade.id == config.FOBID &&   // All barricades that are FOB Structures
                teams.IsTeam(b.group, ETeam.TEAM2)     // All barricades that are friendly
                ).ToList();
        }

        public void RemoveAllFOBBarricadesFromWorld()
        {
            List<BarricadeRegion> barricadeRegions = BarricadeManager.regions.Cast<BarricadeRegion>().ToList();

            List<BarricadeData> barricadeDatas = barricadeRegions.SelectMany(brd => brd.barricades).ToList();
            List<BarricadeDrop> barricadeDrops = barricadeRegions.SelectMany(brd => brd.drops).ToList();

            List<BarricadeData> FOBComponents = barricadeDatas.Where(b =>
            (teams.IsTeam(b.group, ETeam.TEAM1) || teams.IsTeam(b.group, ETeam.TEAM2)) &&
            (b.barricade.id == config.FOBID ||
            b.barricade.id == config.FOBBaseID ||
            b.barricade.id == config.AmmoCrateID ||
            b.barricade.id == config.AmmoCrateBaseID ||
            b.barricade.id == config.RepairStationID ||
            b.barricade.id == config.RepairStationBaseID) ||
            config.Emplacements.Exists(e => e.baseID == b.barricade.id) ||
            config.Fortifications.Exists(f => f.base_id == b.barricade.id) ||
            config.Fortifications.Exists(f => f.barricade_id == b.barricade.id)
            ).ToList();

            foreach (BarricadeData data in FOBComponents)
            {
                BarricadeDrop drop = barricadeDrops.Find(d => d.instanceID == data.instanceID);
                if (drop != null)
                {
                    if (BarricadeManager.tryGetInfo(drop.model, out byte _x, out byte _y, out ushort _plant, out ushort _index, out BarricadeRegion _barricadeRegion))
                    {
                        BarricadeManager.destroyBarricade(_barricadeRegion, _x, _y, _plant, _index);
                    }
                }
            }

            UpdateUIAll();
        }

        public void UpdateUI(UnturnedPlayer player)
        {
            List<Node> locations = LevelNodes.nodes.Where(n => n.type == ENodeType.LOCATION).ToList();

            List<FOB> FOBList = new List<FOB>();

            if (teams.IsTeam(player, ETeam.TEAM1))
            {
                FOBList = Team1FOBs;
            }
            if (teams.IsTeam(player, ETeam.TEAM2))
            {
                FOBList = Team2FOBs;
            }

            int UINumber = 0;

            for (int i = 0; i < 10; i++)
            {
                EffectManager.askEffectClearByID((ushort)(32371 + i), Provider.findTransportConnection(player.CSteamID));
            }

            for (int i = 0; i < FOBList.Count; i++)
            {
                if (i >= 10)
                    break;

                string line = "";

                if (FOBList[i] == null || FOBList[i].Structure.barricade.isDead)
                    continue;

                Node nearerstLocation = locations.Aggregate((n1, n2) => (n1.point - FOBList[i].Structure.point).sqrMagnitude <= (n2.point - FOBList[i].Structure.point).sqrMagnitude ? n1 : n2);

                if (FOBList[i].Structure.barricade.health < 600)
                    line = "<color=#ffc354>" + FOBList[i].Name + "</color>";
                else
                    line = "<color=#54e3ff>" + FOBList[i].Name + "</color>";

                line += $" ({((LocationNode)nearerstLocation).name})";

                EffectManager.sendUIEffect((ushort)(32371 + UINumber), (short)(32371 + UINumber), Provider.findTransportConnection(player.CSteamID), true, line);
                UINumber++;
            }
        }
        public void UpdateUIAll()
        {
            foreach (var steamPlayer in Provider.clients)
            {
                UpdateUI(UnturnedPlayer.FromSteamPlayer(steamPlayer));
            }
        }

        public void SaveConfig()
        {
            StreamWriter file = File.CreateText(config.directory);
            JsonWriter writer = new JsonTextWriter(file);

            JsonSerializer serializer = new JsonSerializer();

            serializer.Formatting = Formatting.Indented;

            try
            {
                serializer.Serialize(writer, config);
                writer.Close();
            }
            catch (Exception ex)
            {
                writer.Close();
                throw ex;
            }
        }
        public void ReloadConfig()
        {
            StreamReader r = File.OpenText(config.directory);

            try
            {
                string json = r.ReadToEnd();
                FOBConfig newConfig = JsonConvert.DeserializeObject<FOBConfig>(json);

                r.Close();
                r.Dispose();

                config = newConfig;
            }
            catch
            {
                throw new ConfigReadException(r, config.directory);
            }
        }


    }

    public class FOB
    {
        public string Name;
        public int Number;
        public BarricadeData Structure;
        public DateTime DateCreated;
        public FOB(string Name, int number, BarricadeData Structure)
        {
            this.Name = Name;
            Number = number;
            this.Structure = Structure;
            DateCreated = new DateTime(DateTime.Now.Ticks);
        }
    }
}
