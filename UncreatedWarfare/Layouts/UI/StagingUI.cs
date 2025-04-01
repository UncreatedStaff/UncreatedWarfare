using SDG.NetTransport;
using System;
using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Data;
using Uncreated.Framework.UI.Reflection;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Layouts.UI;

[UnturnedUI(BasePath = "Canvas")]
public class StagingUI : UnturnedUI
{
    private readonly Func<CSteamID, StagingUIData> _getStagingUIData;

    public readonly UnturnedLabel Top = new UnturnedLabel("Top");
    public readonly UnturnedLabel Bottom = new UnturnedLabel("Bottom");

    public StagingUI(AssetConfiguration assetConfig, ILoggerFactory loggerFactory) : base(loggerFactory,
        assetConfig.GetAssetLink<EffectAsset>("UI:Header"), staticKey: true)
    {
        _getStagingUIData = GetUIData;
    }
    public void SetText(ITransportConnection connection, string top, string bottom)
    {
        Top.SetText(connection, top);
        Bottom.SetText(connection, bottom);
    }

    private StagingUIData GetOrAddUIData(CSteamID steam64)
    {
        return GetOrAddData(steam64, _getStagingUIData);
    }

    private StagingUIData GetUIData(CSteamID steam64)
    {
        return new StagingUIData(steam64, this);
    }

    /// <summary>
    /// Send the initial UI to <paramref name="player"/> without a timer.
    /// </summary>
    public void SendToPlayer(WarfarePlayer player, TranslationList name, TimeSpan timeLeft)
    {
        if (timeLeft < TimeSpan.Zero)
            timeLeft = default;

        string msg = FormattingUtility.ToCountdownString(timeLeft, withHours: false);
        string translatedName = name.Translate(player.Locale.LanguageInfo, string.Empty);
        SendToPlayer(player.Connection, translatedName, msg);
        GetOrAddUIData(player.Steam64).HasUI = true;
    }

    /// <summary>
    /// Send the initial UI to <paramref name="player"/> without a timer.
    /// </summary>
    public void SendToPlayer(WarfarePlayer player, TranslationList name)
    {
        string translatedName = name.Translate(player.Locale.LanguageInfo, string.Empty);
        SendToPlayer(player.Connection, translatedName, string.Empty);
        GetOrAddUIData(player.Steam64).HasUI = true;
    }

    /// <summary>
    /// Update the timer for <paramref name="player"/>.
    /// </summary>
    public void UpdateForPlayer(WarfarePlayer player, TranslationList name, TimeSpan timeLeft)
    {
        if (timeLeft < TimeSpan.Zero)
            timeLeft = default;

        StagingUIData data = GetOrAddUIData(player.Steam64);
        string msg = FormattingUtility.ToCountdownString(timeLeft, withHours: false);
        if (data.HasUI)
        {
            Bottom.SetText(player.Connection, msg);
        }
        else
        {
            SendToPlayer(player.Connection, name.Translate(player.Locale.LanguageInfo, string.Empty), msg);
            data.HasUI = true;
        }
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
            {
                GetOrAddUIData(set.Next.Steam64).HasUI = true;
                SendToPlayer(set.Next.Connection, translatedName, msg);
            }
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
            {
                GetOrAddUIData(set.Next.Steam64).HasUI = true;
                SendToPlayer(set.Next.Connection, translatedName, string.Empty);
            }
        }
    }

    /// <summary>
    /// Update the timer for all players in <paramref name="playerSets"/>.
    /// </summary>
    public void UpdateForAll(LanguageSetEnumerator playerSets, TranslationList name, TimeSpan timeLeft)
    {
        if (timeLeft < TimeSpan.Zero)
            timeLeft = default;

        string msg = FormattingUtility.ToCountdownString(timeLeft, withHours: false);

        foreach (LanguageSet set in playerSets)
        {
            if (!set.Team.IsValid)
            {
                while (set.MoveNext())
                {
                    StagingUIData data = GetOrAddUIData(set.Next.Steam64);
                    if (!data.HasUI)
                        continue;

                    data.HasUI = false;
                    ClearFromPlayer(set.Next.Connection);
                }
            }
            else
            {
                while (set.MoveNext())
                {
                    StagingUIData data = GetOrAddUIData(set.Next.Steam64);
                    if (data.HasUI)
                    {
                        Bottom.SetText(set.Next, msg);
                    }
                    else
                    {
                        SendToPlayer(set.Next.Connection, name.Translate(set.Language, string.Empty), msg);
                        data.HasUI = true;
                    }
                }
            }
        }
    }

    private class StagingUIData : IUnturnedUIData
    {
        public CSteamID Player { get; }
        public UnturnedUI Owner { get; }
        public bool HasUI { get; set; }
        UnturnedUIElement? IUnturnedUIData.Element => null;

        public StagingUIData(CSteamID player, UnturnedUI owner)
        {
            Player = player;
            Owner = owner;
        }
    }
}