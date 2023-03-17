using System;
using System.Collections.Generic;
using Uncreated.Players;
using Uncreated.Warfare.Singletons;
using UnityEngine;

namespace Uncreated.Warfare.Players;

public class Tips : BaseSingleton
{
    private List<Tip> _tips;
    private static Tips _singleton;
    public override void Load()
    {
        _singleton = this;
        _tips = new List<Tip>(64);
    }
    public override void Unload()
    {
        _singleton = null!;
        _tips.Clear();
        _tips = null!;
    }

    public static void TryGiveTip(UCPlayer player, int cooldown, Translation translation)
        => TryGiveTip(player, cooldown, translation, Localization.Translate(translation, player));
    public static void TryGiveTip<T>(UCPlayer player, int cooldown, Translation<T> translation, T arg)
        => TryGiveTip(player, cooldown, translation, Localization.Translate(translation, player, arg));
    public static void TryGiveTip<T1, T2>(UCPlayer player, int cooldown, Translation<T1, T2> translation, T1 arg1, T2 arg2)
        => TryGiveTip(player, cooldown, translation, Localization.Translate(translation, player, arg1, arg2));
    public static void TryGiveTip<T1, T2, T3>(UCPlayer player, int cooldown, Translation<T1, T2, T3> translation, T1 arg1, T2 arg2, T3 arg3)
        => TryGiveTip(player, cooldown, translation, Localization.Translate(translation, player, arg1, arg2, arg3));
    public static void TryGiveTip<T1, T2, T3, T4>(UCPlayer player, int cooldown, Translation<T1, T2, T3, T4> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        => TryGiveTip(player, cooldown, translation, Localization.Translate(translation, player, arg1, arg2, arg3, arg4));
    public static void TryGiveTip<T1, T2, T3, T4, T5>(UCPlayer player, int cooldown, Translation<T1, T2, T3, T4, T5> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        => TryGiveTip(player, cooldown, translation, Localization.Translate(translation, player, arg1, arg2, arg3, arg4, arg5));
    public static void TryGiveTip<T1, T2, T3, T4, T5, T6>(UCPlayer player, int cooldown, Translation<T1, T2, T3, T4, T5, T6> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        => TryGiveTip(player, cooldown, translation, Localization.Translate(translation, player, arg1, arg2, arg3, arg4, arg5, arg6));
    public static void TryGiveTip<T1, T2, T3, T4, T5, T6, T7>(UCPlayer player, int cooldown, Translation<T1, T2, T3, T4, T5, T6, T7> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        => TryGiveTip(player, cooldown, translation, Localization.Translate(translation, player, arg1, arg2, arg3, arg4, arg5, arg6, arg7));
    public static void TryGiveTip<T1, T2, T3, T4, T5, T6, T7, T8>(UCPlayer player, int cooldown, Translation<T1, T2, T3, T4, T5, T6, T7, T8> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        => TryGiveTip(player, cooldown, translation, Localization.Translate(translation, player, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8));
    public static void TryGiveTip<T1, T2, T3, T4, T5, T6, T7, T8, T9>(UCPlayer player, int cooldown, Translation<T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
        => TryGiveTip(player, cooldown, translation, Localization.Translate(translation, player, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9));
    public static void TryGiveTip<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(UCPlayer player, int cooldown, Translation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10)
        => TryGiveTip(player, cooldown, translation, Localization.Translate(translation, player, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10));
    private static void TryGiveTip(UCPlayer player, int cooldown, Translation translation, string text)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        _singleton.AssertLoaded();
        Tip tip = _singleton._tips.Find(t => t.Steam64 == player.Steam64 && t.Translation == translation);
        if (tip is null)
        {
            tip = new Tip(player.Steam64, cooldown, translation);
            _singleton._tips.Add(tip);
            GiveTip(player, text);
        }
        else if ((DateTime.UtcNow - tip.LastSent).TotalSeconds > tip.Cooldown)
        {
            tip.LastSent = DateTime.UtcNow;
            GiveTip(player, text);
        }
    }
    internal static void OnPlayerDisconnected(ulong pid)
    {
        if (!_singleton.IsLoaded()) return;
        _singleton._tips.RemoveAll(x => x.Steam64 == pid);
    }
    private static void GiveTip(UCPlayer player, string translation)
    {
        ToastMessage.QueueMessage(player, new ToastMessage(translation, ToastMessageSeverity.Tip, true));
    }
}
public class Tip
{
    public readonly ulong Steam64;
    public DateTime LastSent;
    public readonly float Cooldown;
    public readonly Translation Translation;

    public Tip(ulong steam64, float cooldown, Translation translation)
    {
        Steam64 = steam64;
        LastSent = DateTime.UtcNow;
        Translation = translation;
        Cooldown = cooldown;
    }
}
