using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Uncreated.Warfare.Point
{
    public class RankData
    {
        public readonly ulong Steam64;
        public readonly int TotalXP;
        public readonly EBranch Branch;
        public readonly int Level;
        public readonly int RankTier;
        public readonly int OfficerTier;
        public readonly ulong OfficerTeam;
        public readonly int CurrentXP;
        public readonly int RequiredXP;
        public readonly string Name;
        public readonly string Abbreviation;
        public RankData(ulong steamID, int xp, EBranch branch, ulong officerTeam)
        {
            Steam64 = steamID;

            TotalXP = xp;
            Branch = branch;

            int a = 600;
            int d = 200;

            float x = d / 2f;
            float y = a - (3 * x);
            float z = 0 - x - y;

            Level = Mathf.RoundToInt(Mathf.Floor((x - a + Mathf.Sqrt(Mathf.Pow(a - x, 2f) + (2f * d * TotalXP))) / d));

            float n = Level + 1;

            RequiredXP = a + Level * d;

            CurrentXP = (int)(TotalXP - ((x * Math.Pow(n, 2)) + (y * n) + z));

            if (OfficerStorage.IsOfficer(Steam64, officerTeam, out var officer))
            {
                OfficerTier = officer.OfficerTier;
                OfficerTeam = officerTeam;
            }
            else
            {
                OfficerTier = 0;
                OfficerTeam = 0;
            }

            RankTier = GetRankTier(Level);


            if (OfficerTier > 0)
            {
                Name = GetOfficerRankName(OfficerTier);
                Abbreviation = GetOfficerRankAbbreviation(OfficerTier);
            }
            else
            {
                Name = GetRankName(RankTier);
                Abbreviation = GetRankAbbreviation(RankTier);
            }
            
        }
        public static int GetRankTier(int level)
        {
            if (level < 2) return 0;
            else if (level < 5) return 1;
            else if (level < 8) return 2;
            else if (level < 12) return 3;
            else if (level < 16) return 4;
            else if (level < 22) return 5;
            else if (level < 28) return 6;
            else if (level < 32) return 7;
            else if (level < 36) return 8;
            else if (level < 40) return 9;
            else return 10;
        }
        public static string GetRankName(int rankTier)
        {
            switch (rankTier)
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
        public static string GetRankAbbreviation(int rankTier)
        {
            switch (rankTier)
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

        public static string GetOfficerRankName(int officerTier)
        {
            switch (officerTier)
            {
                case 1: return "Captain";
                case 2: return "Major";
                case 3: return "Lieutenant";
                case 4: return "Colonel";
                case 5: return "General";
                default: return "unknown";
            }
        }
        public static string GetOfficerRankAbbreviation(int officerTier)
        {
            switch (officerTier)
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
