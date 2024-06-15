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
        if (!Data.Is(out Insurgency gm) || gm.Caches == null || player.HasUIHidden) return;
        ITransportConnection c = player.Player.channel.owner.transportConnection;
        CTFUI.ListUI.SendToPlayer(c);
        CTFUI.ListUI.Header.SetVisibility(c, true);
        CTFUI.ListUI.Header.SetText(c, T.CachesHeader.Translate(player));
        int i = 0;
        int num = Math.Min(gm.Caches.Count, CTFUI.ListUI.Rows.Length);
        for (; i < num; i++)
        {
            Insurgency.CacheData cache = gm.Caches[i];

            CTFUI.ListUI.Rows[i].Root.SetVisibility(c, true);
            CTFUI.ListUI.Rows[i].Name.SetText(c, GetCacheLabel(cache, player, gm));
        }
        for (; i < CTFUI.ListUI.Rows.Length; i++)
            CTFUI.ListUI.Rows[i].Root.SetVisibility(c, false);
    }
    public static void ReplicateCacheUpdate(Insurgency.CacheData cache)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (!Data.Is(out Insurgency gm)) return;
        int index = gm.Caches.IndexOf(cache);
        if (index < 0 || index >= CTFUI.ListUI.Rows.Length)
            return;
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
        {
            UCPlayer player = PlayerManager.OnlinePlayers[i];
            if (!player.HasUIHidden)
                CTFUI.ListUI.Rows[index].Name.SetText(player.Connection, GetCacheLabel(cache, player, gm));
        }
    }
    public static string GetCacheLabel(Insurgency.CacheData cache, UCPlayer player, Insurgency insurgency)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ulong team = player.GetTeam();

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
                    return T.InsurgencyCacheAttack.Translate(player, false, cache.Cache, cache.Cache);
                }
                else
                {
                    return T.InsurgencyCacheDefense.Translate(player, false, cache.Cache, cache.Cache);
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
                    return T.InsurgencyCacheDefenseUndiscovered.Translate(player, false, cache.Cache, cache.Cache);
                }
            }
        }
    }
}
