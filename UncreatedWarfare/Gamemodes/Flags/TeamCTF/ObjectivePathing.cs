using SDG.Unturned;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes.Flags.TeamCTF
{
    public static class ObjectivePathing
    {
        public static float MAIN_SEARCH_RADIUS = 1200f;
        public static float MAIN_STOP_RADIUS = 1600f;
        public static float ABSOLUTE_MAX_DISTANCE_FROM_MAINS = 1900f;
        public static float FLAG_RADIUS_SEARCH = 1200f; // can double if no flags are found
        public static float FORWARD_BIAS = 0.65f;
        public static float BACK_BIAS = 0.05f;
        public static float LEFT_BIAS = 0.15f;
        public static float RIGHT_BIAS = 0.15f;
        public static float DISTANCE_EFFECT = 0.1f;
        public static float AVERAGE_DISTANCE_BUFFER = 2400f; // idea for tomorrow, get average distance from each main and check with buffer.
        public static float RADIUS_TUNING_RESOLUTION = 40f;
        public static float MAIN_BASE_ANGLE_OFFSET = 0;
        public static float SIDE_ANGLE_LEFT_START_DEFAULT = 5 * Mathf.PI / 12; // 75
        public static float SIDE_ANGLE_LEFT_END_DEFAULT = 7 * Mathf.PI / 12; // 105
        public static float SIDE_ANGLE_RIGHT_START_DEFAULT = 17 * Mathf.PI / 12; // 255
        public static float SIDE_ANGLE_RIGHT_END_DEFAULT = 19 * Mathf.PI / 12; // 285
        public static float SIDE_ANGLE_LEFT_START = 0;
        public static float SIDE_ANGLE_LEFT_END = 0;
        public static float SIDE_ANGLE_RIGHT_START = 0;
        public static float SIDE_ANGLE_RIGHT_END = 0;
        public static int MAX_FLAGS = 8;
        public static int MIN_FLAGS = 5;
        public static int MAX_REDOS = 20;
        public static void SetVariables(
            float MAIN_SEARCH_RADIUS,
            float MAIN_STOP_RADIUS,
            float ABSOLUTE_MAX_DISTANCE_FROM_MAINS,
            float FLAG_RADIUS_SEARCH,
            float FORWARD_BIAS,
            float BACK_BIAS,
            float LEFT_BIAS,
            float RIGHT_BIAS,
            float DISTANCE_EFFECT,
            float AVERAGE_DISTANCE_BUFFER,
            float RADIUS_TUNING_RESOLUTION,
            int MAX_FLAGS,
            int MIN_FLAGS,
            int MAX_REDOS)
        {
            ObjectivePathing.MAIN_SEARCH_RADIUS = MAIN_SEARCH_RADIUS;
            ObjectivePathing.MAIN_STOP_RADIUS = MAIN_STOP_RADIUS;
            ObjectivePathing.ABSOLUTE_MAX_DISTANCE_FROM_MAINS = ABSOLUTE_MAX_DISTANCE_FROM_MAINS;
            ObjectivePathing.FLAG_RADIUS_SEARCH = FLAG_RADIUS_SEARCH;
            ObjectivePathing.FORWARD_BIAS = FORWARD_BIAS;
            ObjectivePathing.BACK_BIAS = BACK_BIAS;
            ObjectivePathing.LEFT_BIAS = LEFT_BIAS;
            ObjectivePathing.RIGHT_BIAS = RIGHT_BIAS;
            ObjectivePathing.DISTANCE_EFFECT = DISTANCE_EFFECT;
            ObjectivePathing.AVERAGE_DISTANCE_BUFFER = AVERAGE_DISTANCE_BUFFER;
            ObjectivePathing.RADIUS_TUNING_RESOLUTION = RADIUS_TUNING_RESOLUTION;
            ObjectivePathing.MAX_FLAGS = MAX_FLAGS;
            ObjectivePathing.MIN_FLAGS = MIN_FLAGS;
            ObjectivePathing.MAX_REDOS = MAX_REDOS;
            ObjectivePathing.MAIN_BASE_ANGLE_OFFSET = GetObjectiveAngleDifferenceRad();
            F.Log(MAIN_BASE_ANGLE_OFFSET.ToString(Data.Locale));
            ObjectivePathing.SIDE_ANGLE_LEFT_END = RotateAngleFromOriginal(ObjectivePathing.SIDE_ANGLE_LEFT_END_DEFAULT);
            ObjectivePathing.SIDE_ANGLE_LEFT_START = RotateAngleFromOriginal(ObjectivePathing.SIDE_ANGLE_LEFT_START_DEFAULT);
            ObjectivePathing.SIDE_ANGLE_RIGHT_END = RotateAngleFromOriginal(ObjectivePathing.SIDE_ANGLE_RIGHT_END_DEFAULT);
            ObjectivePathing.SIDE_ANGLE_RIGHT_START = RotateAngleFromOriginal(ObjectivePathing.SIDE_ANGLE_RIGHT_START_DEFAULT);

            F.Log($"angle offset: {MAIN_BASE_ANGLE_OFFSET * 180 / Mathf.PI}: \n" +
                $"side left end: {SIDE_ANGLE_LEFT_END * 180 / Mathf.PI}, \n" +
                $"side left start: {SIDE_ANGLE_LEFT_START * 180 / Mathf.PI}, \n" +
                $"side right end: {SIDE_ANGLE_RIGHT_END * 180 / Mathf.PI}, \n" +
                $"side right start {SIDE_ANGLE_RIGHT_START * 180 / Mathf.PI}");
        }
        private static float GetObjectiveAngleDifferenceRad() =>
            Mathf.Atan(((TeamManager.Team1Main.Center.y - TeamManager.Team2Main.Center.y) / (TeamManager.Team1Main.Center.x - TeamManager.Team2Main.Center.x)));
        private static float RotateAngleFromOriginal(float original) => ClampRadians(original + MAIN_BASE_ANGLE_OFFSET);
        private static float ClampRadians(float unclamped)
        {
            float n = unclamped;
            while (n > Mathf.PI * 2)
                n -= Mathf.PI * 2;
            while (n < 0)
                n += Mathf.PI * 2;
            return n;
        }
        public static List<Flag> CreateAutoPath(List<Flag> rotation)
        {
            List<Flag> path = new List<Flag>();
            StartLoop(ref path, rotation);
            int redoCounter = 0;
            while (redoCounter < MAX_REDOS &&
                (path.Count < MIN_FLAGS ||
                (TeamManager.Team2Main.Center - path.Last().Position2D).sqrMagnitude > ABSOLUTE_MAX_DISTANCE_FROM_MAINS * ABSOLUTE_MAX_DISTANCE_FROM_MAINS ||
                Mathf.Abs(GetAverageDistanceFromTeamMain(true, path) - GetAverageDistanceFromTeamMain(false, path)) > AVERAGE_DISTANCE_BUFFER
                ))
            { // checks for paths with too few flags or that end too far away from team 2 main
                path.Clear();
                StartLoop(ref path, rotation);
                redoCounter++;
            }
            if (redoCounter >= MAX_REDOS)
                F.LogError("Unable to correct bad path after " + MAX_REDOS.ToString(Data.Locale) + " tries.");
            return path;
        }
        private static void StartLoop(ref List<Flag> list, List<Flag> rotation)
        {
            List<Flag> StarterFlags = GetFlagsInRadius(TeamManager.Team1Main.Center, MAIN_SEARCH_RADIUS, rotation);
            if (StarterFlags.Count == 0)
            {
                F.LogError("Objective Pathing was unable to find the first flags around main in a " + MAIN_SEARCH_RADIUS + "m radius of " + TeamManager.Team1Main.Center + " out of " + rotation.Count + " flags.");
                return;
            }
            Flag first = PickRandomFlagWithBias(TeamManager.Team1Main.Center, StarterFlags);
            first.index = 0;
            list.Add(first);
            //F.Log(list.Count + ". " + list[0].Name, ConsoleColor.Green);
            int counter = 0;
            FlagLoop(ref list, ref counter, rotation);
        }
        private static float GetAverageDistanceFromTeamMain(bool team1, List<Flag> list)
        {
            int i = 0;
            float total = 0;
            for (; i < list.Count; i++)
            {
                total += (team1 ? TeamManager.Team1Main.Center : TeamManager.Team2Main.Center - list[i].Position2D).sqrMagnitude;
            }
            float avg = Mathf.Sqrt(total / i);
            return avg;
        }
        private static void FlagLoop(ref List<Flag> list, ref int counter, List<Flag> rotation)
        {
            Flag lastFlag = list.Last();
            List<Flag> candidates = GetFlagsInRadiusExclude(lastFlag.Position2D, FLAG_RADIUS_SEARCH, rotation, lastFlag.ID, list);
            float oldradius = FLAG_RADIUS_SEARCH;
            int uppingCounter = 0;
            int countermax = (int)Mathf.Round(FLAG_RADIUS_SEARCH / RADIUS_TUNING_RESOLUTION);
            while (candidates.Count == 0 && uppingCounter <= countermax)
            {
                uppingCounter++;
                FLAG_RADIUS_SEARCH = oldradius + RADIUS_TUNING_RESOLUTION * uppingCounter;
                candidates = GetFlagsInRadiusExclude(lastFlag.Position2D, FLAG_RADIUS_SEARCH, rotation, lastFlag.ID, list);
                //F.Log(uppingCounter.ToString(Data.Locale) + "th search: " + candidates.Count + " results in " + FLAG_RADIUS_SEARCH.ToString(Data.Locale) + 'm');
                if (candidates.Count < 1) continue;
                lastFlag = PickRandomFlagWithBias(lastFlag.Position2D, candidates);
            }
            //if (uppingCounter != 0) F.Log("Had to raise \"FLAG_RADIUS_SEARCH\" to " + FLAG_RADIUS_SEARCH + " until a flag was found.");
            if (candidates.Count == 0)
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
                FlagLoop(ref list, ref counter, rotation);
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
            if (angle.Between(SIDE_ANGLE_LEFT_START, 0f, false, true) || angle.Between(0f, SIDE_ANGLE_RIGHT_END, true, false))
                return FORWARD_BIAS;
            else if (angle.Between(SIDE_ANGLE_LEFT_START, SIDE_ANGLE_LEFT_END, true, true)) return LEFT_BIAS;
            else if (angle.Between(SIDE_ANGLE_RIGHT_START, SIDE_ANGLE_RIGHT_END, true, true)) return RIGHT_BIAS;
            else return BACK_BIAS;
        }
        public static List<Flag> GetFlagsInRadius(Vector2 center, float radius, List<Flag> Rotation) => Rotation.Where(flag => (flag.Position2D - center).sqrMagnitude <= radius * radius).ToList();
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
        public static List<Flag> CreatePathUsingLevels(List<Flag> rotation, int MaxFlagsPerLevel)
        {
            List<Flag> Rotation = new List<Flag>();
            List<KeyValuePair<int, List<Flag>>> lvls = new List<KeyValuePair<int, List<Flag>>>();
            for (int i = 0; i < rotation.Count; i++)
            {
                KeyValuePair<int, List<Flag>> flag = lvls.FirstOrDefault(x => x.Key == rotation[i].Level);
                if (flag.Equals(default(KeyValuePair<int, List<Flag>>)))
                    lvls.Add(new KeyValuePair<int, List<Flag>>(rotation[i].Level, new List<Flag> { rotation[i] }));
                else
                    flag.Value.Add(rotation[i]);
            }
            lvls.Sort((KeyValuePair<int, List<Flag>> a, KeyValuePair<int, List<Flag>> b) => a.Key.CompareTo(b.Key));
            for (int i = 0; i < lvls.Count; i++)
            {
                int amtToAdd = lvls[i].Value.Count > MaxFlagsPerLevel ? MaxFlagsPerLevel : lvls[i].Value.Count;
                int counter = 0;
                while (counter < amtToAdd)
                {
                    int index = UnityEngine.Random.Range(0, lvls[i].Value.Count - 1);
                    lvls[i].Value[index].index = Rotation.Count;
                    Rotation.Add(lvls[i].Value[index]);
                    lvls[i].Value.RemoveAt(index);
                    counter++;
                }
            }
            return Rotation;
        }

        public static List<Flag> PathWithAdjacents(List<Flag> Selection, Dictionary<int, float> T1Adjacents, Dictionary<int, float> T2Adjacents)
        {
            List<Flag> path = new List<Flag>();
            StartAdjacentsLoop(path, Selection, T1Adjacents, T2Adjacents);
            return path;
        }
        private static void StartAdjacentsLoop(List<Flag> flags, List<Flag> selection, Dictionary<int, float> t1adjacents, Dictionary<int, float> t2adjacents)
        {
            flags.Add(PickRandomFlagWithSpecifiedBias(InstantiateFlags(t1adjacents, selection, flags, null)));
            flags[0].index = 0;
            AdjacentsFlagLoop(flags, selection, t2adjacents);
        }
        private static void AdjacentsFlagLoop(List<Flag> flags, List<Flag> selection, Dictionary<int, float> t2adjacents)
        {
            Flag lastFlag = flags.Last();
            if (lastFlag == null)
            {
                F.LogError("Last flag was null, breaking loop.");
                return;
            }
            Dictionary<Flag, float> initBiases = InstantiateFlags(lastFlag.Adjacencies, selection, flags, lastFlag);
            if (!t2adjacents.TryGetValue(lastFlag.ID, out float mainBias))
            {
                Flag pick = PickRandomFlagWithSpecifiedBias(initBiases);
                if (pick != null)
                {
                    pick.index = flags.Count;
                    flags.Add(pick);
                }
                else
                {
                    F.LogError("Pick was null after " + lastFlag.Name);
                    return;
                }
                AdjacentsFlagLoop(flags, selection, t2adjacents);
            }
            else
            {
                if (!PickRandomFlagOrMainWithSpecifiedBias(initBiases, mainBias, out Flag newFlag))
                {
                    newFlag.index = flags.Count;
                    flags.Add(newFlag);
                    AdjacentsFlagLoop(flags, selection, t2adjacents);
                }
            }
        }
        public static Dictionary<Flag, float> InstantiateFlags(Dictionary<int, float> flags, List<Flag> selection, List<Flag> toNotRemove, Flag current)
        {
            Dictionary<Flag, float> rtn = new Dictionary<Flag, float>();
            foreach (KeyValuePair<int, float> flag in flags)
            {
                Flag f = selection.FirstOrDefault(x => x.ID == flag.Key);
                if (f != default)
                {
                    if (toNotRemove == null || !toNotRemove.Exists(x => x.ID == flag.Key))
                        rtn.Add(f, flag.Value);
                }
                else if (current != null) F.LogWarning("Invalid flag id in adjacents dictionary for flag " + current.Name);
                else F.LogWarning("Invalid flag id in adjacents dictionary for team 1 main base.");
            }
            return rtn;
        }

        private static Flag PickRandomFlagWithSpecifiedBias(Dictionary<Flag, float> biases)
        {
            if (biases.Count < 1)
            {
                F.Log("Biases was empty.");
                return default;
            }
            float total = 0;
            foreach (KeyValuePair<Flag, float> flag in biases)
            {
                total += flag.Value;
            }
            float pick = UnityEngine.Random.Range(0, total);
            float counter = 0;
            foreach (KeyValuePair<Flag, float> flag in biases)
            {
                counter += flag.Value;
                if (pick <= counter) return flag.Key;
            }
            return biases.ElementAt(0).Key;
        }
        private static bool PickRandomFlagOrMainWithSpecifiedBias(Dictionary<Flag, float> biases, float mainBias, out Flag output)
        {
            if (biases.Count < 1)
            {
                output = default;
                return true;
            }
            List<FlagMainTuple> tuples = new List<FlagMainTuple>();
            tuples.Add(new FlagMainTuple(null, true, mainBias));
            float total = mainBias;
            foreach (KeyValuePair<Flag, float> flag in biases)
            {
                total += flag.Value;
                tuples.Add(new FlagMainTuple(flag.Key, false, flag.Value));
            }
            float pick = UnityEngine.Random.Range(0, total);
            float counter = 0;
            foreach (FlagMainTuple tuple in tuples)
            {
                counter += tuple.bias;
                if (pick <= counter)
                {
                    output = tuple.flag;
                    return tuple.isMain;
                }
            }
            output = null;
            return true;
        }
        private struct FlagMainTuple
        {
            public Flag flag;
            public bool isMain;
            public float bias;
            public FlagMainTuple(Flag flag, bool isMain, float bias)
            {
                this.flag = flag;
                this.isMain = isMain;
                this.bias = bias;
            }
        }

        public enum EPathingMode : byte
        {
            LEVELS,
            AUTODISTANCE,
            ADJACENCIES
        }
    }
}
