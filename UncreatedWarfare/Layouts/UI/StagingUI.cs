using SDG.NetTransport;
using System;
using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Reflection;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Layouts.UI;

[UnturnedUI(BasePath = "Canvas")]
public class StagingUI : UnturnedUI
{
    public readonly UnturnedLabel Top = new UnturnedLabel("Top");
    public readonly UnturnedLabel Bottom = new UnturnedLabel("Bottom");
    public StagingUI() : base(GamemodeOld.Config.UIHeader) { }
    public void SetText(ITransportConnection connection, string top, string bottom)
    {
        Top.SetText(connection, top);
        Bottom.SetText(connection, bottom);
    }

    /// <summary>
    /// Send the initial UI to <paramref name="player"/> without a timer.
    /// </summary>
    public void SendToPlayer(UCPlayer player, TranslationList name, TimeSpan timeLeft)
    {
        if (timeLeft < TimeSpan.Zero)
            timeLeft = default;

        string msg = FormattingUtility.ToCountdownString(timeLeft, withHours: false);
        string translatedName = name.Translate(player.Locale.LanguageInfo, string.Empty);
        SendToPlayer(player.Connection, translatedName, msg);
    }

    /// <summary>
    /// Send the initial UI to <paramref name="player"/> without a timer.
    /// </summary>
    public void SendToPlayer(UCPlayer player, TranslationList name)
    {
        string translatedName = name.Translate(player.Locale.LanguageInfo, string.Empty);
        SendToPlayer(player.Connection, translatedName, string.Empty);
    }

    /// <summary>
    /// Update the timer for <paramref name="player"/>.
    /// </summary>
    public void UpdateForPlayer(UCPlayer player, TimeSpan timeLeft)
    {
        if (timeLeft < TimeSpan.Zero)
            timeLeft = default;

        string msg = FormattingUtility.ToCountdownString(timeLeft, withHours: false);
        Bottom.SetText(player.Connection, msg);
    }

    /// <summary>
    /// Send the initial UI to all players in <paramref name="playerSets"/>.
    /// </summary>
    public void SendToAll(LanguageSetEnumerator playerSets, TranslationList name, TimeSpan timeLeft)
    {
        if (timeLeft < TimeSpan.Zero)
            timeLeft = default;

        foreach (LanguageSet set in playerSets)
        {
            string msg = FormattingUtility.ToCountdownString(timeLeft, withHours: false);
            string translatedName = name.Translate(set.Language, string.Empty);
            while (set.MoveNext())
                SendToPlayer(set.Next.Connection, translatedName, msg);
        }
    }

    /// <summary>
    /// Send the initial UI to all players in <paramref name="playerSets"/> without a timer.
    /// </summary>
    public void SendToAll(LanguageSetEnumerator playerSets, TranslationList name)
    {
        foreach (LanguageSet set in playerSets)
        {
            string translatedName = name.Translate(set.Language, string.Empty);
            while (set.MoveNext())
                SendToPlayer(set.Next.Connection, translatedName, string.Empty);
        }
    }

    /// <summary>
    /// Update the timer for all players in <paramref name="playerSets"/>.
    /// </summary>
    public void UpdateForAll(LanguageSetEnumerator playerSets, TimeSpan timeLeft)
    {
        if (timeLeft < TimeSpan.Zero)
            timeLeft = default;
        foreach (LanguageSet set in playerSets)
        {
            string msg = FormattingUtility.ToCountdownString(timeLeft, withHours: false);
            while (set.MoveNext())
                Bottom.SetText(set.Next, msg);
        }
    }
}