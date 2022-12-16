using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Teams;
using UnityEngine;
using Random = UnityEngine.Random;


namespace Uncreated.Warfare.Gamemodes.Flags;
public partial class Conquest
{
    private void IntlLoadRotation()
    {
        int amt = Config.ConquestPointCount;

        if (FlagRotation is null)
            FlagRotation = new List<Flag>(amt);
        else
            FlagRotation.Clear();

        if (amt < 3 || amt % 2 == 0)
            throw new InvalidOperationException(
                "Must have an odd number more than 2 flags for Conquest. Change the \"" +
                nameof(GamemodeConfigData.ConquestPointCount) + "\" value in Gamemode Config.");

        AdjacentFlagData[] adj1 = TeamManager.Team1Main.Data.Adjacencies;
        Flag? flag;
        int ct = 0;
        do
        {
            int id = ObjectivePathing.PickWeightedAdjacency(adj1);
            flag = AllFlags.FirstOrDefault(x => x.ID == id);
        }
        while (flag is null && ++ct < adj1.Length);

        if (flag is null)
            throw new InvalidOperationException("No valid adjacencies on team 1.");
        --amt;
        FlagRotation.Add(flag);
        AdjacentFlagData[] adj2 = TeamManager.Team2Main.Data.Adjacencies;
        ct = 0;
        do
        {
            int id = ObjectivePathing.PickWeightedAdjacency(adj2);
            flag = AllFlags.FirstOrDefault(x => x.ID == id);
        }
        while (flag is null && ++ct < adj2.Length);

        if (flag is null)
            throw new InvalidOperationException("No valid adjacencies on team 2.");
        --amt;
        Flag? l2Flag = null;
        if (amt > 1)
        {
            Flag[] l2 = AllFlags.OrderBy(x => Vector2.Distance(x.Position2D, TeamManager.Team1Main.Center)).Where(x =>
            {
                if (x == flag || x == FlagRotation[0]) return false;
                for (int i = 0; i < adj1.Length; ++i)
                {
                    ref AdjacentFlagData d = ref adj1[i];
                    if (d.flag_id == x.ID)
                        return false;
                }
                return true;
            }).Take(Math.Max(3, AllFlags.Count / 3)).ToArray();

            if (l2.Length > 0)
            {
                --amt;
                l2Flag = l2[Random.Range(0, l2.Length)];
                FlagRotation.Add(l2Flag);
            }
            l2 = AllFlags.OrderBy(x => Vector2.Distance(x.Position2D, TeamManager.Team2Main.Center)).Where(x =>
            {
                if (x == flag || x == l2Flag || x == FlagRotation[0]) return false;
                for (int i = 0; i < adj2.Length; ++i)
                {
                    ref AdjacentFlagData d = ref adj2[i];
                    if (d.flag_id == x.ID)
                        return false;
                }
                return true;
            }).Take(Math.Max(3, AllFlags.Count / 3)).ToArray();
            if (l2.Length > 0)
            {
                --amt;
                l2Flag = l2[Random.Range(0, l2.Length)];
            }
            else l2Flag = null;
        }

        // gets the flags in the center of the map.
        Flag?[] f = AllFlags.OrderBy(x => Vector2.Distance(x.Position2D, Vector2.zero)).Take(Math.Max(Math.Max(amt, 5), AllFlags.Count / 2)).ToArray();

        if (f.Length <= amt)
        {
            L.LogWarning("Unable to get enough flags for the center group, getting " + f.Length + " instead.");
            amt = f.Length - 1;
        }

        for (int i = 0; i < f.Length; ++i)
        {
            Flag? fl = f[i];
            if (fl == l2Flag || fl == flag || fl == FlagRotation[0] || (FlagRotation.Count > 1 && fl == FlagRotation[1])) fl = null;
            for (int j = 0; j < FlagRotation.Count; ++j)
                if (FlagRotation[j] == fl)
                    goto br;

            continue;
        br: fl = null;
            break;
        }

        for (; amt >= 0; --amt)
        {
            Flag? f2;
            ct = 0;
            do
            {
                int ind = Random.Range(0, f.Length);
                f2 = f[ind];
                L.LogDebug(ind.ToString() + " - " + (f2?.Name ?? "null"));
                f[ind] = null;
            } while (f2 is null && ++ct < f.Length);
            if (f2 is not null)
            {
                FlagRotation.Add(f2);
            }
        }

        if (l2Flag is not null)
        {
            FlagRotation.Add(l2Flag);
        }
        FlagRotation.Add(flag);

        if (FlagRotation.Count % 2 == 0)
            FlagRotation.RemoveAt(FlagRotation.Count / 2);

        for (int i = 0; i < FlagRotation.Count; ++i)
        {
            Flag flag1 = FlagRotation[i];
            flag1.index = i;
            InitFlag(flag1);
        }
    }
}
