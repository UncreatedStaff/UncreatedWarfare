using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Gamemodes.Flags.UI;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Teams;
using UnityEngine;
using static Uncreated.Warfare.Gamemodes.Flags.UI.CaptureUI;

namespace Uncreated.Warfare.Gamemodes.Flags.TeamCTF;

public static class CTFUI
{
    public static readonly CaptureUI CaptureUI = new CaptureUI();
    public static readonly FlagListUI ListUI = new FlagListUI();
    public static readonly StagingUI StagingUI = new StagingUI();
    public static int FromMax(int cap) => Math.Abs(cap) >= Mathf.RoundToInt(Flag.MAX_POINTS) ? Gamemode.Config.UI.ProgressChars.Length - 1 : ((Gamemode.Config.UI.ProgressChars.Length - 1) / Mathf.RoundToInt(Flag.MAX_POINTS)) * Math.Abs(cap);
    public static int FromMax(int cap, int max) => Math.Abs(cap) >= max ? Gamemode.Config.UI.ProgressChars.Length - 1 : ((Gamemode.Config.UI.ProgressChars.Length - 1) / max) * Math.Abs(cap);
    public static CaptureUIParameters ComputeUI(ulong team, Flag flag, bool inVehicle)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (flag.LastDeltaPoints == 0)
        {
            if (flag.IsContested(out _))
            {
                return new CaptureUIParameters(team, inVehicle ? EFlagStatus.IN_VEHICLE : EFlagStatus.CONTESTED, flag);
            }
            else
            {
                if (flag.IsObj(team))
                    return new CaptureUIParameters(team, inVehicle ? EFlagStatus.IN_VEHICLE : EFlagStatus.CAPTURING, flag);
                else
                    return new CaptureUIParameters(team, EFlagStatus.NOT_OBJECTIVE, flag);
            }
        }
        else if (flag.LastDeltaPoints > 0)
        {
            if (team == 1)
            {
                if (flag.Points > 0)
                {
                    if (flag.Points < Flag.MAX_POINTS)
                    {
                        return new CaptureUIParameters(team, inVehicle ? EFlagStatus.IN_VEHICLE : EFlagStatus.CAPTURING, flag);
                    }
                    else
                    {
                        return new CaptureUIParameters(team, EFlagStatus.SECURED, flag);
                    }
                }
                else
                {
                    if (flag.Points > -Flag.MAX_POINTS)
                    {
                        return new CaptureUIParameters(team, inVehicle ? EFlagStatus.IN_VEHICLE : EFlagStatus.CLEARING, flag);
                    }
                    else
                    {
                        return new CaptureUIParameters(team, EFlagStatus.NOT_OWNED, flag);
                    }
                }
            }
            else
            {
                if (flag.Points < 0)
                {
                    if (flag.Points > -Flag.MAX_POINTS)
                    {
                        return new CaptureUIParameters(team, inVehicle ? EFlagStatus.IN_VEHICLE : EFlagStatus.LOSING, flag);
                    }
                    else
                    {
                        return new CaptureUIParameters(team, EFlagStatus.SECURED, flag);
                    }
                }
                else
                {
                    if (flag.Points < Flag.MAX_POINTS)
                    {
                        return new CaptureUIParameters(team, inVehicle ? EFlagStatus.IN_VEHICLE : EFlagStatus.LOSING, flag);
                    }
                    else
                    {
                        return new CaptureUIParameters(team, EFlagStatus.NOT_OWNED, flag);
                    }
                }
            }
        }
        else
        {
            if (team == 2)
            {
                if (flag.Points < 0)
                {
                    if (flag.Points > -Flag.MAX_POINTS)
                    {
                        return new CaptureUIParameters(team, inVehicle ? EFlagStatus.IN_VEHICLE : EFlagStatus.CAPTURING, flag);
                    }
                    else
                    {
                        return new CaptureUIParameters(team, EFlagStatus.SECURED, flag);
                    }
                }
                else
                {
                    if (flag.Points < Flag.MAX_POINTS)
                    {
                        return new CaptureUIParameters(team, inVehicle ? EFlagStatus.IN_VEHICLE : EFlagStatus.CLEARING, flag);
                    }
                    else
                    {
                        return new CaptureUIParameters(team, EFlagStatus.NOT_OWNED, flag);
                    }
                }
            }
            else
            {
                if (flag.Points > 0)
                {
                    if (flag.Points < Flag.MAX_POINTS)
                    {
                        return new CaptureUIParameters(team, inVehicle ? EFlagStatus.IN_VEHICLE : EFlagStatus.LOSING, flag);
                    }
                    else
                    {
                        return new CaptureUIParameters(team, EFlagStatus.SECURED, flag);
                    }
                }
                else
                {
                    if (flag.Points > -Flag.MAX_POINTS)
                    {
                        return new CaptureUIParameters(team, inVehicle ? EFlagStatus.IN_VEHICLE : EFlagStatus.LOSING, flag);
                    }
                    else
                    {
                        return new CaptureUIParameters(team, EFlagStatus.NOT_OWNED, flag);
                    }
                }
            }
        }
    }
    public static void ClearCaptureUI(UCPlayer player)
    {
        CaptureUI.ClearFromPlayer(player.Connection);
    }
    public static void ClearCaptureUI(ITransportConnection player)
    {
        CaptureUI.ClearFromPlayer(player);
    }
    public static void ClearCaptureUI()
    {
        CaptureUI.ClearFromAllPlayers();
    }
    public static void ClearFlagList(UCPlayer player)
    {
        ListUI.ClearFromPlayer(player.Connection);
    }
    public static void ClearFlagList(ITransportConnection player)
    {
        ListUI.ClearFromPlayer(player);
    }
    public static void SendFlagList(UCPlayer player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (player == null) return;
        ulong team = player.GetTeam();
        if (team < 1 || team > 3) return;
        if (Data.Is(out IFlagRotation gm))
        {
            ITransportConnection c = player.Player.channel.owner.transportConnection;
            List<Flag> rotation = gm.Rotation;
            ListUI.SendToPlayer(c);
            ListUI.Header.SetVisibility(c, true);
            ListUI.Header.SetText(c, Translation.Translate("flag_header", player));
            if (team == 1 || team == 2)
            {
                for (int i = 0; i < Gamemode.Config.UI.FlagUICount; i++)
                {
                    if (rotation.Count <= i)
                    {
                        ListUI.Parents[i].SetVisibility(c, false);
                    }
                    else
                    {
                        ListUI.Parents[i].SetVisibility(c, true);
                        int index = team == 1 ? i : rotation.Count - i - 1;
                        Flag flag = rotation[index];
                        string objective = string.Empty;
                        if (flag.T1Obj)
                        {
                            if (team == 1)
                                objective = $"<color=#{UCWarfare.GetColorHex("attack_icon_color")}>{Gamemode.Config.UI.AttackIcon}</color>";
                            else if (team == 2 && flag.Owner == 2)
                                objective = $"<color=#{UCWarfare.GetColorHex("defend_icon_color")}>{Gamemode.Config.UI.DefendIcon}</color>";
                        }
                        if (flag.T2Obj)
                        {
                            if (team == 2)
                                objective = $"<color=#{UCWarfare.GetColorHex("attack_icon_color")}>{Gamemode.Config.UI.AttackIcon}</color>";
                            else if (team == 1 && flag.Owner == 1)
                                objective = $"<color=#{UCWarfare.GetColorHex("defend_icon_color")}>{Gamemode.Config.UI.DefendIcon}</color>";
                        }
                        ListUI.Names[i].SetText(c, flag.Discovered(team) ?
                            $"<color=#{flag.TeamSpecificHexColor}>{flag.Name}</color>" :
                            $"<color=#{UCWarfare.GetColorHex("undiscovered_flag")}>{Translation.Translate("undiscovered_flag", player)}</color>");
                        ListUI.Icons[i].SetText(c, objective);
                    }
                }
            }
            else if (team == 3)
            {
                for (int i = 0; i < Gamemode.Config.UI.FlagUICount; i++)
                {
                    if (rotation.Count <= i)
                    {
                        ListUI.Parents[i].SetVisibility(c, false);
                    }
                    else
                    {
                        ListUI.Parents[i].SetVisibility(c, true);
                        Flag flag = rotation[i];
                        string objective = string.Empty;
                        if (flag.T1Obj)
                        {
                            objective = $"<color=#{UCWarfare.GetColorHex("team_1_color")}>{Gamemode.Config.UI.AttackIcon}</color>";
                            if (flag.Owner == 2)
                                objective += $"<color=#{UCWarfare.GetColorHex("team_2_color")}>{Gamemode.Config.UI.DefendIcon}</color>";
                        }
                        if (flag.T2Obj)
                        {
                            objective = $"<color=#{UCWarfare.GetColorHex("team_2_color")}>{Gamemode.Config.UI.AttackIcon}</color>";
                            if (flag.Owner == 1)
                                objective += $"<color=#{UCWarfare.GetColorHex("team_1_color")}>{Gamemode.Config.UI.DefendIcon}</color>";
                        }
                        ListUI.Names[i].SetText(c, $"<color=#{flag.TeamSpecificHexColor}>{flag.Name}</color>" +
                            $"{(flag.Discovered(1) ? string.Empty : $" <color=#{TeamManager.Team1ColorHex}>?</color>")}" +
                            $"{(flag.Discovered(2) ? string.Empty : $" <color=#{TeamManager.Team2ColorHex}>?</color>")}");
                        ListUI.Icons[i].SetText(c, objective);
                    }
                }
            }
        }
    }
    public static void ReplicateFlagUpdate(Flag flag, bool ownerChanged = true)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (Data.Is(out IFlagRotation gm))
        {
            List<Flag> rotation = gm.Rotation;
            int index = rotation.IndexOf(flag);
            if (index == -1) return;
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            {
                UCPlayer player = PlayerManager.OnlinePlayers[i];
                ulong team = player.GetTeam();
                if (team < 1 || team > 3) continue;
                if (!flag.Discovered(team)) continue;
                ITransportConnection c = player.Player.channel.owner.transportConnection;
                int i3 = team == 2 ? rotation.Count - index - 1 : index;
                string objective = string.Empty;
                if (team == 1 || team == 2)
                {
                    if (flag.T1Obj)
                    {
                        if (team == 1)
                            objective = $"<color=#{UCWarfare.GetColorHex("attack_icon_color")}>{Gamemode.Config.UI.AttackIcon}</color>";
                        else if (team == 2 && flag.Owner == 2)
                            objective = $"<color=#{UCWarfare.GetColorHex("defend_icon_color")}>{Gamemode.Config.UI.DefendIcon}</color>";
                    }
                    if (flag.T2Obj)
                    {
                        if (team == 2)
                            objective = $"<color=#{UCWarfare.GetColorHex("attack_icon_color")}>{Gamemode.Config.UI.AttackIcon}</color>";
                        else if (team == 1 && flag.Owner == 1)
                            objective = $"<color=#{UCWarfare.GetColorHex("defend_icon_color")}>{Gamemode.Config.UI.DefendIcon}</color>";
                    }
                    if (ownerChanged)
                        ListUI.Names[i3].SetText(c, $"<color=#{flag.TeamSpecificHexColor}>{flag.Name}</color>");
                    ListUI.Icons[i3].SetText(c, objective);
                }
                else
                {
                    if (flag.T1Obj)
                    {
                        objective = $"<color=#{UCWarfare.GetColorHex("team_1_color")}>{Gamemode.Config.UI.AttackIcon}</color>";
                        if (flag.Owner == 2)
                            objective += $"<color=#{UCWarfare.GetColorHex("team_2_color")}>{Gamemode.Config.UI.DefendIcon}</color>";
                    }
                    if (flag.T2Obj)
                    {
                        objective = $"<color=#{UCWarfare.GetColorHex("team_2_color")}>{Gamemode.Config.UI.AttackIcon}</color>";
                        if (flag.Owner == 1)
                            objective += $"<color=#{UCWarfare.GetColorHex("team_1_color")}>{Gamemode.Config.UI.DefendIcon}</color>";
                    }
                    ListUI.Names[i3].SetText(c, $"<color=#{flag.TeamSpecificHexColor}>{flag.Name}</color>" +
                                                $"{(flag.Discovered(1) ? string.Empty : $" <color=#{TeamManager.Team1ColorHex}>?</color>")}" +
                                                $"{(flag.Discovered(2) ? string.Empty : $" <color=#{TeamManager.Team2ColorHex}>?</color>")}");
                    ListUI.Icons[i3].SetText(c, objective);
                }
            }
        }
    }
    public static CaptureUIParameters RefreshStaticUI(ulong team, Flag flag, bool inVehicle)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (team != 1 && team != 2) return new CaptureUIParameters(0, EFlagStatus.DONT_DISPLAY, null!);
        if (flag.IsAnObj)
        {
            return ComputeUI(team, flag, inVehicle); // if flag is objective send capturing ui.
        }
        else
        {
            if (flag.Owner == team)
                return new CaptureUIParameters(team, EFlagStatus.SECURED, flag);
            else if (flag.Owner == TeamManager.Other(team))
                return new CaptureUIParameters(team, EFlagStatus.NOT_OWNED, flag);
            else
                return new CaptureUIParameters(team, EFlagStatus.NOT_OBJECTIVE, flag);
        }
    }
}

[Translatable("Flag Status")]
public enum EFlagStatus
{
    [Translatable("CAPTURING")]
    CAPTURING,
    [Translatable("LOSING")]
    LOSING,
    [Translatable("SECURED")]
    SECURED,
    [Translatable("CONTESTED")]
    CONTESTED,
    [Translatable("NOT OBJECTIVE")]
    NOT_OBJECTIVE,
    [Translatable("CLEARING")]
    CLEARING,
    [Translatable("")]
    BLANK,
    [Translatable("NOT OWNED")]
    NOT_OWNED,
    [Translatable("")]
    DONT_DISPLAY,
    [Translatable("IN VEHICLE")]
    IN_VEHICLE,
    [Translatable("LOCKED")]
    LOCKED
}
