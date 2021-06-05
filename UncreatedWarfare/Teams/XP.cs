using System;
namespace Uncreated.Warfare.Teams
{
    public static class XP
    {
        public const double d = 200d;
        public const double a = 500d;
        public static string GetRankName(uint level)
        {
            if (level < 3)
                return "Private";
            else if (level < 5)
                return "Private 1st Class";
            else if (level < 8)
                return "Corporal";
            else if (level < 11)
                return "Specialist";
            else if (level < 13)
                return "Sergeant";
            else if (level < 15)
                return "Staff Sergeant";
            else if (level < 17)
                return "Sergeant 1st Class";
            else if (level < 19)
                return "Sergeant Major";
            else if (level < 22)
                return "Warrant Officer";
            else
                return "Chief Warrant Officer";
        }
        public static string GetRankAbbreviation(uint level)
        {
            if (level < 3)
                return "Pvt.";
            else if (level < 5)
                return "Pfc.";
            else if (level < 8)
                return "Cpl";
            else if (level < 11)
                return "Spec.";
            else if (level < 13)
                return "Sgt.";
            else if (level < 15)
                return "Ssg.";
            else if (level < 17)
                return "Sfc.";
            else if (level < 19)
                return "Sm.";
            else if (level < 22)
                return "W.O.";
            else
                return "C.W.O.";
        }
        public static uint XPtoLevel(uint xp) => (uint)Math.Floor(Alpha(xp));
        private static double Alpha(uint xp) => 1 + ((0.5 * d) - a + Math.Sqrt(Math.Pow(a - 0.5 * d, 2) + (2 * d * xp))) / d;
        public static uint PrecentageComplete(uint xp)
        {
            uint lvl = XPtoLevel(xp);
            return LevelXP(xp, lvl) / LevelRequiredXP(lvl);
        }
        private static uint LevelRequiredXP(uint lvl) => (uint)Math.Round(lvl / 2.0 * ((2 * a) + ((lvl - 1) * d)) - (lvl - 1) / 2.0 * ((2 * a) + ((lvl - 2) * d)));
        public static uint LevelXP(uint xp) => LevelXP(xp, XPtoLevel(xp));
        private static uint LevelXP(uint xp, uint lvl) => (uint)Math.Round(LevelRequiredXP(lvl) - ((lvl / 2.0 * ((2 * a) + ((lvl - 1) * d))) - xp));
        
    }
}
