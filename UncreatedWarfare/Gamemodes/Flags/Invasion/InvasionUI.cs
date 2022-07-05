using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Teams;
using UnityEngine;
using static Uncreated.Warfare.Gamemodes.Flags.UI.CaptureUI;

namespace Uncreated.Warfare.Gamemodes.Flags.Invasion;

public static class InvasionUI
{
    public static CaptureUIParameters ComputeUI(ulong team, Flag flag, bool inVehicle, ulong atkTeam)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (flag.Owner == atkTeam)
        {
            if (team == atkTeam)
            {
                return new CaptureUIParameters(team, EFlagStatus.SECURED, flag);
            }
            else
            {
                return new CaptureUIParameters(team, EFlagStatus.LOCKED, flag);
            }
        }
        if (flag.LastDeltaPoints == 0)
        {
            if (flag.IsContested(out ulong winner))
            {
                return new CaptureUIParameters(team, inVehicle ? EFlagStatus.IN_VEHICLE : EFlagStatus.CONTESTED, flag);
            }
            else
            {
                if (winner == team)
                {
                    if (flag.IsFull(team))
                    {
                        return new CaptureUIParameters(team, EFlagStatus.SECURED, flag);
                    }
                    else if (team == atkTeam || flag.Owner != atkTeam)
                    {
                        if (team == 1)
                        {
                            if (flag.Points < 0)
                            {
                                return new CaptureUIParameters(team, inVehicle ? EFlagStatus.IN_VEHICLE : EFlagStatus.CLEARING, flag);
                            }
                            else
                            {
                                return new CaptureUIParameters(team, inVehicle ? EFlagStatus.IN_VEHICLE : EFlagStatus.CAPTURING, flag);
                            }
                        }
                        else
                        {
                            if (flag.Points > 0)
                            {
                                return new CaptureUIParameters(team, inVehicle ? EFlagStatus.IN_VEHICLE : EFlagStatus.CLEARING, flag);
                            }
                            else
                            {
                                return new CaptureUIParameters(team, inVehicle ? EFlagStatus.IN_VEHICLE : EFlagStatus.CAPTURING, flag);
                            }
                        }
                    }
                    else
                    {
                        return new CaptureUIParameters(team, inVehicle ? EFlagStatus.IN_VEHICLE : EFlagStatus.LOSING, flag);
                    }
                }
                else if (winner == TeamManager.Other(team))
                {
                    return new CaptureUIParameters(team, inVehicle ? EFlagStatus.IN_VEHICLE : EFlagStatus.LOSING, flag);
                } 
                else
                {
                    return new CaptureUIParameters(team, EFlagStatus.NOT_OBJECTIVE, flag);
                }
            }
        }
        else if (flag.LastDeltaPoints > 0)
        {
            if (team == 1) // us
            {
                if (atkTeam == 1) // us team is capturing on attack
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
                        return new CaptureUIParameters(team, inVehicle ? EFlagStatus.IN_VEHICLE : EFlagStatus.CLEARING, flag);
                    }
                }
                else // us team is capturing on defence
                {
                    if (flag.Owner != 2)
                    {
                        return new CaptureUIParameters(team, inVehicle ? EFlagStatus.IN_VEHICLE : EFlagStatus.CAPTURING, flag);
                    }
                    else
                    {
                        return new CaptureUIParameters(team, EFlagStatus.LOCKED, flag);
                    }
                }
            }
            else // ru
            {
                if (atkTeam == 2) // ru team is losing on attack
                {
                    if (flag.Owner == 2)
                    {
                        return new CaptureUIParameters(team, inVehicle ? EFlagStatus.IN_VEHICLE : EFlagStatus.CONTESTED, flag);
                    }
                    else
                    {
                        return new CaptureUIParameters(team, inVehicle ? EFlagStatus.IN_VEHICLE : EFlagStatus.LOSING, flag);
                    }
                }
                else // ru team is losing on defence
                {
                    return new CaptureUIParameters(team, inVehicle ? EFlagStatus.IN_VEHICLE : EFlagStatus.LOSING, flag);
                }
            }
        }
        else // flag.LastDeltaPoints < 0
        {
            if (team == 1)
            {
                if (atkTeam == 1) // us team is losing on attack
                {
                    if (flag.Owner == 1)
                    {
                        return new CaptureUIParameters(team, inVehicle ? EFlagStatus.IN_VEHICLE : EFlagStatus.CONTESTED, flag);
                    }
                    else
                    {
                        return new CaptureUIParameters(team, inVehicle ? EFlagStatus.IN_VEHICLE : EFlagStatus.LOSING, flag);
                    }
                }
                else // us team is losing on defence
                {
                    return new CaptureUIParameters(team, inVehicle ? EFlagStatus.IN_VEHICLE : EFlagStatus.LOSING, flag);
                }
            }
            else
            {
                if (atkTeam == 2) // ru team is capturing on attack
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
                        return new CaptureUIParameters(team, inVehicle ? EFlagStatus.IN_VEHICLE : EFlagStatus.CLEARING, flag);
                    }
                }
                else // ru team is capturing on defence
                {
                    if (flag.Owner != 1)
                    {
                        return new CaptureUIParameters(team, inVehicle ? EFlagStatus.IN_VEHICLE : EFlagStatus.CAPTURING, flag);
                    }
                    else
                    {
                        return new CaptureUIParameters(team, EFlagStatus.LOCKED, flag);
                    }
                }
            }
        }
    }
    public static void SendFlagList(UCPlayer player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (player == null) return;
        ulong team = player.GetTeam();
        if (team < 1 || team > 3) return;
        if (Data.Is(out IFlagRotation gm) && Data.Is(out IAttackDefense atkdef))
        {
            ulong attack = atkdef.AttackingTeam;
            ulong defense = atkdef.DefendingTeam;
            ITransportConnection c = player.Player.channel.owner.transportConnection;
            List<Flag> rotation = gm.Rotation;
            CTFUI.ListUI.SendToPlayer(c);
            CTFUI.ListUI.Header.SetVisibility(c, true);
            CTFUI.ListUI.Header.SetText(c, Translation.Translate("flag_header", player));
            if (team == 1 || team == 2)
            {
                for (int i = 0; i < CTFUI.ListUI.Parents.Length; i++)
                {
                    if (rotation.Count <= i)
                    {
                        CTFUI.ListUI.Parents[i].SetVisibility(c, false);
                    }
                    else
                    {
                        CTFUI.ListUI.Parents[i].SetVisibility(c, true);
                        int index = team == 1 ? i : rotation.Count - i - 1;
                        Flag flag = rotation[index];
                        string objective = string.Empty;
                        if (flag.Owner == attack)
                        {
                            objective = $"<color=#{UCWarfare.GetColorHex("locked_icon_color")}>{Gamemode.Config.UI.LockIcon}</color>";
                        }
                        else
                        {
                            if (flag.IsObj(attack))
                            {
                                if (team == attack)
                                    objective = $"<color=#{UCWarfare.GetColorHex("attack_icon_color")}>{Gamemode.Config.UI.AttackIcon}</color>";
                                else
                                    objective = $"<color=#{UCWarfare.GetColorHex("defend_icon_color")}>{Gamemode.Config.UI.DefendIcon}</color>";
                            }
                        }
                        CTFUI.ListUI.Names[i].SetText(c, flag.Discovered(team) ?
                            $"<color=#{flag.TeamSpecificHexColor}>{flag.Name}</color>" :
                            $"<color=#{UCWarfare.GetColorHex("undiscovered_flag")}>{Translation.Translate("undiscovered_flag", player)}</color>");
                        CTFUI.ListUI.Icons[i].SetText(c, objective);
                    }
                }
            }
            else if (team == 3)
            {
                for (int i = 0; i < CTFUI.ListUI.Parents.Length; i++)
                {
                    string i2 = i.ToString();
                    if (rotation.Count <= i)
                    {
                        CTFUI.ListUI.Parents[i].SetVisibility(c, false);
                    }
                    else
                    {
                        CTFUI.ListUI.Parents[i].SetVisibility(c, true);
                        Flag flag = rotation[i];
                        string objective = string.Empty;
                        if (flag.IsObj(attack))
                        {
                            objective = $"<color=#{TeamManager.GetTeamHexColor(attack)}>{Gamemode.Config.UI.AttackIcon}</color> <color=#{TeamManager.GetTeamHexColor(defense)}>{Gamemode.Config.UI.DefendIcon}</color>";
                        }
                        if (flag.Owner == attack)
                            objective = $"<color=#{TeamManager.GetTeamHexColor(defense)}>{Gamemode.Config.UI.LockIcon}</color>";
                        if (flag.T2Obj)
                        {
                            objective = $"<color=#{TeamManager.Team2ColorHex}>{Gamemode.Config.UI.AttackIcon}</color>";
                            if (flag.Owner == 1)
                                objective += $"<color=#{TeamManager.Team1ColorHex}>{Gamemode.Config.UI.DefendIcon}</color>";
                        }
                        CTFUI.ListUI.Names[i].SetText(c, $"<color=#{flag.TeamSpecificHexColor}>{flag.Name}</color>" +
                            $"{(flag.Discovered(1) ? string.Empty : $" <color=#{TeamManager.Team1ColorHex}>?</color>")}" +
                            $"{(flag.Discovered(2) ? string.Empty : $" <color=#{TeamManager.Team2ColorHex}>?</color>")}");
                        CTFUI.ListUI.Icons[i].SetText(c, objective);
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
        if (Data.Is(out IFlagRotation gm) && Data.Is(out IAttackDefense atkdef))
        {
            ulong attack = atkdef.AttackingTeam;
            ulong defense = atkdef.DefendingTeam;
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
                    if (flag.Owner == attack)
                    {
                        objective = $"<color=#{UCWarfare.GetColorHex("locked_icon_color")}>{Gamemode.Config.UI.LockIcon}</color>";
                    }
                    else
                    {
                        if (flag.IsObj(attack))
                        {
                            if (team == attack)
                                objective = $"<color=#{UCWarfare.GetColorHex("attack_icon_color")}>{Gamemode.Config.UI.AttackIcon}</color>";
                            else
                                objective = $"<color=#{UCWarfare.GetColorHex("defend_icon_color")}>{Gamemode.Config.UI.DefendIcon}</color>";
                        }
                    }
                    if (ownerChanged)
                        CTFUI.ListUI.Names[i3].SetText(c, $"<color=#{flag.TeamSpecificHexColor}>{flag.Name}</color>");
                    CTFUI.ListUI.Icons[i3].SetText(c, objective);
                }
                else
                {
                    if (flag.IsObj(attack))
                    {
                        objective = $"<color=#{TeamManager.GetTeamHexColor(attack)}>{Gamemode.Config.UI.AttackIcon}</color> <color=#{TeamManager.GetTeamHexColor(defense)}>{Gamemode.Config.UI.DefendIcon}</color>";
                    }
                    if (flag.Owner == attack)
                        objective = $"<color=#{TeamManager.GetTeamHexColor(defense)}>{Gamemode.Config.UI.LockIcon}</color>";
                    if (flag.T2Obj)
                    {
                        objective = $"<color=#{TeamManager.Team2ColorHex}>{Gamemode.Config.UI.AttackIcon}</color>";
                        if (flag.Owner == 1)
                            objective += $"<color=#{TeamManager.Team1ColorHex}>{Gamemode.Config.UI.DefendIcon}</color>";
                    }
                    if (ownerChanged)
                        CTFUI.ListUI.Names[i3].SetText(c, $"<color=#{flag.TeamSpecificHexColor}>{flag.Name}</color>" +
                                                          $"{(flag.Discovered(1) ? "" : $" <color=#{TeamManager.Team1ColorHex}>?</color>")}" +
                                                          $"{(flag.Discovered(2) ? "" : $" <color=#{TeamManager.Team2ColorHex}>?</color>")}");
                    CTFUI.ListUI.Icons[i3].SetText(c, objective);
                }
            }
        }
    }
    public static CaptureUIParameters RefreshStaticUI(ulong team, Flag flag, bool inVehicle, ulong atkTeam)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (team != 1 && team != 2) return new CaptureUIParameters(0, EFlagStatus.DONT_DISPLAY, null);
        if (flag.IsObj(atkTeam))
        {
            return ComputeUI(team, flag, inVehicle, atkTeam); // if flag is objective send capturing ui.
        }
        else
        {
            if (flag.Owner == team)
            {
                return new CaptureUIParameters(team, EFlagStatus.SECURED, flag);
            }
            else if (flag.Owner == TeamManager.Other(team))
            {
                if (flag.Owner == TeamManager.Other(atkTeam))
                {
                    return new CaptureUIParameters(team, EFlagStatus.NOT_OBJECTIVE, flag);
                }
                else
                {
                    return new CaptureUIParameters(team, EFlagStatus.LOCKED, flag);
                }
            }
            else
            {
                return new CaptureUIParameters(team, EFlagStatus.NOT_OBJECTIVE, flag);
            }
        }
    }
}