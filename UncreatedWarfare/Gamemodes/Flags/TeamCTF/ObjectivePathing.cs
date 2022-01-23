//#define NONADJACENCIES
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes.Flags.TeamCTF
{
    public static class ObjectivePathing
    {
        public static List<Flag> PathWithAdjacents(List<Flag> Selection, AdjacentFlagData[] T1Adjacents, AdjacentFlagData[] T2Adjacents)
        {
            List<Flag> path = new List<Flag>();
            StartAdjacentsLoop(path, Selection, T1Adjacents, T2Adjacents);
            return path;
        }
        private static void StartAdjacentsLoop(List<Flag> flags, List<Flag> selection, AdjacentFlagData[] t1adjacents, AdjacentFlagData[] t2adjacents)
        {
            Flag first = PickRandomFlagWithSpecifiedBias(InstantiateFlags(t1adjacents, selection, flags, null));
            if (first == null)
            {
                L.LogError("Unable to pick the first flag.");
                return;
            }
            flags.Add(first);
            flags[0].index = 0;
            AdjacentsFlagLoop(flags, selection, t2adjacents);
        }
        private static void AdjacentsFlagLoop(List<Flag> flags, List<Flag> selection, AdjacentFlagData[] t2adjacents)
        {
            Flag lastFlag = flags.Last();
            if (lastFlag == null)
            {
                L.LogError("Last flag was null, breaking loop.");
                return;
            }
            Dictionary<Flag, float> initBiases = InstantiateFlags(lastFlag.Adjacencies, selection, flags, lastFlag);
            float mainBias = float.NaN;
            for (int i = 0; i < t2adjacents.Length; i++)
            {
                if (t2adjacents[i].flag_id == lastFlag.ID)
                {
                    mainBias = t2adjacents[i].weight;
                }
            }
            if (float.IsNaN(mainBias))
            {
                Flag pick = PickRandomFlagWithSpecifiedBias(initBiases);
                if (pick != null)
                {
                    pick.index = flags.Count;
                    flags.Add(pick);
                }
                else
                {
                    L.LogError("Pick was null after " + lastFlag.Name);
                    return;
                }
                AdjacentsFlagLoop(flags, selection, t2adjacents);
            }
            else if (!PickRandomFlagOrMainWithSpecifiedBias(initBiases, mainBias, out Flag newFlag))
            {
                newFlag.index = flags.Count;
                flags.Add(newFlag);
                AdjacentsFlagLoop(flags, selection, t2adjacents);
            }
        }
        public static Dictionary<Flag, float> InstantiateFlags(AdjacentFlagData[] flags, List<Flag> selection, List<Flag> toNotRemove, Flag current)
        {
            Dictionary<Flag, float> rtn = new Dictionary<Flag, float>();
            for (int i = 0; i < flags.Length; i++)
            {
                AdjacentFlagData afd = flags[i];
                Flag f = selection.FirstOrDefault(x => x.ID == afd.flag_id);
                if (f != default)
                {
                    if (!rtn.ContainsKey(f) && (toNotRemove == null || !toNotRemove.Exists(x => x.ID == afd.flag_id)))
                        rtn.Add(f, afd.weight);
                }
                else if (current != null)
                {
                    L.LogWarning("Invalid flag id in adjacents dictionary for flag " + current.Name);
                }
                else
                {
                    L.LogWarning("Invalid flag id in adjacents dictionary for team 1 main base.");
                }
            }
            return rtn;
        }

        private static Flag PickRandomFlagWithSpecifiedBias(Dictionary<Flag, float> biases)
        {
            if (biases.Count < 1)
            {
                L.LogError("Biases was empty.");
                return default;
            }
            float total = 0;
            foreach (KeyValuePair<Flag, float> flag in biases)
            {
                total += flag.Value;
            }
            float pick = Random.Range(0, total);
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
