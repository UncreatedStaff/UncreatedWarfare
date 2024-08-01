//#define SHOW_LEVEL
//#define SHOW_DIVISION

using SDG.NetTransport;
using System;
using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Reflection;

namespace Uncreated.Warfare.Levels;

[UnturnedUI(BasePath = "Canvas/Image")]
public class XPUI : UnturnedUI
{
    public readonly UnturnedUIElement Parent = new UnturnedUIElement("~/Canvas/Image");
    public readonly UnturnedLabel Rank = new UnturnedLabel("Image/Rank");
    public readonly UnturnedLabel XP = new UnturnedLabel("Image/XP");
    public readonly UnturnedLabel Next = new UnturnedLabel("Image/Next");
    public readonly UnturnedLabel Progress = new UnturnedLabel("Image/Progress");

#if SHOW_LEVEL
    public readonly UnturnedLabel Level = new UnturnedLabel("Image/Level");
#endif

#if SHOW_DIVISION
    public readonly UnturnedLabel Division = new UnturnedLabel("DivisonBkgr/Division");
    public readonly UnturnedUIElement DivisionBackground = new UnturnedUIElement("DivisonBkgr");
#endif

    public XPUI() : base(Gamemode.Config.UIXPPanel.GetId(), reliable: false) { }
    public void SendTo(UCPlayer player)
    {
        ThreadUtil.assertIsGameThread();
        ITransportConnection c = player.Connection;
        L.LogDebug("Sending xp ui to " + player + " (" + Convert.ToString(player.PointsDirtyMask, 2) + ")");

        LevelData data = player.Level;
        SendToPlayer(c,
            data.Abbreviation,
            data.CurrentXP.ToString(player.Locale.CultureInfo) + "/" + data.RequiredXP.ToString(player.Locale.CultureInfo),
            data.NextAbbreviation,
            data.ProgressBar);
        if (player.HasUIHidden || Data.Gamemode.LeaderboardUp())
        {
            Parent.SetVisibility(c, false);
#if SHOW_DIVISION
            DivisionBackground.SetVisibility(c, false);
#endif
            player.PointsDirtyMask |= 0b00100111;
            return;
        }

#if SHOW_LEVEL
        Level.SetVisibility(c, true);
        Level.SetText(c, data.Level > 0 ? "L" + data.Level : string.Empty);
#endif

#if SHOW_DIVISION
        DivisionBackground.SetVisibility(c, true);
        if (player.Branch != Branch.Default)
            Division.SetText(c, T.XPUIDivision.Translate(player, player.Branch));
#endif
        player.PointsDirtyMask &= unchecked((byte)~0b10000111);
    }

    public void Update(UCPlayer player, bool full)
    {
        L.LogDebug("Updating xp ui for " + player + " (" + Convert.ToString(player.PointsDirtyMask, 2) + ")");
        ThreadUtil.assertIsGameThread();
        if (player.HasUIHidden || Data.Gamemode.LeaderboardUp())
            return;
        if ((player.PointsDirtyMask & 0b10000000) == 0)
        {
            SendTo(player);
            return;
        }
        ITransportConnection c = player.Connection;
        LevelData data = player.Level;
        if (full || (player.PointsDirtyMask & 0b00000001) > 0)
        {
            XP.SetText(c, data.CurrentXP.ToString(player.Locale.CultureInfo) + "/" + data.RequiredXP.ToString(player.Locale.CultureInfo));
        }
        if (full || (player.PointsDirtyMask & 0b00000010) > 0)
        {
            Rank.SetText(c, data.Abbreviation);
            Next.SetText(c, data.NextAbbreviation);
#if SHOW_LEVEL
            Level.SetText(c, player.Level.Level > 0 ? "L" + player.Level.Level : string.Empty);
#endif
        }
#if SHOW_DIVISION
        if (full || (player.PointsDirtyMask & 0b00000100) > 0)
        {
            if ((player.PointsDirtyMask & 0b00100000) > 0)
                DivisionBackground.SetVisibility(c, true);
            Division.SetText(c, T.XPUIDivision.Translate(player, player.Branch));
        }
#endif
        player.PointsDirtyMask &= unchecked((byte)~0b00100111);
    }

    public void Clear(UCPlayer player)
    {
        L.LogDebug("Clearing xp ui for " + player);
        player.PointsDirtyMask |= 0b10100000;
        Parent.SetVisibility(player.Connection, false);
#if SHOW_DIVISION
        DivisionBackground.SetVisibility(player.Connection, false);
#endif
    }
}
public class CreditsUI : UnturnedUI
{
    public readonly UnturnedLabel Credits = new UnturnedLabel("Canvas/Image/Credits");
    public readonly UnturnedUIElement Parent = new UnturnedUIElement("Canvas/Image");
    private static string? _creditsColor;
    public string CreditsColor => _creditsColor ??= UCWarfare.GetColorHex("credits");
    public CreditsUI() : base(Gamemode.Config.UICreditsPanel.GetId()) { }
    public void SendTo(UCPlayer player)
    {
        L.LogDebug("Sending creds ui to " + player + " (" + Convert.ToString(player.PointsDirtyMask, 2) + ")");
        ThreadUtil.assertIsGameThread();
        SendToPlayer(player.Connection, GetCreditsString(player));
        player.PointsDirtyMask &= unchecked((byte)~0b01011000);
        if (player.HasUIHidden || Data.Gamemode.LeaderboardUp())
        {
            Parent.SetVisibility(player.Connection, false);
            player.PointsDirtyMask |= 0b00010000;
        }
    }

    public void Update(UCPlayer player, bool full)
    {
        L.LogDebug("Updating creds ui for " + player + " (" + Convert.ToString(player.PointsDirtyMask, 2) + ")");
        ThreadUtil.assertIsGameThread();
        if ((!full && (player.PointsDirtyMask & 0b00001000) == 0) || player.HasUIHidden || Data.Gamemode.LeaderboardUp())
            return;
        if ((player.PointsDirtyMask & 0b10000000) == 0)
        {
            SendTo(player);
            return;
        }
        if ((player.PointsDirtyMask & 0b00010000) > 0)
        {
            Parent.SetVisibility(player.Connection, true);
        }
        Credits.SetText(player.Connection, GetCreditsString(player));
        player.PointsDirtyMask &= unchecked((byte)~0b00011000);
    }

    public void Clear(UCPlayer player)
    {
        L.LogDebug("Clearing creds ui for " + player);
        player.PointsDirtyMask |= 0b01010000;
        Parent.SetVisibility(player.Connection, false);
    }

    private string GetCreditsString(UCPlayer player)
    {
        return "<color=#" + CreditsColor + ">C</color>  " + player.CachedCredits.ToString(player.Locale.CultureInfo);
    }
}