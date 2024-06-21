using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Util;
using UnityEngine;
using Random = UnityEngine.Random;


namespace Uncreated.Warfare.Gamemodes.Flags;
public partial class Conquest
{
    private void IntlLoadRotation()
    {
        int amt;

        if (PlayerManager.OnlinePlayers.Count <= 16)
            amt = Config.ConquestPointCountLowPop;
        else if (PlayerManager.OnlinePlayers.Count <= 32)
            amt = Config.ConquestPointCountMediumPop;
        else
            amt = Config.ConquestPointCountHighPop;

        int origAmt = amt;
        if (amt % 2 == 0)
            ++amt;
        if (FlagRotation is null)
            FlagRotation = new List<Flag>(amt);
        else
            FlagRotation.Clear();

        if (amt < 3)
            throw new InvalidOperationException(
                "Must have more than 2 flags for Conquest. Change the \"" +
                nameof(GamemodeConfigData.ConquestPointCountLowPop) + "\" value in Gamemode Config.");

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
            // picks 2 flags close to the mains that aren't adjacent to them
            Flag[] l2 = AllFlags.OrderBy(x => Vector2.SqrMagnitude(x.Position2D - TeamManager.Team1Main.Center)).Where(x =>
            {
                if (x == flag || x == FlagRotation[0]) return false;
                for (int i = 0; i < adj1.Length; ++i)
                {
                    ref AdjacentFlagData d = ref adj1[i];
                    if (d.PrimaryKey.Key == x.ID)
                        return false;
                }
                return true;
            }).Take(Math.Max(5, AllFlags.Count / 3)).ToArray();

            if (l2.Length > 0)
            {
                --amt;
                l2Flag = l2[RandomUtility.GetIndex((ICollection)l2)];
                FlagRotation.Add(l2Flag);
            }
            l2 = AllFlags.OrderBy(x => Vector2.SqrMagnitude(x.Position2D - TeamManager.Team2Main.Center)).Where(x =>
            {
                if (x == flag || x == l2Flag || x == FlagRotation[0]) return false;
                for (int i = 0; i < adj2.Length; ++i)
                {
                    ref AdjacentFlagData d = ref adj2[i];
                    if (d.PrimaryKey.Key == x.ID)
                        return false;
                }
                return true;
            }).Take(Math.Max(5, AllFlags.Count / 3)).ToArray();
            if (l2.Length > 0)
            {
                --amt;
                l2Flag = l2[RandomUtility.GetIndex((ICollection)l2)];
            }
            else l2Flag = null;
        }

        // gets the flags in the center of the map.
        Flag?[] f = AllFlags.OrderBy(x => Vector2.SqrMagnitude(x.Position2D - Vector2.zero)).Take(Math.Max(Math.Max(amt, 5), AllFlags.Count / 2)).ToArray();

        if (f.Length <= amt)
        {
            L.LogWarning("Unable to get enough flags (" + amt + ") for the center group, getting " + f.Length + " instead.");
            amt = f.Length;
        }

        for (int i = 0; i < f.Length; ++i)
        {
            ref Flag? fl = ref f[i];
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
                int ind = RandomUtility.GetIndex((ICollection)f);
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
            FlagRotation.Add(l2Flag);
        
        FlagRotation.Add(flag);

        while (origAmt < FlagRotation.Count)
            FlagRotation.RemoveAt(FlagRotation.Count / 2);

        for (int i = 0; i < FlagRotation.Count; ++i)
        {
            Flag flag1 = FlagRotation[i];
            flag1.Index = i;
            InitFlag(flag1);
        }
    }
}
