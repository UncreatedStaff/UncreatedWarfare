﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Networking.Encoding;
using UnityEngine;

namespace Uncreated.Warfare.Point
{
    public class RankData
    {
        public static readonly RankData Nil = new RankData(0, -1, EBranch.DEFAULT, 0);
        public readonly ulong Steam64;
        public readonly EBranch Branch;
        public int TotalXP;
        public int Level;
        public int RankTier;
        public int OfficerTier;
        public ulong OfficerTeam;
        public int CurrentXP;
        public int RequiredXP;
        public string Name;
        public string Abbreviation;
        public RankData(ulong steamID, int xp, EBranch branch, ulong officerTeam)
        {
            Steam64 = steamID;
            Branch = branch;

            // have to assign all variables in constructor in structures

            TotalXP = -1;
            Level = -1;
            CurrentXP = -1;
            RequiredXP = -1;

            RankTier = -1;

            Name = null;
            Abbreviation = null;

            OfficerTeam = officerTeam;
            OfficerTier = -1;

            Update(xp);

        }

        public bool IsNil => TotalXP == -1;
        private const int A = 600;
        private const int D = 200;
        public void Update(int newXP)
        {
            TotalXP = newXP;

            float x = D / 2f;
            float y = A - (3 * x);
            float z = 0 - x - y;
            Level = Mathf.RoundToInt(Mathf.Floor((x - A + Mathf.Sqrt(Mathf.Pow(A - x, 2f) + (2f * D * TotalXP))) / D));

            float n = Level + 1;
            CurrentXP = (int)(TotalXP - ((x * Math.Pow(n, 2)) + (y * n) + z));
            RequiredXP = A + Level * D;

            RankTier = GetRankTier(Level);

            CheckOfficerStatus();
            CheckNameAbbreviations();
        }
        public void CheckOfficerStatus()
        {
            if (OfficerStorage.IsOfficer(Steam64, OfficerTeam, out OfficerData officer))
            {
                OfficerTier = officer.OfficerTier;
            }
            else
            {
                OfficerTier = 0;
            }
        }
        public void CheckNameAbbreviations()
        {
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
            return rankTier switch
            {
                0 => "Recruit",
                1 => "Private",
                2 => "Private 1st Class",
                3 => "Corporal",
                4 => "Specialist",
                5 => "Sergeant",
                6 => "Staff Sergeant",
                7 => "Sergeant 1st Class",
                8 => "Sergeant Major",
                9 => "Warrant Officer",
                10 => "Chief Warrant Officer",
                _ => "unknown",
            };
        }
        public static string GetRankAbbreviation(int rankTier)
        {
            return rankTier switch
            {
                0 => "Rec.",
                1 => "Pvt.",
                2 => "Pfc.",
                3 => "Col.",
                4 => "Spec.",
                5 => "Sgt.",
                6 => "Ssg.",
                7 => "Sfc.",
                8 => "S.M.",
                9 => "W.O.",
                10 => "C.W.O.",
                _ => "###",
            };
        }

        public static string GetOfficerRankName(int officerTier)
        {
            return officerTier switch
            {
                1 => "Captain",
                2 => "Major",
                3 => "Lieutenant",
                4 => "Colonel",
                5 => "General",
                _ => "unknown",
            };
        }
        public static string GetOfficerRankAbbreviation(int officerTier)
        {
            return officerTier switch
            {
                1 => "Cpt.",
                2 => "Maj.",
                3 => "Lt.",
                4 => "Col.",
                5 => "Gen.",
                _ => "###",
            };
        }
    }
}