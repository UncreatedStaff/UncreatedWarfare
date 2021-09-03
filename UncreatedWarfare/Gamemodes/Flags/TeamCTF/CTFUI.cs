using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes.Flags.TeamCTF
{
    public static class CTFUI
    {
        public static int FromMax(int cap, string progresschars) => Math.Abs(cap) >= Flag.MaxPoints ? progresschars.Length - 1 : ((progresschars.Length - 1) / Flag.MaxPoints) * Math.Abs(cap);
        public static int FromMax(int cap, int max, string progresschars) => Math.Abs(cap) >= max ? progresschars.Length - 1 : ((progresschars.Length - 1) / max) * Math.Abs(cap);
        public static SendUIParameters ComputeUI(ulong team, Flag flag, bool inVehicle)
        {
            if (flag.LastDeltaPoints == 0)
            {
                if (flag.IsContested(out _))
                {
                    return new SendUIParameters(team, inVehicle ? F.EFlagStatus.IN_VEHICLE : F.EFlagStatus.CONTESTED, "contested", UCWarfare.GetColor($"contested_team_{team}_chat"),
                        flag.Points, flag.Team1TotalPlayers, flag.Team2TotalCappers, flag.Name, flag.TeamSpecificHexColor);
                }
                else
                {
                    if (flag.IsObj(team))
                        return new SendUIParameters(team, inVehicle ? F.EFlagStatus.IN_VEHICLE : F.EFlagStatus.CAPTURING, "capturing", UCWarfare.GetColor($"capturing_team_{team}_chat"),
                            flag.Points, flag.Team1TotalPlayers, flag.Team2TotalCappers, flag.Name, flag.TeamSpecificHexColor);
                    else
                        return new SendUIParameters(team, F.EFlagStatus.NOT_OBJECTIVE, "nocap", UCWarfare.GetColor($"nocap_team_{team}_chat"),
                            Flag.MaxPoints, flag.Team1TotalPlayers, flag.Team2TotalCappers, flag.Name, flag.TeamSpecificHexColor);
                }
            }
            else if (flag.LastDeltaPoints > 0)
            {
                if (team == 1)
                {
                    if (flag.Points > 0)
                    {
                        if (flag.Points < Flag.MaxPoints)
                        {
                            return new SendUIParameters(team, inVehicle ? F.EFlagStatus.IN_VEHICLE : F.EFlagStatus.CAPTURING, "capturing", UCWarfare.GetColor("capturing_team_1_chat"),
                                flag.Points, flag.Team1TotalPlayers, flag.Team2TotalCappers);
                        }
                        else
                        {
                            return new SendUIParameters(team, F.EFlagStatus.SECURED, "secured", UCWarfare.GetColor("secured_team_1_chat"),
                                Flag.MaxPoints, flag.Team1TotalPlayers, flag.Team2TotalCappers);
                        }
                    }
                    else
                    {
                        if (flag.Points > -Flag.MaxPoints)
                        {
                            return new SendUIParameters(team, inVehicle ? F.EFlagStatus.IN_VEHICLE : F.EFlagStatus.CLEARING, "clearing", UCWarfare.GetColor("clearing_team_1_chat"),
                                flag.Points, flag.Team1TotalPlayers, flag.Team2TotalCappers);
                        }
                        else
                        {
                            return new SendUIParameters(team, F.EFlagStatus.NOT_OWNED, "notowned", UCWarfare.GetColor("notowned_team_1_chat"),
                                Flag.MaxPoints, flag.Team1TotalPlayers, flag.Team2TotalCappers);
                        }
                    }
                }
                else
                {
                    if (flag.Points < 0)
                    {
                        if (flag.Points > -Flag.MaxPoints)
                        {
                            return new SendUIParameters(team, inVehicle ? F.EFlagStatus.IN_VEHICLE : F.EFlagStatus.LOSING, "losing", UCWarfare.GetColor("losing_team_2_chat"),
                                flag.Points, flag.Team1TotalPlayers, flag.Team2TotalCappers);
                        }
                        else
                        {
                            return new SendUIParameters(team, F.EFlagStatus.SECURED, "secured", UCWarfare.GetColor("secured_team_2_chat"),
                                Flag.MaxPoints, flag.Team1TotalPlayers, flag.Team2TotalCappers);
                        }
                    }
                    else
                    {
                        if (flag.Points < Flag.MaxPoints)
                        {
                            return new SendUIParameters(team, inVehicle ? F.EFlagStatus.IN_VEHICLE : F.EFlagStatus.LOSING, "losing", UCWarfare.GetColor("losing_team_2_chat"),
                                flag.Points, flag.Team1TotalPlayers, flag.Team2TotalCappers);
                        }
                        else
                        {
                            return new SendUIParameters(team, F.EFlagStatus.NOT_OWNED, "notowned", UCWarfare.GetColor("notowned_team_2_chat"),
                                Flag.MaxPoints, flag.Team1TotalPlayers, flag.Team2TotalCappers);
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
                        if (flag.Points > -Flag.MaxPoints)
                        {
                            return new SendUIParameters(team, inVehicle ? F.EFlagStatus.IN_VEHICLE : F.EFlagStatus.CAPTURING, "capturing", UCWarfare.GetColor("capturing_team_2_chat"),
                                flag.Points, flag.Team1TotalPlayers, flag.Team2TotalCappers);
                        }
                        else
                        {
                            return new SendUIParameters(team, F.EFlagStatus.SECURED, "secured", UCWarfare.GetColor("secured_team_2_chat"),
                                Flag.MaxPoints, flag.Team1TotalPlayers, flag.Team2TotalCappers);
                        }
                    }
                    else
                    {
                        if (flag.Points < Flag.MaxPoints)
                        {
                            return new SendUIParameters(team, inVehicle ? F.EFlagStatus.IN_VEHICLE : F.EFlagStatus.CLEARING, "clearing", UCWarfare.GetColor("clearing_team_2_chat"),
                                flag.Points, flag.Team1TotalPlayers, flag.Team2TotalCappers);
                        }
                        else
                        {
                            return new SendUIParameters(team, F.EFlagStatus.NOT_OWNED, "notowned", UCWarfare.GetColor("notowned_team_2_chat"),
                                Flag.MaxPoints, flag.Team1TotalPlayers, flag.Team2TotalCappers);
                        }
                    }
                }
                else
                {
                    if (flag.Points > 0)
                    {
                        if (flag.Points < Flag.MaxPoints)
                        {
                            return new SendUIParameters(team, inVehicle ? F.EFlagStatus.IN_VEHICLE : F.EFlagStatus.LOSING, "losing", UCWarfare.GetColor("losing_team_1_chat"),
                                flag.Points, flag.Team1TotalPlayers, flag.Team2TotalCappers);
                        }
                        else
                        {
                            return new SendUIParameters(team, F.EFlagStatus.SECURED, "secured", UCWarfare.GetColor("secured_team_1_chat"),
                                Flag.MaxPoints, flag.Team1TotalPlayers, flag.Team2TotalCappers);
                        }
                    }
                    else
                    {
                        if (flag.Points > -Flag.MaxPoints)
                        {
                            return new SendUIParameters(team, inVehicle ? F.EFlagStatus.IN_VEHICLE : F.EFlagStatus.LOSING, "losing", UCWarfare.GetColor("losing_team_1_chat"),
                                flag.Points, flag.Team1TotalPlayers, flag.Team2TotalCappers);
                        }
                        else
                        {
                            return new SendUIParameters(team, F.EFlagStatus.NOT_OWNED, "notowned", UCWarfare.GetColor("notowned_team_1_chat"),
                                Flag.MaxPoints, flag.Team1TotalPlayers, flag.Team2TotalCappers);
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
        public static void SendFlagListUI(ITransportConnection player, ulong playerid, ulong team, List<Flag> Rotation, int ListUiCount, char AttackIcon, char DefendIcon)
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
                            if (flag.T1Obj)
                            {
                                if (team == 1)
                                    objective = $"<color=#{UCWarfare.GetColorHex("attack_icon_color")}>{AttackIcon}</color>";
                                else if (team == 2 && flag.Owner == 2)
                                    objective = $"<color=#{UCWarfare.GetColorHex("defend_icon_color")}>{DefendIcon}</color>";
                            }
                            if (flag.T2Obj)
                            {
                                if (team == 2)
                                    objective = $"<color=#{UCWarfare.GetColorHex("attack_icon_color")}>{AttackIcon}</color>";
                                else if (team == 1 && flag.Owner == 1)
                                    objective = $"<color=#{UCWarfare.GetColorHex("defend_icon_color")}>{DefendIcon}</color>";
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
                            if (flag.T1Obj)
                            {
                                objective = $"<color=#{UCWarfare.GetColorHex("team_1_color")}>{AttackIcon}</color>";
                                if (flag.Owner == 2)
                                    objective += $"<color=#{UCWarfare.GetColorHex("team_2_color")}>{DefendIcon}</color>";
                            }
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
        public static SendUIParameters RefreshStaticUI(ulong team, Flag flag, bool inVehicle)
        {
            if (team != 1 && team != 2) return SendUIParameters.Nil;
            if (flag.IsAnObj)
            {
                return ComputeUI(team, flag, inVehicle); // if flag is objective send capturing ui.
            }
            else
            {
                if (flag.Owner == team)
                    return new SendUIParameters(team, F.EFlagStatus.SECURED, "secured", UCWarfare.GetColor($"secured_team_{team}_chat"),
                        Flag.MaxPoints, flag.Team1TotalPlayers, flag.Team2TotalCappers);
                else if (flag.Owner == TeamManager.Other(team))
                    return new SendUIParameters(team, F.EFlagStatus.NOT_OWNED, "notowned", UCWarfare.GetColor($"notowned_team_{team}_chat"),
                        Flag.MaxPoints, flag.Team1TotalPlayers, flag.Team2TotalCappers);
                else return new SendUIParameters(team, F.EFlagStatus.NOT_OBJECTIVE, "nocap", UCWarfare.GetColor($"notowned_team_{team}_chat"),
                        Flag.MaxPoints, flag.Team1TotalPlayers, flag.Team2TotalCappers);
            }
        }
    }
    public struct SendUIParameters
    {
        public static readonly SendUIParameters Nil = new SendUIParameters(F.EFlagStatus.DONT_DISPLAY);
        public ulong team;
        public F.EFlagStatus status;
        public string chatTranslation;
        public Color chatColor;
        public int points;
        public bool sendChat;
        public bool sendUI;
        public bool absoluteCap;
        public bool overrideChatConfig;
        public string[] formatting;
        public int team1count;
        public int team2count;
        /// <summary>
        /// <para>Creates a parameter object that will be ignored, kind of the default for the struct.</para>
        /// <para><see cref="F.EFlagStatus.DONT_DISPLAY"/> tells 
        /// <see cref="F.UIOrChat(SendUIParameters, SteamPlayer, ITransportConnection, ulong)"/> not to send the message.</para>
        /// </summary>
        private SendUIParameters(F.EFlagStatus status)
        {
            this.team = 0;
            this.status = status;
            this.chatTranslation = "";
            this.chatColor = UCWarfare.GetColor("default");
            this.points = Flag.MaxPoints;
            this.sendChat = false;
            this.sendUI = false;
            this.absoluteCap = true;
            this.overrideChatConfig = false;
            this.team1count = 0;
            this.team2count = 0;
            this.formatting = new string[0];
        }
        public SendUIParameters(ulong team, F.EFlagStatus status, string chatTranslation,
            Color chatColor, int points, bool sendChat, bool sendUI, bool absoluteCap,
            bool overrideChatConfig, int team1count, int team2count, string[] formatting)
        {
            this.team = team;
            this.status = status;
            this.chatTranslation = chatTranslation;
            this.chatColor = chatColor;
            this.points = points;
            this.sendChat = sendChat;
            this.sendUI = sendUI;
            this.absoluteCap = absoluteCap;
            this.overrideChatConfig = overrideChatConfig;
            this.team1count = team1count;
            this.team2count = team2count;
            this.formatting = formatting;
        }
        public SendUIParameters(ulong team, F.EFlagStatus status, string chatTranslation,
            Color chatColor, int points, int team1count, int team2count, params string[] formatting)
        {
            this.team = team;
            this.status = status;
            this.chatTranslation = chatTranslation;
            this.chatColor = chatColor;
            this.points = points;
            this.sendChat = true;
            this.sendUI = true;
            this.absoluteCap = true;
            this.overrideChatConfig = false;
            this.team1count = team1count;
            this.team2count = team2count;
            this.formatting = formatting;
        }
        public void SendToPlayer(char charactericon, bool useui, ushort uiid, bool pts, string progresschars, SteamPlayer player, ITransportConnection connection) =>
            F.UIOrChat(charactericon, useui, uiid, pts, progresschars, this, player, connection, player.playerID.steamID.m_SteamID);
    }
}
