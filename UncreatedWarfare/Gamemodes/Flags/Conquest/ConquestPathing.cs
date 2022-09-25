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

        if (_rotation is null)
            _rotation = new List<Flag>(amt);
        else
            _rotation.Clear();

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
            flag = _allFlags.FirstOrDefault(x => x.ID == id);
        }
        while (flag is null && ++ct < adj1.Length);

        if (flag is null)
            throw new InvalidOperationException("No valid adjacencies on team 1.");
        --amt;
        _rotation.Add(flag);
        L.LogDebug("Adding " + flag.Name + " (t1)");
        AdjacentFlagData[] adj2 = TeamManager.Team2Main.Data.Adjacencies;
        ct = 0;
        do
        {
            int id = ObjectivePathing.PickWeightedAdjacency(adj2);
            flag = _allFlags.FirstOrDefault(x => x.ID == id);
        }
        while (flag is null && ++ct < adj2.Length);

        if (flag is null)
            throw new InvalidOperationException("No valid adjacencies on team 2.");
        --amt;
        Flag? l2Flag = null;
        if (amt > 1)
        {
            Flag[] l2 = _allFlags.OrderBy(x => Vector2.Distance(x.Position2D, TeamManager.Team1Main.Center)).Where(x =>
            {
                if (x == flag || x == _rotation[0]) return false;
                for (int i = 0; i < adj1.Length; ++i)
                {
                    ref AdjacentFlagData d = ref adj1[i];
                    if (d.flag_id == x.ID)
                        return false;
                }
                return true;
            }).Take(Math.Max(3, _allFlags.Count / 3)).ToArray();

            if (l2.Length > 0)
            {
                --amt;
                l2Flag = l2[Random.Range(0, l2.Length)];
                _rotation.Add(l2Flag);
                L.LogDebug("Adding " + l2Flag.Name + " (t1l2)");
            }
            l2 = _allFlags.OrderBy(x => Vector2.Distance(x.Position2D, TeamManager.Team2Main.Center)).Where(x =>
            {
                if (x == flag || x == l2Flag || x == _rotation[0]) return false;
                for (int i = 0; i < adj2.Length; ++i)
                {
                    ref AdjacentFlagData d = ref adj2[i];
                    if (d.flag_id == x.ID)
                        return false;
                }
                return true;
            }).Take(Math.Max(3, _allFlags.Count / 3)).ToArray();
            if (l2.Length > 0)
            {
                --amt;
                l2Flag = l2[Random.Range(0, l2.Length)];
            }
            else l2Flag = null;
        }

        // gets the flags in the center of the map.
        Flag?[] f = _allFlags.OrderBy(x => Vector2.Distance(x.Position2D, Vector2.zero)).Take(Math.Max(Math.Max(amt, 5), _allFlags.Count / 2)).ToArray();

        if (f.Length <= amt)
        {
            L.LogWarning("Unable to get enough flags for the center group, getting " + f.Length + " instead.");
            amt = f.Length - 1;
        }

        for (int i = 0; i < f.Length; ++i)
        {
            Flag? fl = f[i];
            if (fl == l2Flag || fl == flag || fl == _rotation[0] || (_rotation.Count > 1 && fl == _rotation[1])) fl = null;
            for (int j = 0; j < _rotation.Count; ++j)
                if (_rotation[j] == fl)
                    goto br;

            continue;
        br: fl = null;
            break;
        }

        for (int i = 0; i < f.Length; ++i)
            L.LogDebug("#" + i + " " + (f[i]?.Name ?? "null"));

        L.LogDebug(amt.ToString() + " (" + f.Length + ")");
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
                L.LogDebug("Adding " + f2.Name + " (mid)");
                _rotation.Add(f2);
            }
        }

        if (l2Flag is not null)
        {
            L.LogDebug("Adding " + l2Flag.Name + " (t2l2)");
            _rotation.Add(l2Flag);
        }
        L.LogDebug("Adding " + flag.Name + " (t2)");
        _rotation.Add(flag);

        if (_rotation.Count % 2 == 0)
            _rotation.RemoveAt(_rotation.Count / 2);

        for (int i = 0; i < _rotation.Count; ++i)
        {
            Flag flag1 = _rotation[i];
            flag1.index = i;
            InitFlag(flag1);
        }
    }
}
