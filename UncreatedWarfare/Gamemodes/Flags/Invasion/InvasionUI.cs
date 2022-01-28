using SDG.NetTransport;
using SDG.Unturned;
using System.Collections.Generic;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes.Flags.Invasion
{
    public static class InvasionUI
    {
        public static SendUIParameters ComputeUI(ulong team, Flag flag, bool inVehicle, ulong atkTeam)
        {
            if (flag.Owner == atkTeam)
            {
                if (team == atkTeam)
                {
                    return new SendUIParameters(team, EFlagStatus.SECURED, "secured", UCWarfare.GetColor($"secured_team_{team}_chat"),
                        Mathf.RoundToInt(Flag.MAX_POINTS), flag.Team1TotalPlayers, flag.Team2TotalCappers, flag.Name, flag.TeamSpecificHexColor);
                }
                else
                {
                    return new SendUIParameters(team, EFlagStatus.LOCKED, "locked", UCWarfare.GetColor($"locked_team_{team}_chat"),
                        Mathf.RoundToInt(Flag.MAX_POINTS), flag.Team1TotalPlayers, flag.Team2TotalCappers);
                }
            }
            if (flag.LastDeltaPoints == 0)
            {
                if (flag.IsContested(out ulong winner))
                {
                    return new SendUIParameters(team, inVehicle ? EFlagStatus.IN_VEHICLE : EFlagStatus.CONTESTED, "contested", UCWarfare.GetColor($"contested_team_{team}_chat"),
                        Mathf.RoundToInt(flag.Points), flag.Team1TotalPlayers, flag.Team2TotalCappers, flag.Name, flag.TeamSpecificHexColor);
                }
                else
                {
                    if (winner == team)
                    {
                        if (flag.IsFull(team))
                        {
                            return new SendUIParameters(team, EFlagStatus.SECURED, "secured", UCWarfare.GetColor($"secured_team_{team}_chat"),
                                Mathf.RoundToInt(flag.Points), flag.Team1TotalPlayers, flag.Team2TotalCappers, flag.Name, flag.TeamSpecificHexColor);
                        }
                        else if (team == atkTeam || flag.Owner != atkTeam)
                        {
                            if (team == 1)
                            {
                                if (flag.Points < 0)
                                {
                                    return new SendUIParameters(team, inVehicle ? EFlagStatus.IN_VEHICLE : EFlagStatus.CLEARING, "clearing", UCWarfare.GetColor($"clearing_team_1_chat"),
                                        Mathf.RoundToInt(flag.Points), flag.Team1TotalPlayers, flag.Team2TotalCappers, flag.Name, flag.TeamSpecificHexColor);
                                }
                                else
                                {
                                    return new SendUIParameters(team, inVehicle ? EFlagStatus.IN_VEHICLE : EFlagStatus.CAPTURING, "capturing", UCWarfare.GetColor($"capturing_team_1_chat"),
                                        Mathf.RoundToInt(flag.Points), flag.Team1TotalPlayers, flag.Team2TotalCappers, flag.Name, flag.TeamSpecificHexColor);
                                }
                            }
                            else
                            {
                                if (flag.Points > 0)
                                {
                                    return new SendUIParameters(team, inVehicle ? EFlagStatus.IN_VEHICLE : EFlagStatus.CLEARING, "clearing", UCWarfare.GetColor($"clearing_team_2_chat"),
                                        Mathf.RoundToInt(flag.Points), flag.Team1TotalPlayers, flag.Team2TotalCappers, flag.Name, flag.TeamSpecificHexColor);
                                }
                                else
                                {
                                    return new SendUIParameters(team, inVehicle ? EFlagStatus.IN_VEHICLE : EFlagStatus.CAPTURING, "capturing", UCWarfare.GetColor($"capturing_team_2_chat"),
                                        Mathf.RoundToInt(flag.Points), flag.Team1TotalPlayers, flag.Team2TotalCappers, flag.Name, flag.TeamSpecificHexColor);
                                }
                            }
                        }
                        else
                        {
                            return new SendUIParameters(team, inVehicle ? EFlagStatus.IN_VEHICLE : EFlagStatus.LOSING, "losing", UCWarfare.GetColor($"losing_team_{team}_chat"),
                                Mathf.RoundToInt(flag.Points), flag.Team1TotalPlayers, flag.Team2TotalCappers);
                        }
                    }
                    else if (winner == TeamManager.Other(team))
                    {
                        return new SendUIParameters(team, inVehicle ? EFlagStatus.IN_VEHICLE : EFlagStatus.LOSING, "losing", UCWarfare.GetColor($"losing_team_{team}_chat"),
                            Mathf.RoundToInt(flag.Points), flag.Team1TotalPlayers, flag.Team2TotalCappers);
                    } 
                    else
                    {
                        return new SendUIParameters(team, EFlagStatus.NOT_OBJECTIVE, "nocap", UCWarfare.GetColor($"nocap_team_{team}_chat"),
                            Mathf.RoundToInt(flag.Points), flag.Team1TotalPlayers, flag.Team2TotalCappers);
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
                                return new SendUIParameters(team, inVehicle ? EFlagStatus.IN_VEHICLE : EFlagStatus.CAPTURING, "capturing", UCWarfare.GetColor("capturing_team_1_chat"),
                                    Mathf.RoundToInt(flag.Points), flag.Team1TotalPlayers, flag.Team2TotalCappers);
                            }
                            else
                            {
                                return new SendUIParameters(team, EFlagStatus.SECURED, "secured", UCWarfare.GetColor("secured_team_1_chat"),
                                    Mathf.RoundToInt(Flag.MAX_POINTS), flag.Team1TotalPlayers, flag.Team2TotalCappers);
                            }
                        }
                        else
                        {
                            return new SendUIParameters(team, inVehicle ? EFlagStatus.IN_VEHICLE : EFlagStatus.CLEARING, "clearing", UCWarfare.GetColor("clearing_team_1_chat"),
                                Mathf.RoundToInt(flag.Points), flag.Team1TotalPlayers, flag.Team2TotalCappers);
                        }
                    }
                    else // us team is capturing on defence
                    {
                        if (flag.Owner != 2)
                        {
                            return new SendUIParameters(team, inVehicle ? EFlagStatus.IN_VEHICLE : EFlagStatus.CAPTURING, "capturing", UCWarfare.GetColor("capturing_team_1_chat"),
                                Mathf.RoundToInt(flag.Points), flag.Team1TotalPlayers, flag.Team2TotalCappers);
                        }
                        else
                        {
                            return new SendUIParameters(team, EFlagStatus.LOCKED, "locked", UCWarfare.GetColor("locked_team_1_chat"),
                                Mathf.RoundToInt(Flag.MAX_POINTS), flag.Team1TotalPlayers, flag.Team2TotalCappers);
                        }
                    }
                }
                else // ru
                {
                    if (atkTeam == 2) // ru team is losing on attack
                    {
                        if (flag.Owner == 2)
                        {
                            return new SendUIParameters(team, inVehicle ? EFlagStatus.IN_VEHICLE : EFlagStatus.CONTESTED, "contested", UCWarfare.GetColor("contested_team_2_chat"),
                                Mathf.RoundToInt(flag.Points), flag.Team1TotalPlayers, flag.Team2TotalCappers, flag.Name, flag.TeamSpecificHexColor);
                        }
                        else
                        {
                            return new SendUIParameters(team, inVehicle ? EFlagStatus.IN_VEHICLE : EFlagStatus.LOSING, "losing", UCWarfare.GetColor("losing_team_2_chat"),
                                Mathf.RoundToInt(flag.Points), flag.Team1TotalPlayers, flag.Team2TotalCappers);
                        }
                    }
                    else // ru team is losing on defence
                    {
                        return new SendUIParameters(team, inVehicle ? EFlagStatus.IN_VEHICLE : EFlagStatus.LOSING, "losing", UCWarfare.GetColor("losing_team_2_chat"),
                            Mathf.RoundToInt(flag.Points), flag.Team1TotalPlayers, flag.Team2TotalCappers);
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
                            return new SendUIParameters(team, inVehicle ? EFlagStatus.IN_VEHICLE : EFlagStatus.CONTESTED, "contested", UCWarfare.GetColor("contested_team_1_chat"),
                                Mathf.RoundToInt(flag.Points), flag.Team1TotalPlayers, flag.Team2TotalCappers, flag.Name, flag.TeamSpecificHexColor);
                        }
                        else
                        {
                            return new SendUIParameters(team, inVehicle ? EFlagStatus.IN_VEHICLE : EFlagStatus.LOSING, "losing", UCWarfare.GetColor("losing_team_1_chat"),
                                Mathf.RoundToInt(flag.Points), flag.Team1TotalPlayers, flag.Team2TotalCappers);
                        }
                    }
                    else // us team is losing on defence
                    {
                        return new SendUIParameters(team, inVehicle ? EFlagStatus.IN_VEHICLE : EFlagStatus.LOSING, "losing", UCWarfare.GetColor("losing_team_1_chat"),
                            Mathf.RoundToInt(flag.Points), flag.Team1TotalPlayers, flag.Team2TotalCappers);
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
                                return new SendUIParameters(team, inVehicle ? EFlagStatus.IN_VEHICLE : EFlagStatus.CAPTURING, "capturing", UCWarfare.GetColor("capturing_team_2_chat"),
                                    Mathf.RoundToInt(flag.Points), flag.Team1TotalPlayers, flag.Team2TotalCappers);
                            }
                            else
                            {
                                return new SendUIParameters(team, EFlagStatus.SECURED, "secured", UCWarfare.GetColor("secured_team_2_chat"),
                                    Mathf.RoundToInt(Flag.MAX_POINTS), flag.Team1TotalPlayers, flag.Team2TotalCappers);
                            }
                        }
                        else
                        {
                            return new SendUIParameters(team, inVehicle ? EFlagStatus.IN_VEHICLE : EFlagStatus.CLEARING, "clearing", UCWarfare.GetColor("clearing_team_2_chat"),
                                Mathf.RoundToInt(flag.Points), flag.Team1TotalPlayers, flag.Team2TotalCappers);
                        }
                    }
                    else // ru team is capturing on defence
                    {
                        if (flag.Owner != 1)
                        {
                            return new SendUIParameters(team, inVehicle ? EFlagStatus.IN_VEHICLE : EFlagStatus.CAPTURING, "capturing", UCWarfare.GetColor("capturing_team_2_chat"),
                                Mathf.RoundToInt(flag.Points), flag.Team1TotalPlayers, flag.Team2TotalCappers);
                        }
                        else
                        {
                            return new SendUIParameters(team, EFlagStatus.LOCKED, "locked", UCWarfare.GetColor("locked_team_2_chat"),
                                Mathf.RoundToInt(Flag.MAX_POINTS), flag.Team1TotalPlayers, flag.Team2TotalCappers);
                        }
                    }
                }
            }
        }
        public static void SendFlagList(UCPlayer player)
        {
            if (player == null) return;
            ulong team = player.GetTeam();
            if (team < 1 || team > 3) return;
            if (Data.Is(out IFlagRotation gm) && Data.Is(out IAttackDefense atkdef))
            {
                ulong attack = atkdef.AttackingTeam;
                ulong defense = atkdef.DefendingTeam;
                ITransportConnection c = player.Player.channel.owner.transportConnection;
                List<Flag> rotation = gm.Rotation;
                EffectManager.sendUIEffect(CTFUI.flagListID, CTFUI.flagListKey, c, true);
                EffectManager.sendUIEffectVisibility(CTFUI.flagListKey, c, true, "Header", true);
                EffectManager.sendUIEffectText(CTFUI.flagListKey, c, true, "Header", Translation.Translate("flag_header", player));
                if (team == 1 || team == 2)
                {
                    for (int i = 0; i < Gamemode.Config.UI.FlagUICount; i++)
                    {
                        string i2 = i.ToString();
                        if (rotation.Count <= i)
                        {
                            EffectManager.sendUIEffectVisibility(CTFUI.flagListKey, c, true, i2, false);
                        }
                        else
                        {
                            EffectManager.sendUIEffectVisibility(CTFUI.flagListKey, c, true, i2, true);
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
                            EffectManager.sendUIEffectText(CTFUI.flagListKey, c, true, "N" + i2,
                                flag.Discovered(team) ?
                                $"<color=#{flag.TeamSpecificHexColor}>{flag.Name}</color>" :
                                $"<color=#{UCWarfare.GetColorHex("undiscovered_flag")}>{Translation.Translate("undiscovered_flag", player)}</color>");
                            EffectManager.sendUIEffectText(CTFUI.flagListKey, c, true, "I" + i2, objective);
                        }
                    }
                }
                else if (team == 3)
                {
                    for (int i = 0; i < Gamemode.Config.UI.FlagUICount; i++)
                    {
                        string i2 = i.ToString();
                        if (rotation.Count <= i)
                        {
                            EffectManager.sendUIEffectVisibility(CTFUI.flagListKey, c, true, i2, false);
                        }
                        else
                        {
                            EffectManager.sendUIEffectVisibility(CTFUI.flagListKey, c, true, i2, true);
                            Flag flag = rotation[i];
                            string objective = string.Empty;
                            if (flag.IsObj(attack))
                            {
                                objective = $"<color=#{UCWarfare.GetColorHex($"team_{attack}_color")}>{Gamemode.Config.UI.AttackIcon}</color> <color=#{UCWarfare.GetColorHex($"team_{defense}_color")}>{Gamemode.Config.UI.DefendIcon}</color>";
                            }
                            if (flag.Owner == attack)
                                objective = $"<color=#{UCWarfare.GetColorHex($"team_{defense}_color")}>{Gamemode.Config.UI.LockIcon}</color>";
                            if (flag.T2Obj)
                            {
                                objective = $"<color=#{UCWarfare.GetColorHex("team_2_color")}>{Gamemode.Config.UI.AttackIcon}</color>";
                                if (flag.Owner == 1)
                                    objective += $"<color=#{UCWarfare.GetColorHex("team_1_color")}>{Gamemode.Config.UI.DefendIcon}</color>";
                            }
                            EffectManager.sendUIEffectText(CTFUI.flagListKey, c, true, "N" + i2,
                                $"<color=#{flag.TeamSpecificHexColor}>{flag.Name}</color>" +
                                $"{(flag.Discovered(1) ? "" : $" <color=#{TeamManager.Team1ColorHex}>?</color>")}" +
                                $"{(flag.Discovered(2) ? "" : $" <color=#{TeamManager.Team2ColorHex}>?</color>")}");
                            EffectManager.sendUIEffectText(CTFUI.flagListKey, c, true, "I" + i2, objective);
                        }
                    }
                }
            }
            else
            {
                L.Log($"    Gamemode was not IflagRotation or IAttackDefense");
                L.Log($"        Is IFlagRotation: {Data.Is(out IFlagRotation gg2)}");
                L.Log($"        Is IAttackDefense: {Data.Is(out IAttackDefense gg3)}");
            }
        }
        public static void ReplicateFlagUpdate(Flag flag, bool ownerChanged = true)
        {
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
                    string i2 = i3.ToString();
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
                            EffectManager.sendUIEffectText(CTFUI.flagListKey, c, true, "N" + i2, $"<color=#{flag.TeamSpecificHexColor}>{flag.Name}</color>");
                        EffectManager.sendUIEffectText(CTFUI.flagListKey, c, true, "I" + i2, objective);
                    }
                    else
                    {
                        if (flag.IsObj(attack))
                        {
                            objective = $"<color=#{UCWarfare.GetColorHex($"team_{attack}_color")}>{Gamemode.Config.UI.AttackIcon}</color> <color=#{UCWarfare.GetColorHex($"team_{defense}_color")}>{Gamemode.Config.UI.DefendIcon}</color>";
                        }
                        if (flag.Owner == attack)
                            objective = $"<color=#{UCWarfare.GetColorHex($"team_{defense}_color")}>{Gamemode.Config.UI.LockIcon}</color>";
                        if (flag.T2Obj)
                        {
                            objective = $"<color=#{UCWarfare.GetColorHex("team_2_color")}>{Gamemode.Config.UI.AttackIcon}</color>";
                            if (flag.Owner == 1)
                                objective += $"<color=#{UCWarfare.GetColorHex("team_1_color")}>{Gamemode.Config.UI.DefendIcon}</color>";
                        }
                        if (ownerChanged)
                            EffectManager.sendUIEffectText(CTFUI.flagListKey, c, true, "N" + i2,
                                $"<color=#{flag.TeamSpecificHexColor}>{flag.Name}</color>" +
                                $"{(flag.Discovered(1) ? "" : $" <color=#{TeamManager.Team1ColorHex}>?</color>")}" +
                                $"{(flag.Discovered(2) ? "" : $" <color=#{TeamManager.Team2ColorHex}>?</color>")}");
                        EffectManager.sendUIEffectText(CTFUI.flagListKey, c, true, "I" + i2, objective);
                    }
                }
            }
        }
        public static SendUIParameters RefreshStaticUI(ulong team, Flag flag, bool inVehicle, ulong atkTeam)
        {
            if (team != 1 && team != 2) return SendUIParameters.Nil;
            if (flag.IsObj(atkTeam))
            {
                return ComputeUI(team, flag, inVehicle, atkTeam); // if flag is objective send capturing ui.
            }
            else
            {
                if (flag.Owner == team)
                {
                    return new SendUIParameters(team, EFlagStatus.SECURED, "secured", UCWarfare.GetColor($"secured_team_{team}_chat"),
                        Mathf.RoundToInt(Flag.MAX_POINTS), flag.Team1TotalPlayers, flag.Team2TotalCappers);
                }
                else if (flag.Owner == TeamManager.Other(team))
                {
                    if (flag.Owner == TeamManager.Other(atkTeam))
                    {
                        return new SendUIParameters(team, EFlagStatus.NOT_OBJECTIVE, "nocap", UCWarfare.GetColor($"notowned_team_{team}_chat"),
                            Mathf.RoundToInt(Flag.MAX_POINTS), flag.Team1TotalPlayers, flag.Team2TotalCappers);
                    }
                    else
                    {
                        return new SendUIParameters(team, EFlagStatus.LOCKED, "locked", UCWarfare.GetColor($"locked_team_{team}_chat"),
                            Mathf.RoundToInt(Flag.MAX_POINTS), flag.Team1TotalPlayers, flag.Team2TotalCappers);
                    }
                }
                else
                {
                    return new SendUIParameters(team, EFlagStatus.NOT_OBJECTIVE, "nocap", UCWarfare.GetColor($"notowned_team_{team}_chat"),
                        Mathf.RoundToInt(Flag.MAX_POINTS), flag.Team1TotalPlayers, flag.Team2TotalCappers);
                }
            }
        }
    }
}