using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UncreatedWarfare.Teams;

namespace UncreatedWarfare.FOBs
{
    public class FOBManager
    {
        List<FOB> FOBsTeam1;
        List<FOB> FOBsTeam2;

        private TeamManager TeamManager { get => UCWarfare.I.TeamManager; }

        public FOBManager()
        {
            FOBsTeam1 = new List<FOB>();
            FOBsTeam2 = new List<FOB>();
            Level.onLevelLoaded += OnLevelLoaded;
        }

        public void LoadFobs()
        {
            GetRegionBarricadeLists(
                out List<BarricadeData> FOBStructures_Team1,
                out List<BarricadeData> FOBStructures_Team2
                );

            FOBsTeam1.Clear();
            FOBsTeam2.Clear();

            for (int i = 0; i < FOBStructures_Team1.Count; i++)
            {
                FOBsTeam1.Add(new FOB("FOB" + (i + 1).ToString(), i + 1, FOBStructures_Team1[i]));
            }
            for (int i = 0; i < FOBStructures_Team2.Count; i++)
            {
                FOBsTeam2.Add(new FOB("FOB" + (i + 1).ToString(), i + 1, FOBStructures_Team2[i]));
            }
        }

        public void GetRegionBarricadeLists(
                out List<BarricadeData> FOBStructures_Team1,
                out List<BarricadeData> FOBStructures_Team2
                )
        {
            List<BarricadeRegion> barricadeRegions = BarricadeManager.regions.Cast<BarricadeRegion>().ToList();

            List<BarricadeData> barricadeDatas = barricadeRegions.SelectMany(brd => brd.barricades).ToList();
            List<BarricadeDrop> barricadeDrops = barricadeRegions.SelectMany(brd => brd.drops).ToList();

            FOBStructures_Team1 = barricadeDatas.Where(b =>
                b.barricade.id == UCWarfare.Config.FobSettings.FOBID &&   // All barricades that are FOB structures
                TeamManager.IsTeam(b.group, ETeam.TEAM1)      // All barricades that are friendly
                ).ToList();
            FOBStructures_Team2 = barricadeDatas.Where(b =>
                b.barricade.id == UCWarfare.Config.FobSettings.FOBID &&   // All barricades that are FOB structures
                TeamManager.IsTeam(b.group, ETeam.TEAM2)     // All barricades that are friendly
                ).ToList();
        }

        private void OnLevelLoaded(int level)
        {
            LoadFobs();
        }
    }

    public class FOB
    {
        public string Name;
        public int Number;
        public BarricadeData Structure;
        public DateTime DateCreated;

        public FOB(string name, int number, BarricadeData structure)
        {
            this.Name = name;
            this.Number = number;
            this.Structure = structure;
            this.DateCreated = new DateTime(DateTime.Now.Ticks);
        }
    }
}
