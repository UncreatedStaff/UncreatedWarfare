using System;
using System.Collections.Generic;
using Uncreated.Players;
using Uncreated.Warfare.Singletons;

namespace Uncreated.Warfare;

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
    public static void TryGiveTip(UCPlayer player, ETip type, params object[] translationArgs)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        _singleton.AssertLoaded();
        Tip tip = _singleton._tips.Find(t => t.Steam64 == player.Steam64 && t.Type == type);
        if (tip is null)
        {
            tip = new Tip(player.Steam64, type, translationArgs);
            if (tip.Cooldown > 0)
                _singleton._tips.Add(tip);
            GiveTip(player, tip);
        }
        else if ((DateTime.Now - tip.LastSent).TotalSeconds > tip.Cooldown)
        {
            tip.LastSent = DateTime.Now;
            tip.TranslationArgs = translationArgs;
            GiveTip(player, tip);
        }
    }
    internal static void OnPlayerDisconnected(ulong pid)
    {
        if (!_singleton.IsLoaded()) return;
        _singleton._tips.RemoveAll(x => x.Steam64 == pid);
    }
    private static void GiveTip(UCPlayer player, Tip tip)
    {
        ToastMessage.QueueMessage(player,
            new ToastMessage(
                tip.TranslationKey.TranslateUnsafe(
                    Localization.GetLang(player.Steam64), tip.TranslationArgs, player, player.GetTeam()),
            EToastMessageSeverity.TIP));

    }
}
public class Tip
{
    public readonly ulong Steam64;
    public readonly ETip Type;
    public DateTime LastSent;
    public readonly float Cooldown;
    public readonly Translation TranslationKey;
    public object[] TranslationArgs;

    public Tip(ulong steam64, ETip type, params object[] translationArgs)
    {
        Steam64 = steam64;
        Type = type;
        LastSent = DateTime.Now;
        TranslationArgs = translationArgs;
        switch (Type)
        {
            case ETip.UAV_REQUEST: Cooldown = 0; TranslationKey = T.TipUAVRequest; break;
            case ETip.PLACE_RADIO: Cooldown = 300; TranslationKey = T.TipPlaceRadio; break;
            case ETip.PLACE_BUNKER: Cooldown = 3; TranslationKey = T.TipPlaceBunker; break;
            case ETip.UNLOAD_SUPPLIES: Cooldown = 120; TranslationKey = T.TipUnloadSupplies; break;
            case ETip.HELP_BUILD: Cooldown = 120; TranslationKey = T.TipHelpBuild; break;
            case ETip.LOGI_RESUPPLIED: Cooldown = 120; TranslationKey = T.TipLogisticsVehicleResupplied; break;
        }
    }
}
public enum ETip
{
    PLACE_RADIO,
    PLACE_BUNKER,
    UNLOAD_SUPPLIES,
    HELP_BUILD,
    LOGI_RESUPPLIED,
    UAV_REQUEST
}
