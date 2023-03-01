﻿using System;
using SDG.NetTransport;
using SDG.Unturned;
using Uncreated.Framework.UI;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes.Flags.UI;
public class CaptureUI : UnturnedUI
{
    public readonly UnturnedLabel Background = new UnturnedLabel("BackgroundCircle");
    public readonly UnturnedLabel Foreground = new UnturnedLabel("ForegroundCircle");
    public readonly UnturnedLabel T1CountIcon = new UnturnedLabel("T1CountIcon");
    public readonly UnturnedLabel T1Count = new UnturnedLabel("T1Count");
    public readonly UnturnedLabel T2CountIcon = new UnturnedLabel("T2CountIcon");
    public readonly UnturnedLabel T2Count = new UnturnedLabel("T2Count");
    public readonly UnturnedLabel Status = new UnturnedLabel("Status");
    public CaptureUI() : base(12005, Gamemode.Config.UICapture, true, false) { }

    public void Send(Player player, in CaptureUIParameters p)
    {
        if (p.Type == EFlagStatus.DONT_DISPLAY)
        {
            ClearFromPlayer(player.channel.owner.transportConnection);
            return;
        }
        GetColors(p.Team, p.Type, out string backcolor, out string forecolor);
        string lang = Localization.GetLang(player.channel.owner.playerID.steamID.m_SteamID);
        string translation = p.Type is EFlagStatus.BLANK ? string.Empty : Localization.TranslateEnum(p.Type, lang);
        ITransportConnection c = player.channel.owner.transportConnection;
        string desc = new string(Gamemode.Config.UICircleFontCharacters[CTFUI.FromMax(p.Points)], 1);
        IFormatProvider locale = Localization.GetLocale(lang);
        if (p.Type is not EFlagStatus.BLANK or EFlagStatus.DONT_DISPLAY && Gamemode.Config.UICaptureShowPointCount)
            translation += " (" + p.Points.ToString(locale) + "/" + Flag.MaxPoints.ToString(locale) + ")";

        SendToPlayer(c, "<color=#" + forecolor + ">" + translation + "</color>", "<color=#" + forecolor + ">" + desc + "</color>", backcolor);
        if (Gamemode.Config.UICaptureEnablePlayerCount && p.Flag is not null)
        {
            T1Count.SetText(c, "<color=#ffffff>" + p.Flag.Team1TotalCappers.ToString(locale) + "</color>");
            T2Count.SetText(c, "<color=#ffffff>" + p.Flag.Team2TotalCappers.ToString(locale) + "</color>");
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
