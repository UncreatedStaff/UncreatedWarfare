//#define NONADJACENCIES
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Gamemodes.Flags.TeamCTF;

public static class ObjectivePathing
{
    public static bool TryPath(List<Flag> output)
    {
        if (Data.Gamemode is not IFlagRotation rotation) throw new InvalidOperationException("Expected IFlagRotation gamemode.");

        List<Flag> selection = rotation.LoadedFlags;
        AdjacentFlagData[] t1adjacencies = TeamManager.Team1Main.Data.Adjacencies;
        AdjacentFlagData[] t2adjacencies = TeamManager.Team2Main.Data.Adjacencies;
        if (selection is null || selection.Count < 1)
        {
            L.LogWarning("Flag selection is not loaded.", method: "FLAG PATHING");
            return false;
        }
        if (t1adjacencies is null || t1adjacencies.Length < 1)
        {
            L.LogWarning("Team 1 Adjacencies are not defined.", method: "FLAG PATHING");
            return false;
        }
        if (t2adjacencies is null || t2adjacencies.Length < 1)
        {
            L.LogWarning("Team 2 Adjacencies are not defined.", method: "FLAG PATHING");
            return false;
        }

        float ttl = 0f;
        for (int i = 0; i < t1adjacencies.Length; ++i)
            ttl += t1adjacencies[i].weight;
        float pick = UnityEngine.Random.Range(0, ttl);
        ttl = 0f;
        int id = -1;
        for (int i = 0; i < t1adjacencies.Length; ++i)
        {
            ref AdjacentFlagData d = ref t1adjacencies[i];
            ttl += d.weight;
            if (pick <= ttl)
            {
                id = d.flag_id;
                break;
            }
        }

        if (id == -1)
            id = t1adjacencies[t1adjacencies.Length - 1].flag_id;

        int c = -1;
        Flag last;
        while (!TryGetFlag(id, selection, out last))
        {
            if (++c < t1adjacencies.Length)
                id = t1adjacencies[c].flag_id;
            else break;
        }
        if (last is null)
        {
            L.LogWarning("First flag could not be detected, check the flag_id values on the t1main adjacencies.");
            return false;
        }

        output.Add(last);
        last.index = 0;

        while (true)
        {
            AdjacentFlagData[] adjs = last.ZoneData.Data.Adjacencies;
            int lid = last.ID;
            float t2MainWeight = 0f;
            for (int i = 0; i < t2adjacencies.Length; ++i)
            {
                ref AdjacentFlagData d = ref t2adjacencies[i];
                if (d.flag_id == lid)
                {
                    t2MainWeight = d.weight;
                    break;
                }
            }
            if (adjs is null || adjs.Length < 1)
            {
                if (t2MainWeight > 0f)
                    goto closeLoop;
            }
            else
            {
                bool[] filter = new bool[adjs.Length];
                ttl = t2MainWeight;
                for (int i = 0; i < adjs.Length; ++i)
                {
                    ref AdjacentFlagData d = ref adjs[i];
                    for (int j = 0; j < output.Count; ++j)
                    {
                        if (output[j].ID == d.flag_id)
                        {
                            filter[i] = true;
                            break;
                        }
                    }

                    if (!filter[i])
                        ttl += d.weight;
                }

                if (ttl == t2MainWeight)
                {
                    if (ttl <= 0)
                        L.LogWarning("Got stuck at flag ID " + lid + " trying to find the next flag.");
                    goto closeLoop;
                }
                pick = UnityEngine.Random.Range(0, ttl);
                if (t2MainWeight > 0f && pick <= t2MainWeight)
                    goto closeLoop;
                ttl = t2MainWeight;
                id = -1;
                for (int i = 0; i < adjs.Length; ++i)
                {
                    if (filter[i]) continue;
                    ref AdjacentFlagData d = ref adjs[i];
                    ttl += d.weight;
                    if (pick <= ttl)
                    {
                        id = d.flag_id;
                        break;
                    }
                }
                if (id == -1)
                {
                    int ind = adjs.Length - 1;
                    if (filter[ind])
                    {
                        if (t2MainWeight <= 0)
                            L.LogWarning("Got stuck at flag ID " + lid + " trying to find the next flag.");
                        goto closeLoop;
                    }
                    id = adjs[ind].flag_id;
                }
                if (!TryGetFlag(id, selection, out last))
                {
                    if (ttl <= 0)
                        L.LogWarning("Got stuck at flag ID " + lid + " trying to find the next flag.");
                    goto closeLoop;
                }

                last.index = output.Count;
                output.Add(last);
            }
        }

    closeLoop:
        return true;
    }
    public static bool TryGetFlag(int id, List<Flag> selection, out Flag flag)
    {
        for (int i = 0; i < selection.Count; ++i)
        {
            if (selection[i].ID == id)
            {
                flag = selection[i];
                return true;
            }
        }
        flag = null!;
        return false;
    }
    public static int PickWeightedAdjacency(AdjacentFlagData[] adj)
    {
        float ttl = 0f;
        for (int i = 0; i < adj.Length; ++i)
            ttl += adj[i].weight;
        float pick = UnityEngine.Random.Range(0, ttl);
        ttl = 0f;
        int id = -1;
        for (int i = 0; i < adj.Length; ++i)
        {
            ref AdjacentFlagData d = ref adj[i];
            ttl += d.weight;
            if (pick <= ttl)
            {
                id = d.flag_id;
                break;
            }
        }

        if (id == -1)
            id = adj[adj.Length - 1].flag_id;

        return id;
    }
}
