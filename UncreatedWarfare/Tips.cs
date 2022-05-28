using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    public static void TryGiveTip(UCPlayer player, ETip type, params string[] translationArgs)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        _singleton.AssertLoaded();
        Tip tip = _singleton._tips.Find(t => t.Steam64 == player.Steam64 && t.Type == type);
        if (tip is null)
        {
            tip = new Tip(player.Steam64, type, translationArgs);
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
        ToastMessage.QueueMessage(player, new ToastMessage(Translation.Translate(tip.TranslationKey, player, tip.TranslationArgs), EToastMessageSeverity.TIP));
    }
}
public class Tip
{
    public readonly ulong Steam64;
    public readonly ETip Type;
    public DateTime LastSent;
    public readonly float Cooldown;
    public readonly string TranslationKey;
    public string[] TranslationArgs;

    public Tip(ulong steam64, ETip type, params string[] translationArgs)
    {
        Steam64 = steam64;
        Type = type;
        LastSent = DateTime.Now;
        TranslationArgs = translationArgs;
        switch (Type)
        {
            case ETip.PLACE_RADIO: Cooldown = 300; TranslationKey = "tip_place_radio";  break;
            case ETip.PLACE_BUNKER: Cooldown = 3; TranslationKey = "tip_place_bunker";  break;
            case ETip.UNLOAD_SUPPLIES: Cooldown = 120; TranslationKey = "tip_unload_supplies";  break;
            case ETip.HELP_BUILD: Cooldown = 120; TranslationKey = "tip_help_build";  break;
            case ETip.LOGI_RESUPPLIED: Cooldown = 120; TranslationKey = "tip_logi_resupplied";  break;
        }
    }
}
public enum ETip
{
    PLACE_RADIO,
    PLACE_BUNKER,
    UNLOAD_SUPPLIES,
    HELP_BUILD,
    LOGI_RESUPPLIED
}
