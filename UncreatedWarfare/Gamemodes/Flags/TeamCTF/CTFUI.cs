using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes.Flags.TeamCTF
{
    public static class CTFUI
    {
        internal static ushort flagListID;
        internal const short flagListKey = 12004;
        internal static ushort captureID;
        internal const short captureKey = 12005;
        internal static ushort headerID;
        internal const short headerKey = 12006;
        internal static void TempCacheEffectIDs()
        {
            if (Assets.find(Gamemode.Config.UI.FlagListGUID) is EffectAsset flagList)
                flagListID = flagList.id;
            if (Assets.find(Gamemode.Config.UI.CaptureGUID) is EffectAsset capture)
                captureID = capture.id;
            if (Assets.find(Gamemode.Config.UI.HeaderGUID) is EffectAsset header)
                headerID = header.id;
            F.Log("Found flags UIs: " + flagListID + ", " + captureID + ", " + headerID);
        }
        public static int FromMax(int cap) => Math.Abs(cap) >= Mathf.RoundToInt(Flag.MAX_POINTS) ? Gamemode.Config.UI.ProgressChars.Length - 1 : ((Gamemode.Config.UI.ProgressChars.Length - 1) / Mathf.RoundToInt(Flag.MAX_POINTS)) * Math.Abs(cap);
        public static int FromMax(int cap, int max) => Math.Abs(cap) >= max ? Gamemode.Config.UI.ProgressChars.Length - 1 : ((Gamemode.Config.UI.ProgressChars.Length - 1) / max) * Math.Abs(cap);
        public static SendUIParameters ComputeUI(ulong team, Flag flag, bool inVehicle)
        {
            if (flag.LastDeltaPoints == 0)
            {
                if (flag.IsContested(out _))
                {
                    return new SendUIParameters(team, inVehicle ? EFlagStatus.IN_VEHICLE : EFlagStatus.CONTESTED, "contested", UCWarfare.GetColor($"contested_team_{team}_chat"),
                        Mathf.RoundToInt(flag.Points), flag.Team1TotalPlayers, flag.Team2TotalCappers, flag.Name, flag.TeamSpecificHexColor);
                }
                else
                {
                    if (flag.IsObj(team))
                        return new SendUIParameters(team, inVehicle ? EFlagStatus.IN_VEHICLE : EFlagStatus.CAPTURING, "capturing", UCWarfare.GetColor($"capturing_team_{team}_chat"),
                            Mathf.RoundToInt(flag.Points), flag.Team1TotalPlayers, flag.Team2TotalCappers, flag.Name, flag.TeamSpecificHexColor);
                    else
                        return new SendUIParameters(team, EFlagStatus.NOT_OBJECTIVE, "nocap", UCWarfare.GetColor($"nocap_team_{team}_chat"),
                            Mathf.RoundToInt(Flag.MAX_POINTS), flag.Team1TotalPlayers, flag.Team2TotalCappers, flag.Name, flag.TeamSpecificHexColor);
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
                        if (flag.Points > -Flag.MAX_POINTS)
                        {
                            return new SendUIParameters(team, inVehicle ? EFlagStatus.IN_VEHICLE : EFlagStatus.CLEARING, "clearing", UCWarfare.GetColor("clearing_team_1_chat"),
                                Mathf.RoundToInt(flag.Points), flag.Team1TotalPlayers, flag.Team2TotalCappers);
                        }
                        else
                        {
                            return new SendUIParameters(team, EFlagStatus.NOT_OWNED, "notowned", UCWarfare.GetColor("notowned_team_1_chat"),
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
                            return new SendUIParameters(team, inVehicle ? EFlagStatus.IN_VEHICLE : EFlagStatus.LOSING, "losing", UCWarfare.GetColor("losing_team_2_chat"),
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
                        if (flag.Points < Flag.MAX_POINTS)
                        {
                            return new SendUIParameters(team, inVehicle ? EFlagStatus.IN_VEHICLE : EFlagStatus.LOSING, "losing", UCWarfare.GetColor("losing_team_2_chat"),
                                Mathf.RoundToInt(flag.Points), flag.Team1TotalPlayers, flag.Team2TotalCappers);
                        }
                        else
                        {
                            return new SendUIParameters(team, EFlagStatus.NOT_OWNED, "notowned", UCWarfare.GetColor("notowned_team_2_chat"),
                                Mathf.RoundToInt(Flag.MAX_POINTS), flag.Team1TotalPlayers, flag.Team2TotalCappers);
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
                        if (flag.Points < Flag.MAX_POINTS)
                        {
                            return new SendUIParameters(team, inVehicle ? EFlagStatus.IN_VEHICLE : EFlagStatus.CLEARING, "clearing", UCWarfare.GetColor("clearing_team_2_chat"),
                                Mathf.RoundToInt(flag.Points), flag.Team1TotalPlayers, flag.Team2TotalCappers);
                        }
                        else
                        {
                            return new SendUIParameters(team, EFlagStatus.NOT_OWNED, "notowned", UCWarfare.GetColor("notowned_team_2_chat"),
                                Mathf.RoundToInt(Flag.MAX_POINTS), flag.Team1TotalPlayers, flag.Team2TotalCappers);
                        }
                    }
                }
                else
                {
                    if (flag.Points > 0)
                    {
                        if (flag.Points < Flag.MAX_POINTS)
                        {
                            return new SendUIParameters(team, inVehicle ? EFlagStatus.IN_VEHICLE : EFlagStatus.LOSING, "losing", UCWarfare.GetColor("losing_team_1_chat"),
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
                        if (flag.Points > -Flag.MAX_POINTS)
                        {
                            return new SendUIParameters(team, inVehicle ? EFlagStatus.IN_VEHICLE : EFlagStatus.LOSING, "losing", UCWarfare.GetColor("losing_team_1_chat"),
                                Mathf.RoundToInt(flag.Points), flag.Team1TotalPlayers, flag.Team2TotalCappers);
                        }
                        else
                        {
                            return new SendUIParameters(team, EFlagStatus.NOT_OWNED, "notowned", UCWarfare.GetColor("notowned_team_1_chat"),
                                Mathf.RoundToInt(Flag.MAX_POINTS), flag.Team1TotalPlayers, flag.Team2TotalCappers);
                        }
                    }
                }
            }
        }
        public static void ClearCaptureUI(UCPlayer player)
        {
            EffectManager.askEffectClearByID(captureID, player.Player.channel.owner.transportConnection);
        }
        public static void ClearCaptureUI(ITransportConnection player)
        {
            EffectManager.askEffectClearByID(captureID, player);
        }
        public static void ClearCaptureUI()
        {
            EffectManager.ClearEffectByID_AllPlayers(captureID);
        }
        public static void ClearFlagList(UCPlayer player)
        {
            EffectManager.askEffectClearByID(flagListID, player.Player.channel.owner.transportConnection);
        }
        public static void ClearFlagList(ITransportConnection player)
        {
            EffectManager.askEffectClearByID(flagListID, player);
        }
        public static void SendFlagList(UCPlayer player)
        {
            if (player == null) return;
            ulong team = player.GetTeam();
            if (team < 1 || team > 3) return;
            if (Data.Is(out IFlagRotation gm))
            {
                ITransportConnection c = player.Player.channel.owner.transportConnection;
                List<Flag> rotation = gm.Rotation;
                EffectManager.sendUIEffect(flagListID, flagListKey, c, true);
                EffectManager.sendUIEffectVisibility(flagListKey, c, true, "Header", true);
                EffectManager.sendUIEffectText(flagListKey, c, true, "Header", F.Translate("flag_header", player));
                if (team == 1 || team == 2)
                {
                    for (int i = 0; i < Gamemode.Config.UI.FlagUICount; i++)
                    {
                        string i2 = i.ToString();
                        if (rotation.Count <= i)
                        {
                            EffectManager.sendUIEffectVisibility(flagListKey, c, true, i2, false);
                        }
                        else
                        {
                            EffectManager.sendUIEffectVisibility(flagListKey, c, true, i2, true);
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
                            EffectManager.sendUIEffectText(flagListKey, c, true, "N" + i2,
                                flag.Discovered(team) ?
                                $"<color=#{flag.TeamSpecificHexColor}>{flag.Name}</color>" :
                                $"<color=#{UCWarfare.GetColorHex("undiscovered_flag")}>{F.Translate("undiscovered_flag", player)}</color>");
                            EffectManager.sendUIEffectText(flagListKey, c, true, "I" + i2, objective);
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
                            EffectManager.sendUIEffectVisibility(flagListKey, c, true, i2, false);
                        }
                        else
                        {
                            EffectManager.sendUIEffectVisibility(flagListKey, c, true, i2, true);
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
                            EffectManager.sendUIEffectText(flagListKey, c, true, "N" + i2,
                                $"<color=#{flag.TeamSpecificHexColor}>{flag.Name}</color>" +
                                $"{(flag.Discovered(1) ? "" : $" <color=#{TeamManager.Team1ColorHex}>?</color>")}" +
                                $"{(flag.Discovered(2) ? "" : $" <color=#{TeamManager.Team2ColorHex}>?</color>")}");
                            EffectManager.sendUIEffectText(flagListKey, c, true, "I" + i2, objective);
                        }
                    }
                }
            }
        }
        public static void ReplicateFlagUpdate(Flag flag, bool ownerChanged = true)
        {
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
                    string i2 = i3.ToString();
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
                            EffectManager.sendUIEffectText(flagListKey, c, true, "N" + i2, $"<color=#{flag.TeamSpecificHexColor}>{flag.Name}</color>");
                        EffectManager.sendUIEffectText(flagListKey, c, true, "I" + i2, objective);
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
                        EffectManager.sendUIEffectText(flagListKey, c, true, "N" + i2,
                            $"<color=#{flag.TeamSpecificHexColor}>{flag.Name}</color>" +
                            $"{(flag.Discovered(1) ? "" : $" <color=#{TeamManager.Team1ColorHex}>?</color>")}" +
                            $"{(flag.Discovered(2) ? "" : $" <color=#{TeamManager.Team2ColorHex}>?</color>")}");
                        EffectManager.sendUIEffectText(flagListKey, c, true, "I" + i2, objective);
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
                    return new SendUIParameters(team, EFlagStatus.SECURED, "secured", UCWarfare.GetColor($"secured_team_{team}_chat"),
                        Mathf.RoundToInt(Flag.MAX_POINTS), flag.Team1TotalPlayers, flag.Team2TotalCappers);
                else if (flag.Owner == TeamManager.Other(team))
                    return new SendUIParameters(team, EFlagStatus.NOT_OWNED, "notowned", UCWarfare.GetColor($"notowned_team_{team}_chat"),
                        Mathf.RoundToInt(Flag.MAX_POINTS), flag.Team1TotalPlayers, flag.Team2TotalCappers);
                else return new SendUIParameters(team, EFlagStatus.NOT_OBJECTIVE, "nocap", UCWarfare.GetColor($"notowned_team_{team}_chat"),
                        Mathf.RoundToInt(Flag.MAX_POINTS), flag.Team1TotalPlayers, flag.Team2TotalCappers);
            }
        }
    }
    public struct SendUIParameters
    {
        public static readonly SendUIParameters Nil = new SendUIParameters(EFlagStatus.DONT_DISPLAY);
        public ulong team;
        public EFlagStatus status;
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
        /// <para><see cref="EFlagStatus.DONT_DISPLAY"/> tells 
        /// <see cref="F.UIOrChat(SendUIParameters, SteamPlayer, ITransportConnection, ulong)"/> not to send the message.</para>
        /// </summary>
        private SendUIParameters(EFlagStatus status)
        {
            this.team = 0;
            this.status = status;
            this.chatTranslation = "";
            this.chatColor = UCWarfare.GetColor("default");
            this.points = Mathf.RoundToInt(Flag.MAX_POINTS);
            this.sendChat = false;
            this.sendUI = false;
            this.absoluteCap = true;
            this.overrideChatConfig = false;
            this.team1count = 0;
            this.team2count = 0;
            this.formatting = new string[0];
        }
        public SendUIParameters(ulong team, EFlagStatus status, string chatTranslation,
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
        public SendUIParameters(ulong team, EFlagStatus status, string chatTranslation,
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
        public static void UIOrChat(char charactericon, bool useui, ushort uiid, bool pts, SendUIParameters p, SteamPlayer player, ITransportConnection connection, ulong translationID) =>
            UIOrChat(charactericon, useui, uiid, pts, p.team, p.status, p.chatTranslation, p.chatColor, connection, player, p.points, translationID,
                p.sendChat, p.sendUI, p.absoluteCap, p.overrideChatConfig, p.formatting, p.team1count, p.team2count);
        public static void UIOrChat(char charactericon, bool useui, ushort uiid, bool pts, ulong team, EFlagStatus type, string translation_key, Color color, ITransportConnection PlayerConnection, SteamPlayer player,
            int c, ulong playerID = 0, bool SendChatIfConfiged = true, bool SendUIIfConfiged = true,
            bool absolute = true, bool sendChatOverride = false, string[] formatting = null, int team1count = 0, int team2count = 0)
        {
            if (type == EFlagStatus.DONT_DISPLAY)
            {
                if (useui && SendUIIfConfiged)
                    EffectManager.askEffectClearByID(uiid, PlayerConnection);
                return;
            }
            int circleAmount = absolute ? Math.Abs(c) : c;
            if (useui && SendUIIfConfiged)
            {
                EffectManager.askEffectClearByID(uiid, PlayerConnection);
                short key = unchecked((short)uiid);
                switch (type)
                {
                    case EFlagStatus.CAPTURING:
                        if (team == 1)
                            EffectManager.sendUIEffect(uiid, key, PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("capturing_team_1_words")}>{F.Translate("ui_capturing", playerID)}{(pts ? $" ({circleAmount}/{Flag.MAX_POINTS})" : "")}</color>",
                                $"<color=#{UCWarfare.GetColorHex("capturing_team_1")}>" +
                                $"{Gamemode.Config.UI.ProgressChars[CTFUI.FromMax(circleAmount)]}</color>", UCWarfare.GetColorHex("capturing_team_1_bkgr"));
                        else if (team == 2)
                            EffectManager.sendUIEffect(uiid, key, PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("capturing_team_2_words")}>{F.Translate("ui_capturing", playerID)}{(pts ? $" ({circleAmount}/{Flag.MAX_POINTS})" : "")}</color>",
                                $"<color=#{UCWarfare.GetColorHex("capturing_team_2")}>" +
                                $"{Gamemode.Config.UI.ProgressChars[CTFUI.FromMax(circleAmount)]}</color>", UCWarfare.GetColorHex("capturing_team_2_bkgr"));
                        break;
                    default:
                    case EFlagStatus.BLANK:
                        if (team == 1)
                            EffectManager.sendUIEffect(uiid, key, PlayerConnection, true, $"",
                                $"<color=#{UCWarfare.GetColorHex("capturing_team_1")}>" +
                                $"{Gamemode.Config.UI.ProgressChars[CTFUI.FromMax(0)]}</color>", UCWarfare.GetColorHex("capturing_team_1_bkgr"));
                        else if (team == 2)
                            EffectManager.sendUIEffect(uiid, key, PlayerConnection, true, $"",
                                $"<color=#{UCWarfare.GetColorHex("capturing_team_2")}>" +
                                $"{Gamemode.Config.UI.ProgressChars[CTFUI.FromMax(0)]}</color>", UCWarfare.GetColorHex("capturing_team_2_bkgr"));
                        break;
                    case EFlagStatus.IN_VEHICLE:
                        if (team == 1)
                            EffectManager.sendUIEffect(uiid, key, PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("in_vehicle_team_1_words")}>{F.Translate("ui_in_vehicle", playerID)}{(pts ? $" ({circleAmount}/{Flag.MAX_POINTS})" : "")}</color>",
                                $"<color=#{UCWarfare.GetColorHex("in_vehicle_team_1")}>" +
                                $"{Gamemode.Config.UI.ProgressChars[CTFUI.FromMax(circleAmount)]}</color>", UCWarfare.GetColorHex("in_vehicle_team_1_bkgr"));
                        else if (team == 2)
                            EffectManager.sendUIEffect(uiid, key, PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("in_vehicle_team_2_words")}>{F.Translate("ui_in_vehicle", playerID)}{(pts ? $" ({circleAmount}/{Flag.MAX_POINTS})" : "")}</color>",
                                $"<color=#{UCWarfare.GetColorHex("in_vehicle_team_2")}>" +
                                $"{Gamemode.Config.UI.ProgressChars[CTFUI.FromMax(circleAmount)]}</color>", UCWarfare.GetColorHex("in_vehicle_team_2_bkgr"));
                        break;
                    case EFlagStatus.LOSING:
                        if (team == 1)
                            EffectManager.sendUIEffect(uiid, key, PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("losing_team_1_words")}>{F.Translate("ui_losing", playerID)}{(pts ? $" ({circleAmount}/{Flag.MAX_POINTS})" : "")}</color>",
                                $"<color=#{UCWarfare.GetColorHex("losing_team_1")}>" +
                                $"{Gamemode.Config.UI.ProgressChars[CTFUI.FromMax(circleAmount)]}</color>", UCWarfare.GetColorHex("losing_team_1_bkgr"));
                        else if (team == 2)
                            EffectManager.sendUIEffect(uiid, key, PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("losing_team_2_words")}>{F.Translate("ui_losing", playerID)}{(pts ? $" ({circleAmount}/{Flag.MAX_POINTS})" : "")}</color>",
                                $"<color=#{UCWarfare.GetColorHex("losing_team_2")}>" +
                                $"{Gamemode.Config.UI.ProgressChars[CTFUI.FromMax(circleAmount)]}</color>", UCWarfare.GetColorHex("losing_team_2_bkgr"));
                        break;
                    case EFlagStatus.SECURED:
                        if (team == 1)
                            EffectManager.sendUIEffect(uiid, key, PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("secured_team_1_words")}>{F.Translate("ui_secured", playerID)}{(pts ? $" ({circleAmount}/{Flag.MAX_POINTS})" : "")}</color>",
                                $"<color=#{UCWarfare.GetColorHex("secured_team_1")}>" +
                                $"{Gamemode.Config.UI.ProgressChars[CTFUI.FromMax(circleAmount)]}</color>", UCWarfare.GetColorHex("secured_team_1_bkgr"));
                        else if (team == 2)
                            EffectManager.sendUIEffect(uiid, key, PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("secured_team_2_words")}>{F.Translate("ui_secured", playerID)}{(pts ? $" ({circleAmount}/{Flag.MAX_POINTS})" : "")}</color>",
                                $"<color=#{UCWarfare.GetColorHex("secured_team_2")}>" +
                                $"{Gamemode.Config.UI.ProgressChars[CTFUI.FromMax(circleAmount)]}</color>", UCWarfare.GetColorHex("secured_team_2_bkgr"));
                        break;
                    case EFlagStatus.CONTESTED:
                        if (team == 1)
                            EffectManager.sendUIEffect(uiid, key, PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("contested_team_1_words")}>{F.Translate("ui_contested", playerID)}{(pts ? $" ({circleAmount}/{Flag.MAX_POINTS})" : "")}</color>",
                                $"<color=#{UCWarfare.GetColorHex("contested_team_1")}>" +
                                $"{Gamemode.Config.UI.ProgressChars[CTFUI.FromMax(circleAmount)]}</color>", UCWarfare.GetColorHex("contested_team_1_bkgr"));
                        else if (team == 2)
                            EffectManager.sendUIEffect(uiid, key, PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("contested_team_2_words")}>{F.Translate("ui_contested", playerID)}{(pts ? $" ({circleAmount}/{Flag.MAX_POINTS})" : "")}</color>",
                                $"<color=#{UCWarfare.GetColorHex("contested_team_2")}>" +
                                $"{Gamemode.Config.UI.ProgressChars[CTFUI.FromMax(circleAmount)]}</color>", UCWarfare.GetColorHex("contested_team_2_bkgr"));
                        break;
                    case EFlagStatus.NOT_OBJECTIVE:
                        if (team == 1)
                            EffectManager.sendUIEffect(uiid, key, PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("nocap_team_1_words")}>{F.Translate("ui_nocap", playerID)}{(pts ? $" ({circleAmount}/{Flag.MAX_POINTS})" : "")}</color>",
                                $"<color=#{UCWarfare.GetColorHex("nocap_team_1")}>" +
                                $"{Gamemode.Config.UI.ProgressChars[CTFUI.FromMax(circleAmount)]}</color>", UCWarfare.GetColorHex("nocap_team_1_bkgr"));
                        else if (team == 2)
                            EffectManager.sendUIEffect(uiid, key, PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("nocap_team_2_words")}>{F.Translate("ui_nocap", playerID)}{(pts ? $" ({circleAmount}/{Flag.MAX_POINTS})" : "")}</color>",
                                $"<color=#{UCWarfare.GetColorHex("nocap_team_2")}>" +
                                $"{Gamemode.Config.UI.ProgressChars[CTFUI.FromMax(circleAmount)]}</color>", UCWarfare.GetColorHex("nocap_team_2_bkgr"));
                        break;
                    case EFlagStatus.LOCKED:
                        if (team == 1)
                            EffectManager.sendUIEffect(uiid, key, PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("locked_team_1_words")}>{F.Translate("ui_locked", playerID)}{(pts ? $" ({circleAmount}/{Flag.MAX_POINTS})" : "")}</color>",
                                $"<color=#{UCWarfare.GetColorHex("locked_team_1")}>" +
                                $"{Gamemode.Config.UI.ProgressChars[CTFUI.FromMax(circleAmount)]}</color>", UCWarfare.GetColorHex("locked_team_1_bkgr"));
                        else if (team == 2)
                            EffectManager.sendUIEffect(uiid, key, PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("locked_team_2_words")}>{F.Translate("ui_locked", playerID)}{(pts ? $" ({circleAmount}/{Flag.MAX_POINTS})" : "")}</color>",
                                $"<color=#{UCWarfare.GetColorHex("locked_team_2")}>" +
                                $"{Gamemode.Config.UI.ProgressChars[CTFUI.FromMax(circleAmount)]}</color>", UCWarfare.GetColorHex("locked_team_2_bkgr"));
                        break;
                    case EFlagStatus.CLEARING:
                        if (team == 1)
                            EffectManager.sendUIEffect(uiid, key, PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("clearing_team_1_words")}>{F.Translate("ui_clearing", playerID)}{(pts ? $" ({circleAmount}/{Flag.MAX_POINTS})" : "")}</color>",
                                $"<color=#{UCWarfare.GetColorHex("clearing_team_1")}>" +
                                $"{Gamemode.Config.UI.ProgressChars[CTFUI.FromMax(circleAmount)]}</color>", UCWarfare.GetColorHex("clearing_team_1_bkgr"));
                        else if (team == 2)
                            EffectManager.sendUIEffect(uiid, key, PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("clearing_team_2_words")}>{F.Translate("ui_clearing", playerID)}{(pts ? $" ({circleAmount}/{Flag.MAX_POINTS})" : "")}</color>",
                                $"<color=#{UCWarfare.GetColorHex("clearing_team_2")}>" +
                                $"{Gamemode.Config.UI.ProgressChars[CTFUI.FromMax(circleAmount)]}</color>", UCWarfare.GetColorHex("clearing_team_2_bkgr"));
                        break;
                    case EFlagStatus.NOT_OWNED:
                        if (team == 1)
                            EffectManager.sendUIEffect(uiid, key, PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("notowned_team_1_words")}>{F.Translate("ui_notowned", playerID)}{(pts ? $" ({circleAmount}/{Flag.MAX_POINTS})" : "")}</color>",
                                $"<color=#{UCWarfare.GetColorHex("notowned_team_2")}>" +
                                $"{Gamemode.Config.UI.ProgressChars[CTFUI.FromMax(circleAmount)]}</color>", UCWarfare.GetColorHex("notowned_team_1_bkgr"));
                        else if (team == 2)
                            EffectManager.sendUIEffect(uiid, key, PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("notowned_team_2_words")}>{F.Translate("ui_notowned", playerID)}{(pts ? $" ({circleAmount}/{Flag.MAX_POINTS})" : "")}</color>",
                                $"<color=#{UCWarfare.GetColorHex("notowned_team_2")}>" +
                                $"{Gamemode.Config.UI.ProgressChars[CTFUI.FromMax(circleAmount)]}</color>", UCWarfare.GetColorHex("notowned_team_2_bkgr"));
                        break;
                }
                if (team1count > 0 && UCWarfare.Config.FlagSettings.EnablePlayerCount)
                {
                    EffectManager.sendUIEffectText(key, PlayerConnection, true, "T1CountIcon", $"<color=#{UCWarfare.GetColorHex("team_count_ui_color_team_1_icon")}>{charactericon}</color>");
                    EffectManager.sendUIEffectText(key, PlayerConnection, true, "T1Count", $"<color=#{UCWarfare.GetColorHex("team_count_ui_color_team_1")}>{team1count}</color>");
                }
                else
                {
                    EffectManager.sendUIEffectText(key, PlayerConnection, true, "T1CountIcon", "");
                    EffectManager.sendUIEffectText(key, PlayerConnection, true, "T1Count", "");
                }
                if (team2count > 0 && UCWarfare.Config.FlagSettings.EnablePlayerCount)
                {
                    EffectManager.sendUIEffectText(key, PlayerConnection, true, "T2CountIcon", $"<color=#{UCWarfare.GetColorHex("team_count_ui_color_team_2_icon")}>{charactericon}</color>");
                    EffectManager.sendUIEffectText(key, PlayerConnection, true, "T2Count", $"<color=#{UCWarfare.GetColorHex("team_count_ui_color_team_2")}>{team2count}</color>");
                }
                else
                {
                    EffectManager.sendUIEffectText(key, PlayerConnection, true, "T2CountIcon", "");
                    EffectManager.sendUIEffectText(key, PlayerConnection, true, "T2Count", "");
                }
            }
            if (sendChatOverride || (UCWarfare.Config.FlagSettings.UseChat && SendChatIfConfiged))
            {
                if (formatting == null)
                    player.SendChat(translation_key, color);
                else
                    player.SendChat(translation_key, color, formatting);
            }
        }
        public void SendToPlayer(SteamPlayer player) =>
            UIOrChat(Gamemode.Config.UI.PlayerIcon, true, CTFUI.captureID, Gamemode.Config.UI.ShowPointsOnUI, this, player, player.transportConnection, player.playerID.steamID.m_SteamID);
    }
    public enum EFlagStatus
    {
        CAPTURING,
        LOSING,
        SECURED,
        CONTESTED,
        NOT_OBJECTIVE,
        CLEARING,
        BLANK,
        NOT_OWNED,
        DONT_DISPLAY,
        IN_VEHICLE,
        LOCKED
    }
}
