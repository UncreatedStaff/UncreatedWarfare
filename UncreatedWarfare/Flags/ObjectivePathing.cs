using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.Flags
{
    public static class ObjectivePathing
    {
        public static float MAIN_RADIUS_SEARCH = 1200f;
        public static float MAIN_STOP_RADIUS = 1600f;
        public static float ABSOLUTE_MAX_DISTANCE_FROM_MAINS = 1900f;
        public static float FLAG_RADIUS_SEARCH = 1200f; // can double if no flags are found
        public static float FORWARD_BIAS = 0.65f;
        public static float BACK_BIAS = 0.05f;
        public static float SIDE_BIAS = 0.3f;
        public static float DISTANCE_EFFECT = 0.1f;
        public static float AVERAGE_DISTANCE_BUFFER = 2400f; // idea for tomorrow, get average distance from each main and check with buffer.
        public static int MAX_FLAGS = 8;
        public static int MIN_FLAGS = 5;
        public static int MAX_REDOS = 20;
        public const float RADIUS_TUNING_RESOLUTION = 40f;
        public static List<Flag> CreatePath()
        {
            List<Flag> path = new List<Flag>();
            UnityEngine.Random random = new UnityEngine.Random();
            StartLoop(ref path, random);
            int redoCounter = 0;
            while(redoCounter < MAX_REDOS && 
                (path.Count < MIN_FLAGS || 
                (TeamManager.Team2Main.Center - path.Last().Position2D).sqrMagnitude > ABSOLUTE_MAX_DISTANCE_FROM_MAINS * ABSOLUTE_MAX_DISTANCE_FROM_MAINS || 
                Mathf.Abs(GetAverageDistanceFromTeamMain(true, path) - GetAverageDistanceFromTeamMain(false, path)) > AVERAGE_DISTANCE_BUFFER
                ))
            { // checks for paths with too few flags or that end too far away from team 2 main
                path.Clear();
                StartLoop(ref path, random);
                redoCounter++;
            }
            if(redoCounter >= MAX_REDOS)
                F.LogError("Unable to correct bad path after " + MAX_REDOS.ToString() + " tries.");
            return path;
        }
        private static void StartLoop(ref List<Flag> list, UnityEngine.Random r)
        {
            List<Flag> StarterFlags = GetFlagsInRadius(TeamManager.Team1Main.Center, MAIN_RADIUS_SEARCH);
            Flag first = PickRandomFlagWithBias(TeamManager.Team1Main.Center, StarterFlags);
            first.index = 0;
            list.Add(first);
            //F.Log(list.Count + ". " + list[0].Name, ConsoleColor.Green);
            int counter = 0;
            FlagLoop(ref list, ref counter);
        }
        private static float GetAverageDistanceFromTeamMain(bool team1, List<Flag> list)
        {
            int i = 0;
            float total = 0;
            for(; i < list.Count; i++)
            {
                total += (team1 ? TeamManager.Team1Main.Center : TeamManager.Team2Main.Center - list[i].Position2D).sqrMagnitude;
            }
            float avg = Mathf.Sqrt(total / i);
            return avg;
        }
        private static void FlagLoop(ref List<Flag> list,  ref int counter)
        {
            Flag lastFlag = list.Last();
            List<Flag> candidates = GetFlagsInRadiusExclude(lastFlag.Position2D, FLAG_RADIUS_SEARCH, lastFlag.ID, list);
            float oldradius = FLAG_RADIUS_SEARCH;
            int uppingCounter = 0;
            int countermax = (int)Mathf.Round(FLAG_RADIUS_SEARCH / RADIUS_TUNING_RESOLUTION);
            while (candidates.Count == 0 && uppingCounter <= countermax)
            {
                uppingCounter++;
                FLAG_RADIUS_SEARCH = oldradius + RADIUS_TUNING_RESOLUTION * uppingCounter;
                candidates = GetFlagsInRadiusExclude(lastFlag.Position2D, FLAG_RADIUS_SEARCH, lastFlag.ID, list);
                //F.Log(uppingCounter.ToString() + "th search: " + candidates.Count + " results in " + FLAG_RADIUS_SEARCH.ToString() + 'm');
                if (candidates.Count < 1) continue;
                lastFlag = PickRandomFlagWithBias(lastFlag.Position2D, candidates);
            }
            //if (uppingCounter != 0) F.Log("Had to raise \"FLAG_RADIUS_SEARCH\" to " + FLAG_RADIUS_SEARCH + " until a flag was found.");
            if(candidates.Count == 0)
            {
                F.LogError("Ran out of flags before reaching the team 2 base.");
                return;
            }
            FLAG_RADIUS_SEARCH = oldradius;
            Flag pick = PickRandomFlagWithBias(lastFlag.Position2D, candidates);
            pick.index = list.Count;
            list.Add(pick);
            //F.Log(list.Count + ". " + pick.Name, ConsoleColor.Green);
            counter++;
            if (counter < MAX_FLAGS - 1 && (TeamManager.Team2Main.Center - pick.Position2D).sqrMagnitude > MAIN_STOP_RADIUS * MAIN_STOP_RADIUS) // if the picked flag is not in range of team 2 main base. 
            {
                FlagLoop(ref list, ref counter);
            }
        }
        private static Flag PickRandomFlagWithBias(Vector2 origin, List<Flag> candidates)
        {
            if (candidates.Count < 1) return default;
            Dictionary<Flag, int> values = new Dictionary<Flag, int>();
            int total = 0;
            foreach (Flag flag in candidates)
            {
                float angle_bias = GetAngleBiasFromOrigin(origin, flag.Position2D, out float distance);
                int bias = (int)Mathf.Round(100 * angle_bias * (Level.size - distance) * DISTANCE_EFFECT);
                total += bias;
                values.Add(flag, bias);
            }
            int pick = UnityEngine.Random.Range(0, total);
            int counter = 0;
            foreach (KeyValuePair<Flag, int> flag in values)
            {
                counter += flag.Value;
                if (pick <= counter) return flag.Key;
            }
            return candidates[0];
        }
        private static float GetAngleBiasFromOrigin(Vector2 origin, Vector2 position, out float distance)
        {
            float angle = GetAngleFromCenter(origin, position, out distance);
            if (angle.Between(Mathf.PI / 6f /* 30° */, 0f, true, false) || angle.Between(4f * Mathf.PI / 3f  /* 240° */, 2f * Mathf.PI /* 360° */, true, true)) return BACK_BIAS;
            else if (angle.Between(Mathf.PI / 3 /* 60° */, Mathf.PI / 6f /* 30° */, true, false)) return SIDE_BIAS / 2;
            else if (angle.Between(4f * Mathf.PI / 3f  /* 240° */, 7f * Mathf.PI / 6f  /* 210° */, false, true)) return SIDE_BIAS / 2;
            else return FORWARD_BIAS;
        }
        public static List<Flag> GetFlagsInRadius(Vector2 center, float radius) => GetFlagsInRadius(center, radius, Data.FlagManager.AllFlags);
        public static List<Flag> GetFlagsInRadius(Vector2 center, float radius, List<Flag> Rotation) => Rotation.Where(flag => (flag.Position2D - center).sqrMagnitude <= radius * radius).ToList();
        public static List<Flag> GetFlagsInRadiusExclude(Vector2 center, float radius, int excluded_flag_id, List<Flag> history) => GetFlagsInRadiusExclude(center, radius, Data.FlagManager.AllFlags, excluded_flag_id, history);
        public static List<Flag> GetFlagsInRadiusExclude(Vector2 center, float radius, List<Flag> Rotation, int excluded_flag_id, List<Flag> history) => 
            Rotation.Where(flag => !history.Exists(f => f.ID == flag.ID) && flag.ID != excluded_flag_id && (flag.Position2D - center).sqrMagnitude <= radius * radius).ToList();
        public static float GetAngleFromCenter(Vector2 center, Vector2 point, out float distance, bool degrees = false)
        {
            if (center == point)
            {
                distance = 0;
                return 0;
            }
            Vector2 offsetted = point - center;
            float radius = offsetted.magnitude;
            Vector2 scaled = offsetted / radius;
            float arccos = Mathf.Acos(scaled.x);
            float arcsin = Mathf.Asin(scaled.y);
            distance = radius;
            if (center.x > 0) return degrees ? (Mathf.PI / 180) * arccos : arccos; // return arccos if in q1 or q2
            else if (center.y > 0) return degrees ? (Mathf.PI / 180) * arcsin : arcsin; // return arcsin if in q4
            else return degrees ? (Mathf.PI / 180) * (arccos + (Mathf.PI / 2)) : arccos + (Mathf.PI / 2); // add 90° to arccos if in q3
        }
    }
}
