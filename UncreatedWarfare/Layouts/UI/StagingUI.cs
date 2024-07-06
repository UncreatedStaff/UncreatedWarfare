using SDG.NetTransport;
using System;
using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Reflection;
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

        string msg = FormattingUtility.GetTimerString(player.Locale.CultureInfo, timeLeft);
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

        string msg = FormattingUtility.GetTimerString(player.Locale.CultureInfo, timeLeft);
        Bottom.SetText(player.Connection, msg);
    }

    /// <summary>
    /// Send the initial UI to all online players.
    /// </summary>
    public void SendToAll(TranslationList name, TimeSpan timeLeft) => SendToAll(LanguageSet.All(), name, timeLeft);

    /// <summary>
    /// Send the initial UI to all online players without a timer.
    /// </summary>
    public void SendToAll(TranslationList name) => SendToAll(LanguageSet.All(), name);

    /// <summary>
    /// Send the initial UI to all players in <paramref name="languageSet"/>.
    /// </summary>
    public void SendToAll(LanguageSet.LanguageSetEnumerator languageSet, TranslationList name, TimeSpan timeLeft)
    {
        if (timeLeft < TimeSpan.Zero)
            timeLeft = default;

        foreach (LanguageSet set in languageSet)
        {
            string msg = FormattingUtility.GetTimerString(set.CultureInfo, timeLeft);
            string translatedName = name.Translate(set.Language, string.Empty);
            while (set.MoveNext())
                SendToPlayer(set.Next.Connection, translatedName, msg);
        }
    }

    /// <summary>
    /// Send the initial UI to all players in <paramref name="languageSet"/> without a timer.
    /// </summary>
    public void SendToAll(LanguageSet.LanguageSetEnumerator languageSet, TranslationList name)
    {
        foreach (LanguageSet set in languageSet)
        {
            string translatedName = name.Translate(set.Language, string.Empty);
            while (set.MoveNext())
                SendToPlayer(set.Next.Connection, translatedName, string.Empty);
        }
    }

    /// <summary>
    /// Update the timer for all online players.
    /// </summary>
    public void UpdateForAll(TimeSpan timeLeft) => UpdateForAll(LanguageSet.All(), timeLeft);

    /// <summary>
    /// Update the timer for all players in <paramref name="languageSet"/>.
    /// </summary>
    public void UpdateForAll(LanguageSet.LanguageSetEnumerator languageSet, TimeSpan timeLeft)
    {
        if (timeLeft < TimeSpan.Zero)
            timeLeft = default;
        foreach (LanguageSet set in languageSet)
        {
            string msg = FormattingUtility.GetTimerString(set.CultureInfo, timeLeft);
            while (set.MoveNext())
                Bottom.SetText(set.Next, msg);
        }
    }
}
