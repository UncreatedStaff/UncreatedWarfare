using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uncreated.Warfare.Point
{
    public class RankData
    {
        public readonly ulong Steam64;
        public readonly int TotalXP;
        public readonly EBranch Branch;
        public readonly int Level;
        public readonly int OfficerLevel;
        public readonly EBranch OfficerBranch;
        public readonly int CurrentXP;
        public readonly int RequiredXP;
        public readonly string Name;
        public readonly string Abbreviation;
        public readonly RankData NextRank;
        private Dictionary<int, int> RankValues;
        public RankData(ulong steamID, int xp, EBranch branch)
        {
            Steam64 = steamID;

            TotalXP = xp;
            Branch = branch;

            if (OfficerStorage.IsOfficer(Steam64, out var officer))
            {
                OfficerLevel = officer.OfficerLevel;
                OfficerBranch = officer.Branch;
            }

            if (Branch == EBranch.INFANTRY) RankValues = Points.XPConfig.InfantryRankValues;
            else if (Branch == EBranch.ARMOR) RankValues = Points.XPConfig.ArmorRankValues;
            else if (Branch == EBranch.AIRFORCE) RankValues = Points.XPConfig.AirforceRankValues;
            else RankValues = null;

            // set level & RequiredXP
            Level = RankValues.Last().Key; ;
            foreach (var level in RankValues)
            {
                if (TotalXP < level.Value)
                {
                    Level = level.Key;
                    RequiredXP = level.Value;
                    break;
                }
            }

            // set CurrentXP
            int totalRequiredXP = 0;
            for (int i = 0; i < RankValues.Count; i++)
            {
                totalRequiredXP += RankValues[i];
                if (TotalXP < totalRequiredXP)
                {
                    CurrentXP = unchecked(RankValues[i] - (totalRequiredXP - xp));
                    break;
                }
            }

            // set NextRank
            if (Level >= RankValues.Count - 1 || OfficerLevel == 5)
                NextRank = null;
            else
            {
                int nextLevel = Level + 1;
                int nextXP = RankValues[nextLevel];
                NextRank = new RankData(Steam64, nextXP, Branch, nextLevel, OfficerLevel, OfficerBranch, CurrentXP, RequiredXP, RankValues);
            }

            // set Name & Abbreivation

            if (OfficerLevel > 0)
            {
                Name = GetOfficerRankName(OfficerLevel);
                Abbreviation = GetOfficerRankAbbreviation(OfficerLevel);
            }
            else
            {
                Name = GetRankName(Level);
                Abbreviation = GetRankAbbreviation(Level);
            }
            
        }
        private RankData(ulong steamID, int xp, EBranch branch, int level, int officerLevel, EBranch officerBranch, int currentXP, int requiredXP, Dictionary<int, int> rankValues)
        {
            Steam64 = steamID;
            TotalXP = xp;
            Branch = branch;
            Level = level;
            OfficerLevel = officerLevel;
            OfficerBranch = officerBranch;
            RankValues = rankValues;
            CurrentXP = currentXP;
            RequiredXP = requiredXP;
        }

        public static string GetRankName(int level)
        {
            switch (level)
            {
                case 0: return "Recruit";
                case 1: return "Private";
                case 2: return "Private 1st Class";
                case 3: return "Corporal";
                case 4: return "Specialist";
                case 5: return "Sergeant";
                case 6: return "Staff Sergeant";
                case 7: return "Sergeant 1st Class";
                case 8: return "Sergeant Major";
                case 9: return "Warrant Officer";
                case 10: return "Chief Warrant Officer";
                default: return "unknown";
            }
        }
        public static string GetRankAbbreviation(int level)
        {
            switch (level)
            {
                case 0: return "Rec.";
                case 1: return "Pvt.";
                case 2: return "Pfc.";
                case 3: return "Col.";
                case 4: return "Spec.";
                case 5: return "Sgt.";
                case 6: return "Ssg.";
                case 7: return "Sfc.";
                case 8: return "S.M.";
                case 9: return "W.O.";
                case 10: return "C.W.O.";
                default: return "###";
            }
        }

        public static string GetOfficerRankName(int officerLevel)
        {
            switch (officerLevel)
            {
                case 1: return "Captain";
                case 2: return "Major";
                case 3: return "Lieutenant";
                case 4: return "Colonel";
                case 5: return "General";
                default: return "unknown";
            }
        }
        public static string GetOfficerRankAbbreviation(int officerLevel)
        {
            switch (officerLevel)
            {
                case 1: return "Cpt.";
                case 2: return "Maj.";
                case 3: return "Lt.";
                case 4: return "Col.";
                case 5: return "Gen.";
                default: return "###";
            }
        }
    }
}
