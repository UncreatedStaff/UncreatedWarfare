using SDG.NetTransport;
using System;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;

namespace Uncreated.Warfare.Gamemodes.Insurgency;

public static class InsurgencyUI
{
    public static void SendCacheList(UCPlayer player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (!Data.Is(out Insurgency gm) || gm.Caches == null) return;
        ITransportConnection c = player.Player.channel.owner.transportConnection;
        CTFUI.ListUI.SendToPlayer(c);
        CTFUI.ListUI.Header.SetVisibility(c, true);
        CTFUI.ListUI.Header.SetText(c, T.CachesHeader.Translate(player));
        int i = 0;
        int num = Math.Min(gm.Caches.Count, CTFUI.ListUI.Parents.Length);
        for (; i < num; i++)
        {
            Insurgency.CacheData cache = gm.Caches[i];
            ulong team = player.GetTeam();

            CTFUI.ListUI.Parents[i].SetVisibility(c, true);
            CTFUI.ListUI.Names[i].SetText(c, GetCacheLabel(cache, player, team, gm));
        }
        for (; i < CTFUI.ListUI.Parents.Length; i++)
            CTFUI.ListUI.Parents[i].SetVisibility(c, false);
    }
    public static void ReplicateCacheUpdate(Insurgency.CacheData cache)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (!Data.Is(out Insurgency gm)) return;
        int index = gm.Caches.IndexOf(cache);
        if (index < 0 || index >= CTFUI.ListUI.Parents.Length)
            return;
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
        {
            UCPlayer player = PlayerManager.OnlinePlayers[i];
            CTFUI.ListUI.Names[index].SetText(player.Connection, GetCacheLabel(cache, player, player.GetTeam(), gm));
        }
    }
    public static string GetCacheLabel(Insurgency.CacheData cache, UCPlayer player, ulong team, Insurgency insurgency)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (!cache.IsActive)
        {
            if (team == insurgency.AttackingTeam)
            {
                return T.InsurgencyUnknownCacheAttack.Translate(player);
            }
            else
            {
                return T.InsurgencyUnknownCacheDefense.Translate(player);
            }
        }
        else if (cache.IsDestroyed)
        {
            if (team == insurgency.AttackingTeam)
            {
                return T.InsurgencyDestroyedCacheAttack.Translate(player);
            }
            else
            {
                return T.InsurgencyDestroyedCacheDefense.Translate(player);
            }
        }
        else
        {
            if (cache.IsDiscovered)
            {
                if (team == insurgency.AttackingTeam)
                {
                    return T.InsurgencyCacheAttack.Translate(player, cache.Cache, cache.Cache);
                }
                else
                {
                    return T.InsurgencyCacheDefense.Translate(player, cache.Cache, cache.Cache);
                }
            }
            else
            {
                if (team == insurgency.AttackingTeam)
                {
                    return T.InsurgencyUnknownCacheAttack.Translate(player);
                }
                else
                {
                    return T.InsurgencyUnknownCacheDefense.Translate(player);
                }
            }
        }
    }
}
