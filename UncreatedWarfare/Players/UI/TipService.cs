using System;
using System.Collections.Generic;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Players.UI;

public class TipService : ILayoutHostedService, IEventListener<PlayerLeft>
{
    private readonly List<Tip> _tips = new List<Tip>(64);

    public TipTranslations Translations { get; }

    public TipService(TranslationInjection<TipTranslations> translations)
    {
        Translations = translations.Value;
    }

    UniTask ILayoutHostedService.StartAsync(CancellationToken token)
    {
        return UniTask.CompletedTask;
    }

    UniTask ILayoutHostedService.StopAsync(CancellationToken token)
    {
        _tips.Clear();
        return UniTask.CompletedTask;
    }

    public void TryGiveTip(WarfarePlayer player, int cooldown, Translation translation)
        => TryGiveTip(player, cooldown, translation, translation.Translate(player));
    public void TryGiveTip<T>(WarfarePlayer player, int cooldown, Translation<T> translation, T arg)
        => TryGiveTip(player, cooldown, translation, translation.Translate(arg, player));
    public void TryGiveTip<T1, T2>(WarfarePlayer player, int cooldown, Translation<T1, T2> translation, T1 arg1, T2 arg2)
        => TryGiveTip(player, cooldown, translation, translation.Translate(arg1, arg2, player));
    public void TryGiveTip<T1, T2, T3>(WarfarePlayer player, int cooldown, Translation<T1, T2, T3> translation, T1 arg1, T2 arg2, T3 arg3)
        => TryGiveTip(player, cooldown, translation, translation.Translate(arg1, arg2, arg3, player));
    public void TryGiveTip<T1, T2, T3, T4>(WarfarePlayer player, int cooldown, Translation<T1, T2, T3, T4> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        => TryGiveTip(player, cooldown, translation, translation.Translate(arg1, arg2, arg3, arg4, player));
    public void TryGiveTip<T1, T2, T3, T4, T5>(WarfarePlayer player, int cooldown, Translation<T1, T2, T3, T4, T5> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        => TryGiveTip(player, cooldown, translation, translation.Translate(arg1, arg2, arg3, arg4, arg5, player));
    public void TryGiveTip<T1, T2, T3, T4, T5, T6>(WarfarePlayer player, int cooldown, Translation<T1, T2, T3, T4, T5, T6> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        => TryGiveTip(player, cooldown, translation, translation.Translate(arg1, arg2, arg3, arg4, arg5, arg6, player));
    public void TryGiveTip<T1, T2, T3, T4, T5, T6, T7>(WarfarePlayer player, int cooldown, Translation<T1, T2, T3, T4, T5, T6, T7> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        => TryGiveTip(player, cooldown, translation, translation.Translate(arg1, arg2, arg3, arg4, arg5, arg6, arg7, player));
    public void TryGiveTip<T1, T2, T3, T4, T5, T6, T7, T8>(WarfarePlayer player, int cooldown, Translation<T1, T2, T3, T4, T5, T6, T7, T8> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        => TryGiveTip(player, cooldown, translation, translation.Translate(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, player));
    public void TryGiveTip<T1, T2, T3, T4, T5, T6, T7, T8, T9>(WarfarePlayer player, int cooldown, Translation<T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
        => TryGiveTip(player, cooldown, translation, translation.Translate(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, player));
    public void TryGiveTip<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(WarfarePlayer player, int cooldown, Translation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10)
        => TryGiveTip(player, cooldown, translation, translation.Translate(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, player));
    private void TryGiveTip(WarfarePlayer player, int cooldown, Translation translation, string text)
    {
        Tip tip = _tips.Find(t => t.Steam64 == player.Steam64.m_SteamID && t.Translation == translation);
        if (tip is null)
        {
            tip = new Tip(player.Steam64.m_SteamID, cooldown, translation);
            _tips.Add(tip);
            GiveTip(player, text);
        }
        else if ((DateTime.UtcNow - tip.LastSent).TotalSeconds > tip.Cooldown)
        {
            tip.LastSent = DateTime.UtcNow;
            GiveTip(player, text);
        }
    }

    void IEventListener<PlayerLeft>.HandleEvent(PlayerLeft e, IServiceProvider serviceProvider)
    {
        _tips.RemoveAll(x => x.Steam64 == e.Steam64.m_SteamID);
    }

    private static void GiveTip(WarfarePlayer player, string translation)
    {
        player.SendToast(new ToastMessage(ToastMessageStyle.Tip, translation) { Resend = ToastManager.PluginKeyMatch.IsMatch(translation) });
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
