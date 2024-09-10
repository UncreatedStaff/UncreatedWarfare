using SDG.NetTransport;
using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Reflection;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes.Flags.UI;

[UnturnedUI(BasePath = "Canvas/Circles")]
public class CaptureUI : UnturnedUI
{
    public readonly UnturnedLabel Background = new UnturnedLabel("BackgroundCircle");
    public readonly UnturnedLabel Foreground = new UnturnedLabel("BackgroundCircle/ForegroundCircle");
    public readonly UnturnedLabel T1CountIcon = new UnturnedLabel("BackgroundCircle/ForegroundCircle/T1CountIcon");
    public readonly UnturnedLabel T1Count = new UnturnedLabel("BackgroundCircle/ForegroundCircle/T1CountIcon/T1Count");
    public readonly UnturnedLabel T2CountIcon = new UnturnedLabel("BackgroundCircle/ForegroundCircle/T2CountIcon");
    public readonly UnturnedLabel T2Count = new UnturnedLabel("BackgroundCircle/ForegroundCircle/T2CountIcon/T2Count");
    public readonly UnturnedLabel Status = new UnturnedLabel("Status");
    public CaptureUI() : base(Gamemode.Config.UICapture.AsAssetContainer(), reliable: false) { }

    public void Send(UCPlayer player, in CaptureUIParameters p)
    {
        ITransportConnection c = player.Connection;
        if (p.Type == EFlagStatus.DONT_DISPLAY || player.HasUIHidden)
        {
            ClearFromPlayer(c);
            return;
        }
        GetColors(p.Team, p.Type, out string backcolor, out string forecolor);
        string translation = p.Type is EFlagStatus.BLANK ? string.Empty : Localization.TranslateEnum(p.Type, player.Locale.LanguageInfo);
        string desc = new string(Gamemode.Config.UICircleFontCharacters[CTFUI.FromMax(p.Points)], 1);
        if (p.Type is not EFlagStatus.BLANK and not EFlagStatus.DONT_DISPLAY && Gamemode.Config.UICaptureShowPointCount)
            translation += " (" + p.Points.ToString(player.Locale.CultureInfo) + "/" + Flag.MaxPoints.ToString(player.Locale.CultureInfo) + ")";

        SendToPlayer(c, "<color=#" + forecolor + ">" + translation + "</color>", "<color=#" + forecolor + ">" + desc + "</color>", backcolor);
        if (Gamemode.Config.UICaptureEnablePlayerCount && p.Flag is not null)
        {
            T1Count.SetText(c, "<color=#ffffff>" + p.Flag.Team1TotalCappers.ToString(player.Locale.CultureInfo) + "</color>");
            T2Count.SetText(c, "<color=#ffffff>" + p.Flag.Team2TotalCappers.ToString(player.Locale.CultureInfo) + "</color>");
            T1CountIcon.SetText(c, "<color=#" + TeamManager.GetTeamHexColor(1) + ">" + Gamemode.Config.UIIconPlayer + "</color>");
            T2CountIcon.SetText(c, "<color=#" + TeamManager.GetTeamHexColor(2) + ">" + Gamemode.Config.UIIconPlayer + "</color>");
        }
        else
        {
            T1CountIcon.SetVisibility(c, false);
            T2CountIcon.SetVisibility(c, false);
        }
    }
    private static void GetColors(ulong team, EFlagStatus type, out string backcolor, out string forecolor)
    {
        if (type is EFlagStatus.LOSING or EFlagStatus.LOST)
            team = TeamManager.Other(team);
        const float darkness = 0.3f;
        if (type is EFlagStatus.CAPTURING or EFlagStatus.CLEARING or EFlagStatus.LOSING or EFlagStatus.LOST)
        {
            forecolor = TeamManager.GetTeamHexColor(team);
            Color tc = TeamManager.GetTeamColor(team);
            backcolor = ColorUtility.ToHtmlStringRGB(new Color(tc.r * darkness, tc.g * darkness, tc.b * darkness, 1f));
        }
        else
        {
            Color c = UCWarfare.GetColor(type switch
            {
                EFlagStatus.CONTESTED => "contested",
                EFlagStatus.SECURED => "secured",
                EFlagStatus.NEUTRALIZED => "neutral",
                EFlagStatus.LOCKED => "locked",
                EFlagStatus.IN_VEHICLE => "invehicle",
                _ => "nocap"
            });
            forecolor = ColorUtility.ToHtmlStringRGB(c);
            backcolor = ColorUtility.ToHtmlStringRGB(new Color(c.r * darkness, c.g * darkness, c.b * darkness, 1f));
        }
    }

    public readonly struct CaptureUIParameters
    {
        public readonly ulong Team;
        public readonly EFlagStatus Type;
        public readonly Flag? Flag;
        public readonly int Points;

        public CaptureUIParameters(ulong team, EFlagStatus type, Flag? flag)
        {
            Team = team;
            Type = type;
            Flag = flag;
            if (flag is not null && type is EFlagStatus.CAPTURING or EFlagStatus.LOSING or EFlagStatus.CONTESTED or EFlagStatus.CLEARING or EFlagStatus.IN_VEHICLE)
                Points = Mathf.RoundToInt(flag.Points);
            else
                Points = Mathf.RoundToInt(Flag.MaxPoints);
        }
    }
}
