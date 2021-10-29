﻿using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes.Flags.Invasion
{
    public static class InvasionUI
    {
        public static int FromMax(int cap, string progresschars) => Math.Abs(cap) >= Mathf.RoundToInt(Flag.MAX_POINTS) ? progresschars.Length - 1 : ((progresschars.Length - 1) / Mathf.RoundToInt(Flag.MAX_POINTS)) * Math.Abs(cap);
        public static int FromMax(int cap, int max, string progresschars) => Math.Abs(cap) >= max ? progresschars.Length - 1 : ((progresschars.Length - 1) / max) * Math.Abs(cap);
        //public static Invasion Gamemode = null;
        public static SendUIParameters ComputeUI(ulong team, Flag flag, bool inVehicle, ulong atkTeam, bool canDefContest)
        {
            if (flag.LastDeltaPoints == 0)
            {
                if (flag.IsContested(out ulong winner))
                {
                    if (canDefContest)
                    {
                        return new SendUIParameters(team, inVehicle ? F.EFlagStatus.IN_VEHICLE : F.EFlagStatus.CONTESTED, "contested", UCWarfare.GetColor($"contested_team_{team}_chat"),
                            Mathf.RoundToInt(flag.Points), flag.Team1TotalPlayers, flag.Team2TotalCappers, flag.Name, flag.TeamSpecificHexColor);
                    }
                    else if (team == atkTeam)
                    {
                        return new SendUIParameters(team, inVehicle ? F.EFlagStatus.IN_VEHICLE : F.EFlagStatus.CAPTURING, "capturing", UCWarfare.GetColor($"capturing_team_{team}_chat"),
                            Mathf.RoundToInt(flag.Points), flag.Team1TotalPlayers, flag.Team2TotalCappers, flag.Name, flag.TeamSpecificHexColor);
                    }
                    else
                    {
                        return new SendUIParameters(team, inVehicle ? F.EFlagStatus.IN_VEHICLE : F.EFlagStatus.LOSING, "losing", UCWarfare.GetColor($"losing_team_{team}_chat"),
                            Mathf.RoundToInt(flag.Points), flag.Team1TotalPlayers, flag.Team2TotalCappers);
                    }
                }
                else
                {
                    if (winner == team)
                    {
                        if ((team == atkTeam && flag.IsFull(atkTeam)) || flag.IsFull(team))
                        {
                            return new SendUIParameters(team, inVehicle ? F.EFlagStatus.IN_VEHICLE : F.EFlagStatus.SECURED, "secured", UCWarfare.GetColor($"capturing_team_{team}_chat"),
                                Mathf.RoundToInt(flag.Points), flag.Team1TotalPlayers, flag.Team2TotalCappers, flag.Name, flag.TeamSpecificHexColor);
                        }
                        else if (team == atkTeam)
                        {
                            return new SendUIParameters(team, inVehicle ? F.EFlagStatus.IN_VEHICLE : F.EFlagStatus.CAPTURING, "capturing", UCWarfare.GetColor($"capturing_team_{team}_chat"),
                                Mathf.RoundToInt(flag.Points), flag.Team1TotalPlayers, flag.Team2TotalCappers, flag.Name, flag.TeamSpecificHexColor);
                        }
                        else
                        {
                            return new SendUIParameters(team, inVehicle ? F.EFlagStatus.IN_VEHICLE : F.EFlagStatus.LOSING, "losing", UCWarfare.GetColor($"losing_team_{team}_chat"),
                                Mathf.RoundToInt(flag.Points), flag.Team1TotalPlayers, flag.Team2TotalCappers);
                        }
                    }
                    else
                    {
                        if (team == atkTeam)
                        {
                            if (canDefContest)
                            {
                                return new SendUIParameters(team, inVehicle ? F.EFlagStatus.IN_VEHICLE : F.EFlagStatus.CONTESTED, "contested", UCWarfare.GetColor($"contested_team_{team}_chat"),
                                    Mathf.RoundToInt(flag.Points), flag.Team1TotalPlayers, flag.Team2TotalCappers, flag.Name, flag.TeamSpecificHexColor);
                            }
                            else
                            {
                                return new SendUIParameters(team, inVehicle ? F.EFlagStatus.IN_VEHICLE : F.EFlagStatus.CAPTURING, "capturing", UCWarfare.GetColor($"capturing_team_{team}_chat"),
                                    Mathf.RoundToInt(flag.Points), flag.Team1TotalPlayers, flag.Team2TotalCappers, flag.Name, flag.TeamSpecificHexColor);
                            }
                        }
                        else
                        {
                            if (canDefContest)
                            {
                                return new SendUIParameters(team, inVehicle ? F.EFlagStatus.IN_VEHICLE : F.EFlagStatus.CONTESTED, "contested", UCWarfare.GetColor($"contested_team_{team}_chat"),
                                    Mathf.RoundToInt(flag.Points), flag.Team1TotalPlayers, flag.Team2TotalCappers, flag.Name, flag.TeamSpecificHexColor);
                            }
                            else
                            {
                                return new SendUIParameters(team, inVehicle ? F.EFlagStatus.IN_VEHICLE : F.EFlagStatus.LOSING, "losing", UCWarfare.GetColor($"losing_team_{team}_chat"),
                                    Mathf.RoundToInt(flag.Points), flag.Team1TotalPlayers, flag.Team2TotalCappers);
                            }
                        }
                    }
                }
            }
            else
            {
                if (atkTeam == team)
                {
                    if (team == 1)
                    {
                        if (flag.Points > 0)
                        {
                            if (flag.Points < Flag.MAX_POINTS)
                            {
                                return new SendUIParameters(team, inVehicle ? F.EFlagStatus.IN_VEHICLE : F.EFlagStatus.CAPTURING, "capturing", UCWarfare.GetColor("capturing_team_1_chat"),
                                    Mathf.RoundToInt(flag.Points), flag.Team1TotalPlayers, flag.Team2TotalCappers);
                            }
                            else
                            {
                                return new SendUIParameters(team, F.EFlagStatus.SECURED, "secured", UCWarfare.GetColor("secured_team_1_chat"),
                                    Mathf.RoundToInt(Flag.MAX_POINTS), flag.Team1TotalPlayers, flag.Team2TotalCappers);
                            }
                        }
                        else
                        {
                            if (flag.Points > -Flag.MAX_POINTS)
                            {
                                return new SendUIParameters(team, inVehicle ? F.EFlagStatus.IN_VEHICLE : F.EFlagStatus.CLEARING, "clearing", UCWarfare.GetColor("clearing_team_1_chat"),
                                    Mathf.RoundToInt(flag.Points), flag.Team1TotalPlayers, flag.Team2TotalCappers);
                            }
                            else
                            {
                                return new SendUIParameters(team, F.EFlagStatus.NOT_OBJECTIVE, "nocap", UCWarfare.GetColor("nocap_team_1_chat"),
                                    Mathf.RoundToInt(Flag.MAX_POINTS), flag.Team1TotalPlayers, flag.Team2TotalCappers);
                            }
                        }
                    } 
                    else
                    {
                        if (flag.Points < 0)
                        {
                            if (flag.Points > -Flag.MAX_POINTS)
                            {
                                return new SendUIParameters(team, inVehicle ? F.EFlagStatus.IN_VEHICLE : F.EFlagStatus.CAPTURING, "capturing", UCWarfare.GetColor("capturing_team_2_chat"),
                                    Mathf.RoundToInt(flag.Points), flag.Team1TotalPlayers, flag.Team2TotalCappers);
                            }
                            else
                            {
                                return new SendUIParameters(team, F.EFlagStatus.SECURED, "secured", UCWarfare.GetColor("secured_team_2_chat"),
                                    Mathf.RoundToInt(Flag.MAX_POINTS), flag.Team1TotalPlayers, flag.Team2TotalCappers);
                            }
                        }
                        else
                        {
                            if (flag.Points < Flag.MAX_POINTS)
                            {
                                return new SendUIParameters(team, inVehicle ? F.EFlagStatus.IN_VEHICLE : F.EFlagStatus.CLEARING, "clearing", UCWarfare.GetColor("clearing_team_2_chat"),
                                    Mathf.RoundToInt(flag.Points), flag.Team1TotalPlayers, flag.Team2TotalCappers);
                            }
                            else
                            {
                                return new SendUIParameters(team, F.EFlagStatus.NOT_OBJECTIVE, "nocap", UCWarfare.GetColor("nocap_team_2_chat"),
                                    Mathf.RoundToInt(Flag.MAX_POINTS), flag.Team1TotalPlayers, flag.Team2TotalCappers);
                            }
                        }
                    }
                }
                else
                {
                    if (team == 1)
                    {
                        if (flag.Points < 0)
                        {
                            if (flag.Points > -Flag.MAX_POINTS)
                            {
                                return new SendUIParameters(team, inVehicle ? F.EFlagStatus.IN_VEHICLE : F.EFlagStatus.LOSING, "losing", UCWarfare.GetColor("losing_team_1_chat"),
                                    Mathf.RoundToInt(flag.Points), flag.Team1TotalPlayers, flag.Team2TotalCappers);
                            }
                            else
                            {
                                return new SendUIParameters(team, F.EFlagStatus.LOCKED, "locked", UCWarfare.GetColor("locked_team_1_chat"),
                                    Mathf.RoundToInt(Flag.MAX_POINTS), flag.Team1TotalPlayers, flag.Team2TotalCappers);
                            }
                        }
                        else
                        {
                            if (flag.Points < Flag.MAX_POINTS)
                            {
                                return new SendUIParameters(team, inVehicle ? F.EFlagStatus.IN_VEHICLE : F.EFlagStatus.LOSING, "losing", UCWarfare.GetColor("losing_team_1_chat"),
                                    Mathf.RoundToInt(flag.Points), flag.Team1TotalPlayers, flag.Team2TotalCappers);
                            }
                            else
                            {
                                return new SendUIParameters(team, F.EFlagStatus.SECURED, "secured", UCWarfare.GetColor("secured_team_1_chat"),
                                    Mathf.RoundToInt(Flag.MAX_POINTS), flag.Team1TotalPlayers, flag.Team2TotalCappers);
                            }
                        }
                    }
                    else
                    {
                        if (flag.Points < 0)
                        {
                            if (flag.Points > -Flag.MAX_POINTS)
                            {
                                return new SendUIParameters(team, inVehicle ? F.EFlagStatus.IN_VEHICLE : F.EFlagStatus.LOSING, "losing", UCWarfare.GetColor("losing_team_2_chat"),
                                    Mathf.RoundToInt(flag.Points), flag.Team1TotalPlayers, flag.Team2TotalCappers);
                            }
                            else
                            {
                                return new SendUIParameters(team, F.EFlagStatus.SECURED, "secured", UCWarfare.GetColor("secured_team_2_chat"),
                                    Mathf.RoundToInt(Flag.MAX_POINTS), flag.Team1TotalPlayers, flag.Team2TotalCappers);
                            }
                        }
                        else
                        {
                            if (flag.Points < Flag.MAX_POINTS)
                            {
                                return new SendUIParameters(team, inVehicle ? F.EFlagStatus.IN_VEHICLE : F.EFlagStatus.LOSING, "losing", UCWarfare.GetColor("losing_team_2_chat"),
                                    Mathf.RoundToInt(flag.Points), flag.Team1TotalPlayers, flag.Team2TotalCappers);
                            }
                            else
                            {
                                return new SendUIParameters(team, F.EFlagStatus.LOCKED, "locked", UCWarfare.GetColor("locked_team_2_chat"),
                                    Mathf.RoundToInt(Flag.MAX_POINTS), flag.Team1TotalPlayers, flag.Team2TotalCappers);
                            }
                        }
                    }
                }
            }
        }
        public static void ClearListUI(ITransportConnection player, int ListUiCount)
        {
            for (int i = 0; i < ListUiCount; i++) unchecked
                {
                    EffectManager.askEffectClearByID((ushort)(UCWarfare.Config.FlagSettings.FlagUIIdFirst + i), player);
                }
        }
        public static void SendFlagListUI(ITransportConnection player, ulong playerid, ulong team, List<Flag> Rotation, int ListUiCount, char AttackIcon, char DefendIcon, ulong atkteam, char LockIcon)
        {
            ClearListUI(player, ListUiCount);
            if (team == 1 || team == 2)
            {
                for (int i = 0; i < ListUiCount; i++)
                {
                    if (Rotation.Count <= i)
                    {
                        EffectManager.askEffectClearByID((ushort)(UCWarfare.Config.FlagSettings.FlagUIIdFirst + i), player);
                    }
                    else
                    {
                        int index = team == 1 ? i : Rotation.Count - i - 1;
                        if (Rotation[i] == default) continue;
                        unchecked
                        {
                            Flag flag = Rotation[index];
                            string objective = string.Empty;

                            if (flag.Owner == atkteam)
                            {
                                // send locked UI

                                objective = $"<color=#{UCWarfare.GetColorHex("locked_icon_color")}>{LockIcon}</color>";
                            }
                            else
                            {
                                if (flag.IsObj(atkteam))
                                {
                                    if (team == atkteam)
                                        objective = $"<color=#{UCWarfare.GetColorHex("attack_icon_color")}>{AttackIcon}</color>";
                                    else
                                        objective = $"<color=#{UCWarfare.GetColorHex("defend_icon_color")}>{DefendIcon}</color>";
                                }
                            }
                            EffectManager.sendUIEffect((ushort)(UCWarfare.Config.FlagSettings.FlagUIIdFirst + i), (short)(1000 + i), player, true, flag.Discovered(team) ?
                                $"<color=#{flag.TeamSpecificHexColor}>{flag.Name}</color>" :
                                $"<color=#{UCWarfare.GetColorHex("undiscovered_flag")}>{F.Translate("undiscovered_flag", playerid)}</color>",
                                objective
                            );
                        }
                    }
                }
            }
            else if (team == 3)
            {
                ulong defteam = TeamManager.Other(atkteam);
                for (int i = 0; i < ListUiCount; i++)
                {
                    if (Rotation.Count <= i)
                    {
                        EffectManager.askEffectClearByID((ushort)(UCWarfare.Config.FlagSettings.FlagUIIdFirst + i), player);
                    }
                    else
                    {
                        if (Rotation.Count <= i || Rotation[i] == default) continue;
                        unchecked
                        {
                            Flag flag = Rotation[i];
                            string objective = string.Empty;
                            if (flag.IsObj(atkteam))
                            {
                                objective = $"<color=#{UCWarfare.GetColorHex($"team_{atkteam}_color")}>{AttackIcon}</color> <color=#{UCWarfare.GetColorHex($"team_{defteam}_color")}>{DefendIcon}</color>";
                            }
                            if (flag.Owner == atkteam)
                                objective = $"<color=#{UCWarfare.GetColorHex($"team_{defteam}_color")}>{LockIcon}</color>";
                            if (flag.T2Obj)
                            {
                                objective = $"<color=#{UCWarfare.GetColorHex("team_2_color")}>{AttackIcon}</color>";
                                if (flag.Owner == 1)
                                    objective += $"<color=#{UCWarfare.GetColorHex("team_1_color")}>{DefendIcon}</color>";
                            }
                            EffectManager.sendUIEffect((ushort)(UCWarfare.Config.FlagSettings.FlagUIIdFirst + i), (short)(1000 + i), player, true,
                                $"<color=#{flag.TeamSpecificHexColor}>{flag.Name}</color>" +
                                $"{(flag.Discovered(1) ? "" : $" <color=#{TeamManager.Team1ColorHex}>?</color>")}" +
                                $"{(flag.Discovered(2) ? "" : $" <color=#{TeamManager.Team2ColorHex}>?</color>")}",
                                objective
                                );
                        }
                    }
                }
            }
        }
        public static SendUIParameters RefreshStaticUI(ulong team, Flag flag, bool inVehicle, ulong atkTeam, bool canDefContest)
        {
            if (team != 1 && team != 2) return SendUIParameters.Nil;
            if (flag.IsObj(atkTeam))
            {
                return ComputeUI(team, flag, inVehicle, atkTeam, canDefContest); // if flag is objective send capturing ui.
            }
            else
            {
                if (flag.Owner == team)
                {
                    return new SendUIParameters(team, F.EFlagStatus.SECURED, "secured", UCWarfare.GetColor($"secured_team_{team}_chat"),
                        Mathf.RoundToInt(Flag.MAX_POINTS), flag.Team1TotalPlayers, flag.Team2TotalCappers);
                }
                else if (flag.Owner == TeamManager.Other(team))
                {
                    if (flag.Owner == TeamManager.Other(atkTeam))
                    {
                        return new SendUIParameters(team, F.EFlagStatus.NOT_OBJECTIVE, "nocap", UCWarfare.GetColor($"nocap_team_{team}_chat"),
                            Mathf.RoundToInt(Flag.MAX_POINTS), flag.Team1TotalPlayers, flag.Team2TotalCappers);
                    }
                    else
                    {
                        return new SendUIParameters(team, F.EFlagStatus.LOCKED, "locked", UCWarfare.GetColor($"locked_team_{team}_chat"),
                            Mathf.RoundToInt(Flag.MAX_POINTS), flag.Team1TotalPlayers, flag.Team2TotalCappers);
                    }
                }
                else
                    return new SendUIParameters(team, F.EFlagStatus.NOT_OBJECTIVE, "nocap", UCWarfare.GetColor($"notowned_team_{team}_chat"),
                        Mathf.RoundToInt(Flag.MAX_POINTS), flag.Team1TotalPlayers, flag.Team2TotalCappers);
            }
        }
    }
}