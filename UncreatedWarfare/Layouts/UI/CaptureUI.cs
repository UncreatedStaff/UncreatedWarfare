using SDG.NetTransport;
using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Reflection;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Layouts.UI;

[UnturnedUI(BasePath = "Canvas/Circles")]
public class CaptureUI : UnturnedUI
{
    private readonly ITranslationValueFormatter _valueFormatter;

    public readonly UnturnedLabel Background = new UnturnedLabel("BackgroundCircle");
    public readonly UnturnedLabel Foreground = new UnturnedLabel("BackgroundCircle/ForegroundCircle");
    public readonly UnturnedLabel T1CountIcon = new UnturnedLabel("BackgroundCircle/ForegroundCircle/T1CountIcon");
    public readonly UnturnedLabel T1Count = new UnturnedLabel("BackgroundCircle/ForegroundCircle/T1CountIcon/T1Count");
    public readonly UnturnedLabel T2CountIcon = new UnturnedLabel("BackgroundCircle/ForegroundCircle/T2CountIcon");
    public readonly UnturnedLabel T2Count = new UnturnedLabel("BackgroundCircle/ForegroundCircle/T2CountIcon/T2Count");
    public readonly UnturnedLabel Status = new UnturnedLabel("Status");
    public CaptureUI(ITranslationValueFormatter valueFormatter) : base(Gamemode.Config.UICapture.GetId(), reliable: false)
    {
        _valueFormatter = valueFormatter;
    }

    public void Send(UCPlayer player, in CaptureUIParameters p)
    {
        ITransportConnection c = player.Connection;
        if (p.Type == FlagStatus.DontDisplay || player.HasUIHidden)
        {
            ClearFromPlayer(c);
            return;
        }

        GetColors(p.Team, p.Type, out string backcolor, out string forecolor);
        string translation = p.Type is FlagStatus.Blank ? string.Empty : _valueFormatter.FormatEnum(p.Type, player.Locale.LanguageInfo);
        string desc = new string(Gamemode.Config.UICircleFontCharacters[CTFUI.FromMax(p.Points)], 1);
        if (p.Type is not FlagStatus.Blank and not FlagStatus.DontDisplay && Gamemode.Config.UICaptureShowPointCount)
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
    private static void GetColors(ulong team, FlagStatus type, out string backcolor, out string forecolor)
    {
        if (type is FlagStatus.Losing or FlagStatus.Lost)
            team = TeamManager.Other(team);
        const float darkness = 0.3f;
        if (type is FlagStatus.Capturing or FlagStatus.Clearing or FlagStatus.Losing or FlagStatus.Lost)
        {
            forecolor = TeamManager.GetTeamHexColor(team);
            Color tc = TeamManager.GetTeamColor(team);
            backcolor = ColorUtility.ToHtmlStringRGB(new Color(tc.r * darkness, tc.g * darkness, tc.b * darkness, 1f));
        }
        else
        {
            Color c = UCWarfare.GetColor(type switch
            {
                FlagStatus.Contested => "contested",
                FlagStatus.Secured => "secured",
                FlagStatus.Neutralized => "neutral",
                FlagStatus.Locked => "locked",
                FlagStatus.InVehicle => "invehicle",
                _ => "nocap"
            });
            forecolor = ColorUtility.ToHtmlStringRGB(c);
            backcolor = ColorUtility.ToHtmlStringRGB(new Color(c.r * darkness, c.g * darkness, c.b * darkness, 1f));
        }
    }

    public readonly struct CaptureUIParameters
    {
        public readonly ulong Team;
        public readonly FlagStatus Type;
        public readonly Flag? Flag;
        public readonly int Points;

        public CaptureUIParameters(ulong team, FlagStatus type, Flag? flag)
        {
            Team = team;
            Type = type;
            Flag = flag;
            if (flag is not null && type is FlagStatus.Capturing or FlagStatus.Losing or FlagStatus.Contested or FlagStatus.Clearing or FlagStatus.InVehicle)
                Points = Mathf.RoundToInt(flag.Points);
            else
                Points = Mathf.RoundToInt(Flag.MaxPoints);
        }
    }
}

public enum FlagStatus
{
    [Translatable("CAPTURING", Description = "Shown when your team is capturing the flag.")]
    Capturing,

    [Translatable("LOSING", Description = "Shown when your team is losing the flag because the other team has more players.")]
    Losing,

    [Translatable("SECURED", Description = "Shown when your team is holding the flag after it has been captured.")]
    Secured,

    [Translatable("NEUTRALIZED", Description = "Shown when the flag has not been captured by either team.")]
    Neutralized,

    [Translatable("LOST", Description = "Shown when your team lost the flag and you dont have enough people on the flag to clear.")]
    Lost,

    [Translatable("CONTESTED", Description = "Shown when your team and the other team have the same amount of people on the flag.")]
    Contested,

    [Translatable("INEFFECTIVE", Description = "Shown when you're on a flag but it's not the objective.")]
    Ineffective,

    [Translatable("CLEARING", Description = "Shown when your team is capturing a flag still owned by the other team.")]
    Clearing,

    /// <summary>
    /// No text on the UI.
    /// </summary>
    [Translatable("", Description = "Leave blank.", IsPrioritizedTranslation = false)]
    Blank,
    
    /// <summary>
    /// Removes the UI completely.
    /// </summary>
    [Translatable("", Description = "Leave blank.", IsPrioritizedTranslation = false)]
    DontDisplay,

    [Translatable("IN VEHICLE", Description = "Shown when you're trying to capture a flag while in a vehicle.")]
    InVehicle,

    [Translatable("LOCKED", Description = "Shown in Invasion when a flag has already been captured by attackers and can't be recaptured.")]
    Locked
}