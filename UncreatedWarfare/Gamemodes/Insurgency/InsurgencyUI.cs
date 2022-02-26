using SDG.NetTransport;
using SDG.Unturned;
using System;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;

namespace Uncreated.Warfare.Gamemodes.Insurgency
{
    public static class InsurgencyUI
    {
        public static void SendCacheList(UCPlayer player)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (!Data.Is(out Insurgency gm)) return;
            ITransportConnection c = player.Player.channel.owner.transportConnection;
            EffectManager.sendUIEffect(CTFUI.flagListID, CTFUI.flagListKey, c, true);
            EffectManager.sendUIEffectVisibility(CTFUI.flagListKey, c, true, "Header", true);
            EffectManager.sendUIEffectText(CTFUI.flagListKey, c, true, "Header", Translation.Translate("caches_header", player));
            int i = 0;
            for (; i < gm.Caches.Count; i++)
            {
                string i2 = i.ToString();
                Insurgency.CacheData cache = gm.Caches[i];
                ulong team = player.GetTeam();

                EffectManager.sendUIEffectVisibility(CTFUI.flagListKey, c, true, i2, true);
                EffectManager.sendUIEffectText(CTFUI.flagListKey, c, true, "N" + i2, GetCacheLabel(cache, player, team, gm));
            }
            for (; i < Gamemode.Config.UI.FlagUICount; i++)
                EffectManager.sendUIEffectVisibility(CTFUI.flagListKey, c, true, i.ToString(), false);
        }
        public static void ReplicateCacheUpdate(Insurgency.CacheData cache)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (!Data.Is(out Insurgency gm)) return;
            int index = gm.Caches.IndexOf(cache);
            string i2 = "N" + index.ToString();
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            {
                UCPlayer player = PlayerManager.OnlinePlayers[i];
                EffectManager.sendUIEffectText(CTFUI.flagListKey, player.Player.channel.owner.transportConnection, true, i2, GetCacheLabel(cache, player, player.GetTeam(), gm));
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
                    return Translation.Translate("insurgency_ui_unknown_attack", player);
                }
                else
                {
                    return Translation.Translate("insurgency_ui_unknown_defense", player);
                }
            }
            else if (cache.IsDestroyed)
            {
                if (team == insurgency.AttackingTeam)
                {
                    return Translation.Translate("insurgency_ui_destroyed_attack", player);
                }
                else
                {
                    return Translation.Translate("insurgency_ui_destroyed_defense", player);
                }
            }
            else
            {
                if (cache.IsDiscovered)
                {
                    if (team == insurgency.AttackingTeam)
                    {
                        return Translation.Translate("insurgency_ui_cache_attack", player, cache.Cache.Name, cache.Cache.ClosestLocation);
                    }
                    else
                    {
                        return Translation.Translate("insurgency_ui_cache_defense_discovered", player, cache.Cache.Name, cache.Cache.ClosestLocation);
                    }
                }
                else
                {
                    if (team == insurgency.AttackingTeam)
                    {
                        return Translation.Translate("insurgency_ui_unknown_attack", player);
                    }
                    else
                    {
                        return Translation.Translate("insurgency_ui_cache_defense_undiscovered", player, cache.Cache.Name, cache.Cache.ClosestLocation);
                    }
                }
            }
        }
    }
}
